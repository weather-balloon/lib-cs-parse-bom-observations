//-----------------------------------------------------------------------
// <copyright file="BomFtpService.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Services.BoM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using WeatherBalloon.Observations.Model;
    using WeatherBalloon.Observations.Parser;

    /// <summary>Used to collect BoM observations via FTP.</summary>
    public class BomFtpService : IObservationService
    {
        private readonly ILogger logger;
        private readonly FtpServiceConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="BomFtpService"/> class.
        /// </summary>
        /// <param name="logger">The usual logger.</param>
        /// <param name="config">The FTP Service configuration.</param>
        public BomFtpService(ILogger<BomFtpService> logger, IOptions<FtpServiceConfiguration> config)
        {
            this.logger = logger;
            this.config = config.Value;

            if (this.config.BaseFtpUrl is null)
            {
                throw new ArgumentException("The observation FTP URL is null");
            }

            if (string.IsNullOrEmpty(this.config.Product))
            {
                throw new ArgumentException("No observation product provided");
            }
        }

        /// <summary>Performs the FTP data load.</summary>
        /// <param name="url">The FTP resource URL.</param>
        /// <param name="parser">The parser to handle the resource.</param>
        /// <returns>A list of weather observations.</returns>
        public static IEnumerable<WeatherStationObservation> LoadFtpResource(Uri url, IObservationParser parser)
        {
            WebResponse response;
            if (url.Scheme == Uri.UriSchemeFtp)
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                response = (FtpWebResponse)request.GetResponse();
            }
            else
            {
                return null;
            }

            Stream responseStream = response.GetResponseStream();
            var result = parser.ParseXml(responseStream);
            responseStream.Close();
            response.Close();

            return result;
        }

        /// <summary>Performs the FTP data load.</summary>
        /// <returns>A list of weather observations.</returns>
        public IEnumerable<WeatherStationObservation> LoadObservations()
        {
            Uri productUrl = new Uri(config.BaseFtpUrl, $"{config.Product}.xml");

            logger.LogInformation($"Loading observation data from: {productUrl.ToString()}");

            IEnumerable<WeatherStationObservation> result;

            try
            {
                result = LoadFtpResource(productUrl, new ObservationParser());
                logger.LogInformation($"Completed loading observation data from: {productUrl.ToString()}");
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to retrieve and parse the observation data. {e.Message}");
                result = null;
            }

            return result;
        }
    }
}
