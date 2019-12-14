//-----------------------------------------------------------------------
// <copyright file="IObservationParser.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.Observations.Parser
{
    using System.Collections.Generic;
    using System.IO;
    using WeatherBalloon.Observations.Model;

    /// <summary>
    /// Interface for parsing weather observations.
    /// </summary>
    public interface IObservationParser
    {
        /// <summary>
        /// Parses an XML-based observation report from a weather station.
        /// </summary>
        /// <param name="observationXml">Observation data in XML format.</param>
        /// <returns>A list of observations.</returns>
        IEnumerable<WeatherStationObservation> ParseXml(Stream observationXml);
    }
}
