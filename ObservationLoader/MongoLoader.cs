using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using WeatherBalloon.Observations;

namespace WeatherBalloon.ObservationLoader
{

    class DataStoreConfiguration
    {
        public string ConnectionString { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Server { get; set; }

        public bool UseTls { get; set; } = true;

        public string DatabaseName { get; set; }

        public string CollectionName { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this);
    }

    interface IDataLoader
    {
        public bool connect();

        public bool upsertMany(IEnumerable<WeatherStationObservation> station, int maxRetries, IList<CosmosError> carriedOverErrors, bool performCheck = true);
    }

    class MongoDataLoader : IDataLoader
    {
        private Random _rand = new Random();

        private readonly ILogger _logger;
        private readonly DataStoreConfiguration _config;

        private IMongoDatabase _database;

        private MongoClient _client;

        public MongoDataLoader(ILogger<MongoDataLoader> logger, IOptions<DataStoreConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;

            _config.ConnectionString = !string.IsNullOrEmpty(_config.ConnectionString) ? _config.ConnectionString : $"mongodb://{_config.Username}:{_config.Password}@{_config.Server}";

            if (String.IsNullOrEmpty(_config.ConnectionString))
            {
                throw new ArgumentException("No connection string provided");
            }

            if (String.IsNullOrEmpty(_config.DatabaseName))
            {
                throw new ArgumentException("No database name provided");
            }

            if (String.IsNullOrEmpty(_config.CollectionName))
            {
                throw new ArgumentException("No collection name provided");
            }
        }

        public bool connect()
        {
            if (_client is null)
            {
                MongoClientSettings settings;

                try
                {
                    settings = MongoClientSettings.FromUrl(
                        new MongoUrl(_config.ConnectionString)
                    );
                    // Cosmos DB appears to need this disabled
                    settings.RetryWrites = false;
                    settings.UseTls = _config.UseTls;

                    // Although this is deprecated in favour of UseTls, Cosmos uses this property
                    settings.UseSsl = _config.UseTls;
                }
                catch (MongoConfigurationException)
                {
                    _logger.LogError($"Failed to connect to the datastore (configuration).");
                    return false;
                }

                _logger.LogDebug($"Data store server: {settings.Server}; using TLS/SSL: {settings.SslSettings}");
                settings.SslSettings =
                    new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

                _client = new MongoClient(settings);

            }

            try
            {
                _logger.LogDebug($"Accessing database {_config.DatabaseName}");
                _database = this._client.GetDatabase(_config.DatabaseName);

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
                _logger.LogError($"Failed to connect to the datastore (timeout). {e.Message}");
                return false;
            }
            catch (MongoAuthenticationException e)
            {
                _logger.LogError($"Failed to connect to the datastore (authentication). {e.Message}");
                return false;
            }

        }

        public bool upsertMany(IEnumerable<WeatherStationObservation> observations,
                                int maxRetries = 10,
                                IList<CosmosError> carriedOverErrors = null,
                                bool performCheck = true)
        {
            var collection = _database.GetCollection<WeatherStationObservation>(_config.CollectionName);
            WeatherStationObservation[] observationsArr = observations.ToArray();

            if (observationsArr.Count() == 0)
            {
                _logger.LogInformation("No records to insert - nothing to do.");
                return true;
            }

            if (performCheck)
            {
                var checkId = observationsArr.First().id;
                var filter = Builders<WeatherStationObservation>.Filter.Eq("_id", checkId);

                try
                {
                    if (collection.CountDocuments(filter) > 0)
                    {
                        _logger.LogInformation("Datastore not updated as data exists");
                        return false;
                    }
                }
                catch (MongoCommandException e)
                {
                    _logger.LogInformation($"Couldn't perform check: {e.Message}");
                    if (maxRetries == 0)
                    {
                        _logger.LogError($"Datastore update exception (no retries available): {e.Message}");
                        return false;
                    }

                    var cosmosError = CosmosError.ParseCosmosErrorMessage(e.Message);
                    Task.Delay(cosmosError.RetryAfterMs);
                    upsertMany(observations, maxRetries: maxRetries - 1, carriedOverErrors: carriedOverErrors, performCheck: true);
                }
            }

            try
            {
                _logger.LogInformation($"Records to be inserted: {observationsArr.Count()}");

                collection.InsertMany(observationsArr);
                _logger.LogInformation("Datastore updated");
                return true;
            }
            catch (MongoCommandException e)
            {
                _logger.LogError($"Unhandled exception: {e.Source} - {e.Message}");
                return false;
            }
            catch (MongoBulkWriteException<WeatherStationObservation> e)
            {
                if (maxRetries == 0)
                {
                    _logger.LogError($"Datastore update exception (no retries available): {e.Source} - {e.Message}");
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
                    int retryInterval = errorList.Max(rec => rec.RetryAfterMs) + _rand.Next(1000, 5000);

                    _logger.LogInformation($"Retrying {remainingObservations.Count()} records in {retryInterval}ms");
                    Task.Delay(retryInterval);
                    return upsertMany(remainingObservations,
                        maxRetries: maxRetries - 1,
                        carriedOverErrors: carriedOverErrors,
                        performCheck: false);
                }
                return false;
            }
        }
    }
    public class CosmosError
    {

        public static readonly HashSet<int> RetriableErrorCodes = new HashSet<int> { 16500 };

        public int Index { get; set; }
        public int ErrorCode { get; set; } = 0;
        public int RetryAfterMs { get; set; }
        public string Details { get; set; } = "";

        public bool isRetriable
        {
            get
            {
                return RetriableErrorCodes.Contains(ErrorCode);
            }
        }

        static public IList<CosmosError> ParseBulkWriteException(MongoBulkWriteException exceptionObject)
        {
            List<CosmosError> result = new List<CosmosError>();
            foreach (var we in exceptionObject.WriteErrors)
            {
                result.Add(ParseBulkWriteError(we));
            }
            return result;
        }

        static public CosmosError ParseBulkWriteError(BulkWriteError writeError)
        {

            var result = ParseCosmosErrorMessage(writeError.Message);
            result.Index = writeError.Index;

            return result;
        }

        static public CosmosError ParseCosmosErrorMessage(string message)
        {
            var result = new CosmosError();
            foreach (var el in message.Split(','))
            {
                var attr = el.Trim().Split('=');

                if (attr.Count() != 2) continue;

                if (attr[0] == "Error") result.ErrorCode = Int32.Parse(attr[1]);

                if (attr[0] == "RetryAfterMs") result.RetryAfterMs = Int32.Parse(attr[1]);
            }
            return result;

        }
    }
}
