//-----------------------------------------------------------------------
// <copyright file="ObservationPeriod.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.Observations.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Collects the measurements collected during an observation.
    /// </summary>
    public class ObservationPeriod
    {
        /// <summary>The measurements recorded over the observation.</summary>
        public IEnumerable<Measurement> Measurements { get; set; }

        /// <summary>The observation datetime.</summary>
        public DateTime Period { get; set; }
    }
}
