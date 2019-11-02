using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text.Json;
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

        public string DatabaseName { get; set; }

        public string CollectionName { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this);
    }

    interface IDataLoader
    {
        public bool connect();

        public bool upsertMany(IEnumerable<WeatherStationObservation> station);
    }

    class MongoDataLoader : IDataLoader
    {

        private readonly ILogger _logger;
        private readonly DataStoreConfiguration _config;

        private IMongoDatabase _database;

        private MongoClient _client;

        public MongoDataLoader(ILogger<MongoDataLoader> logger, IOptions<DataStoreConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public bool connect()
        {
            if (_client is null)
            {
                MongoClientSettings settings = MongoClientSettings.FromUrl(
                    new MongoUrl(_config.ConnectionString)
                );
                _logger.LogDebug($"Data store URL: {settings.Server}");
                settings.SslSettings =
                    new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };


                _client = new MongoClient(settings);

            }

            try
            {
                _logger.LogDebug($"Accessing database {_config.DatabaseName}");
                _database = this._client.GetDatabase(_config.DatabaseName);

                var filter = new BsonDocument("name", _config.CollectionName);
                var collections = this._database.ListCollections(new ListCollectionsOptions { Filter = filter });

                if (!collections.Any())
                {
                    _logger.LogInformation($"Creating collection {_config.CollectionName}");
                    this._database.CreateCollection(_config.CollectionName);
                }

                return true;
            }
            catch (TimeoutException e)
            {
                _logger.LogError($"Failed to connect to the datastore. {e.Message}");
                return false;
            }

        }

        public bool upsertMany(IEnumerable<WeatherStationObservation> observations)
        {
            var collection = _database.GetCollection<WeatherStationObservation>(_config.CollectionName);
            try
            {
                var checkId = observations.First().id;
                var filter = Builders<WeatherStationObservation>.Filter.Eq("_id", checkId);

                if (collection.CountDocuments(filter) == 0)
                {
                    collection.InsertMany(observations);
                    _logger.LogInformation("Datastore updated");
                }
                else
                {
                    _logger.LogInformation("Datastore not updated as data exists");
                }
                return true;
            }
            catch (MongoBulkWriteException e)
            {
                _logger.LogError($"Failed to write to the datastore. {e.Message}");
                return false;
            }
        }

    }
}
