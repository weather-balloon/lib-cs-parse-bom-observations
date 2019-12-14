//-----------------------------------------------------------------------
// <copyright file="Measurement.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.Observations.Model
{
    using System;

    /// <summary>
    /// Stores a measurement collected during an observation.
    /// </summary>
    public class Measurement
    {
        /// <summary>The unit of measurement (e.g. celsius).</summary>
        public string Unit { get; set; }

        /// <summary>The type of measurement (e.g. air temperature).</summary>
        public string Type { get; set; }

        /// <summary>The measurement value.</summary>
        public string Value { get; set; }

        /// <summary>The time period over which the measurement was taken.</summary>
        public int? Duration { get; set; }

        /// <summary>Start time of observation period.</summary>
        public DateTime? StartTimestampUtc { get; set; }

        /// <summary>End time of observation period.</summary>
        public DateTime? EndTimestampUtc { get; set; }
    }
}
