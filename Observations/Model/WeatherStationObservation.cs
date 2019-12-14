//-----------------------------------------------------------------------
// <copyright file="WeatherStationObservation.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.Observations.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Describes a weather station and a set of observations
    /// being published for (from) the station.
    /// </summary>
    public class WeatherStationObservation
    {
        /// <summary>A unique identifier for the weather station.</summary>
        public string Id
        {
            get
            {
                return $"{this.WeatherStationId}-{this.IssueTimeUtc.ToString("o")}";
            }
        }

        /// <summary>The ID given to the weather station by its operator.</summary>
        public string WeatherStationId { get; set; }

        /// <summary>The weather station name.</summary>
        public string Name { get; set; }

        /// <summary>The region (e.g. state) in which the station is situated.</summary>
        public string Region { get; set; }

        /// <summary>The datetime on which the report was issued.</summary>
        public DateTime IssueTimeUtc { get; set; }

        /// <summary>Open text field for general details.</summary>
        public string Description { get; set; }

        /// <summary>Weather station latitude.</summary>
        public string Latitude { get; set; }

        /// <summary>Weather station longitude.</summary>
        public string Longitude { get; set; }

        /// <summary>Weather station's local timezone.</summary>
        public string Timezone { get; set; }

        /// <summary>The altitude of the weather station.</summary>
        public string Height { get; set; }

        /// <summary>A list of recorded observations.</summary>
        public IEnumerable<ObservationPeriod> Observations { get; set; }
    }
}
