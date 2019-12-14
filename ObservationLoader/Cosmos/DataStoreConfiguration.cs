//-----------------------------------------------------------------------
// <copyright file="DataStoreConfiguration.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Cosmos
{
    using System.Text.Json;

    /// <summary>Datastore connection config.</summary>
    public class DataStoreConfiguration
    {
        /// <summary>The full Datastore connection string.</summary>
        public string ConnectionString { get; set; }

        /// <summary>The username.</summary>
        public string Username { get; set; }

        /// <summary>Connection password.</summary>
        public string Password { get; set; }

        /// <summary>Address of the server.</summary>
        public string Server { get; set; }

        /// <summary>Use TLS connection encyrption.</summary>
        public bool UseTls { get; set; } = true;

        /// <summary>The name of the database.</summary>
        public string DatabaseName { get; set; }

        /// <summary>The collection in which the database is stored.</summary>
        public string CollectionName { get; set; }

        /// <summary>Returns the configuration as a JSON string.</summary>
        /// <returns>JSON string.</returns>
        public string Serialize() => JsonSerializer.Serialize(this);
    }
}
