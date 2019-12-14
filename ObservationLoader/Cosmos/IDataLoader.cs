//-----------------------------------------------------------------------
// <copyright file="IDataLoader.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Cosmos
{
    using System.Collections.Generic;
    using WeatherBalloon.Observations.Model;

    /// <summary>
    /// A basic interface for connecting to a datasource and
    /// performing a bulk upload.
    /// </summary>
    public interface IDataLoader
    {
        /// <summary>Connects to the datastore.</summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool Connect();

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
            int maxRetries,
            IList<CosmosError> carriedOverErrors,
            bool performCheck = true);
    }
}
