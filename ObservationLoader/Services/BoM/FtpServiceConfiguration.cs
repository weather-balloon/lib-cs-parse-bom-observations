//-----------------------------------------------------------------------
// <copyright file="FtpServiceConfiguration.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Services.BoM
{
    using System;
    using System.Text.Json;

    /// <summary>Configuration for a single FTP file resource.</summary>
    public class FtpServiceConfiguration
    {
        /// <summary>The base FTP URL.</summary>
        public Uri BaseFtpUrl { get; set; }

        /// <summary>The BoM product (eg IDV60920).</summary>
        public string Product { get; set; }

        /// <summary>Output config to JSON string.</summary>
        /// <returns>JSON string.</returns>
        public string Serialize() => JsonSerializer.Serialize(this);
    }
}
