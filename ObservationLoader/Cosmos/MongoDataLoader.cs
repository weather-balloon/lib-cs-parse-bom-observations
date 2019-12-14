//-----------------------------------------------------------------------
// <copyright file="MongoDataLoader.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MongoDB.Driver;
    using WeatherBalloon.Observations.Model;

    /// <summary>
    /// Loads observation data to MongoDB (Cosmos).
    /// </summary>
    public class MongoDataLoader : IDataLoader
    {
        private readonly ILogger logger;
        private readonly DataStoreConfiguration config;

        private Random rand = new Random();

        private IMongoDatabase database;

        private MongoClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDataLoader"/> class.
        /// </summary>
        /// <param name="logger">The usual logger.</param>
        /// <param name="config">The Data Store configuration.</param>
        public MongoDataLoader(ILogger<MongoDataLoader> logger, IOptions<DataStoreConfiguration> config)
        {
            this.logger = logger;
            this.config = config.Value;

            this.config.ConnectionString = !string.IsNullOrEmpty(this.config.ConnectionString) ? this.config.ConnectionString : $"mongodb://{this.config.Username}:{this.config.Password}@{this.config.Server}";

            if (string.IsNullOrEmpty(this.config.ConnectionString))
            {
                throw new ArgumentException("No connection string provided");
            }

            if (string.IsNullOrEmpty(this.config.DatabaseName))
            {
                throw new ArgumentException("No database name provided");
            }

            if (string.IsNullOrEmpty(this.config.CollectionName))
            {
                throw new ArgumentException("No collection name provided");
            }
        }

        /// <summary>Connects to the datastore.</summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool Connect()
        {
            if (client is null)
            {
                MongoClientSettings settings;

                try
                {
                    settings = MongoClientSettings.FromUrl(
                        new MongoUrl(config.ConnectionString));

                    // Cosmos DB appears to need this disabled
                    settings.RetryWrites = false;

                    settings.UseTls = config.UseTls;

                    // Although this is deprecated in favour of UseTls, Cosmos uses this property
                    settings.UseSsl = config.UseTls;
                }
                catch (MongoConfigurationException)
                {
                    logger.LogError($"Failed to connect to the datastore (configuration).");
                    return false;
                }

                logger.LogDebug($"Data store server: {settings.Server}; using TLS/SSL: {settings.SslSettings}");
                settings.SslSettings =
                    new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

                client = new MongoClient(settings);
            }

            try
            {
                logger.LogDebug($"Accessing database {config.DatabaseName}");
                database = this.client.GetDatabase(config.DatabaseName);

                /*
                // This section causes problems when you have multiple processes
                // as they compete to create the collection. Cosmos also doesn't seem
                // to need this

                var filter = new BsonDocument("name", _config.CollectionName);
                var collections = this._database.ListCollections(new ListCollectionsOptions { Filter = filter });

                if (!collections.Any())
                {
                    _logger.LogInformation($"Creating collection {_config.CollectionName}");
                    this._database.CreateCollection(_config.CollectionName);
                }
                 */

                return true;
            }
            catch (TimeoutException e)
            {
                logger.LogError($"Failed to connect to the datastore (timeout). {e.Message}");
                return false;
            }
            catch (MongoAuthenticationException e)
            {
                logger.LogError($"Failed to connect to the datastore (authentication). {e.Message}");
                return false;
            }
        }

        /// <summary>Bulk upsert records.</summary>
        /// <param name="observations">The set of observations to be loaded.</param>
        /// <param name="maxRetries">Max number of retries that can be used if rate limited.</param>
        /// <param name="carriedOverErrors">Accumulates errors over retries.</param>
        /// <param name="performCheck">
        /// If True, check if the record already exists and delete it. If False, the method attempts
        /// a basic Insert operation.
        /// </param>
        /// <returns>True if success, False otherwise.</returns>
        public bool UpsertMany(
            IEnumerable<WeatherStationObservation> observations,
            int maxRetries = 10,
            IList<CosmosError> carriedOverErrors = null,
            bool performCheck = true)
        {
            var collection = database.GetCollection<WeatherStationObservation>(config.CollectionName);
            WeatherStationObservation[] observationsArr = observations.ToArray();

            if (observationsArr.Count() == 0)
            {
                logger.LogInformation("No records to insert - nothing to do.");
                return true;
            }

            if (performCheck)
            {
                var checkId = observationsArr.First().Id;
                var filter = Builders<WeatherStationObservation>.Filter.Eq("_id", checkId);

                try
                {
                    if (collection.CountDocuments(filter) > 0)
                    {
                        logger.LogInformation("Datastore not updated as data exists");
                        return false;
                    }
                }
                catch (MongoCommandException e)
                {
                    logger.LogInformation($"Couldn't perform check: {e.Message}");
                    if (maxRetries == 0)
                    {
                        logger.LogError($"Datastore update exception (no retries available): {e.Message}");
                        return false;
                    }

                    var cosmosError = CosmosError.ParseCosmosErrorMessage(e.Message);
                    Task.Delay(cosmosError.RetryAfterMs);
                    UpsertMany(observations, maxRetries: maxRetries - 1, carriedOverErrors: carriedOverErrors, performCheck: true);
                }
            }

            try
            {
                logger.LogInformation($"Records to be inserted: {observationsArr.Count()}");

                collection.InsertMany(observationsArr);
                logger.LogInformation("Datastore updated");
                return true;
            }
            catch (MongoCommandException e)
            {
                logger.LogError($"Unhandled exception: {e.Source} - {e.Message}");
                return false;
            }
            catch (MongoBulkWriteException<WeatherStationObservation> e)
            {
                if (maxRetries == 0)
                {
                    logger.LogError($"Datastore update exception (no retries available): {e.Source} - {e.Message}");
                    return false;
                }

                var errorList = CosmosError.ParseBulkWriteException(e);

                var errorRecords = errorList.Select(we => observationsArr[we.Index]);

                var unprocessedRecords = e.UnprocessedRequests
                    .Where(ur => ur.ModelType == WriteModelType.InsertOne)
                    .OfType<InsertOneModel<WeatherStationObservation>>()
                    .Select(ur => ur.Document);

                var remainingObservations = unprocessedRecords.Union(errorRecords);

                if (remainingObservations.Count() > 0)
                {
                    int retryInterval = errorList.Max(rec => rec.RetryAfterMs) + rand.Next(1000, 5000);

                    logger.LogInformation($"Retrying {remainingObservations.Count()} records in {retryInterval}ms");
                    Task.Delay(retryInterval);
                    return UpsertMany(
                        remainingObservations,
                        maxRetries: maxRetries - 1,
                        carriedOverErrors: carriedOverErrors,
                        performCheck: false);
                }

                return false;
            }
        }
    }
}
