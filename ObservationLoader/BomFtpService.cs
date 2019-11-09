using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherBalloon.Observations;

namespace WeatherBalloon.ObservationLoader
{
    class FtpServiceConfiguration
    {
        public Uri BaseFtpUrl { get; set; }

        public string Product { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this);
    }
    interface IObservationService
    {
        IEnumerable<WeatherStationObservation> loadObservations();
    }
    class BomFtpService : IObservationService
    {
        private readonly ILogger _logger;
        private readonly FtpServiceConfiguration _config;

        public BomFtpService(ILogger<BomFtpService> logger, IOptions<FtpServiceConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;

            if (_config.BaseFtpUrl is null)
            {
                throw new ArgumentException("The observation FTP URL is null");
            }

            if (String.IsNullOrEmpty(_config.Product))
            {
                throw new ArgumentException("No observation product provided");
            }
        }

        public IEnumerable<WeatherStationObservation> loadObservations()
        {
            Uri productUrl = new Uri(_config.BaseFtpUrl, $"{_config.Product}.xml");

            _logger.LogInformation($"Loading observation data from: {productUrl.ToString()}");

            IEnumerable<WeatherStationObservation> result;

            try
            {
                result = loadFtpResource(productUrl, new ObservationParser());
                _logger.LogInformation($"Completed loading observation data from: {productUrl.ToString()}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to retrieve and parse the observation data. {e.Message}");
                result = null;
            }

            return result;
        }

        static IEnumerable<WeatherStationObservation> loadFtpResource(Uri url, IObservationParser parser)
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
            var result = parser.parseXml(responseStream);
            responseStream.Close();
            response.Close();

            return result;
        }

    }
}
