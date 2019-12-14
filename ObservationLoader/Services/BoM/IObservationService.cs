//-----------------------------------------------------------------------
// <copyright file="IObservationService.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Services.BoM
{
    using System.Collections.Generic;
    using WeatherBalloon.Observations.Model;

    /// <summary>Interface for observation loaders.</summary>
    public interface IObservationService
    {
        /// <summary>Load observations from a service.</summary>
        /// <returns>A list of observations.</returns>
        IEnumerable<WeatherStationObservation> LoadObservations();
    }
}
