using System;
using System.Collections.Generic;

namespace WeatherBalloon.Observations
{
    public class Measurement
    {
        public string unit { get; set; }

        public string type { get; set; }

        public string value { get; set; }

        public int? duration { get; set; }

        public DateTime? startTimestampUtc { get; set; }

        public DateTime? endTimestampUtc { get; set; }
    }

    public class ObservationPeriod
    {

        public IEnumerable<Measurement> measurements { get; set; }

        public DateTime period { get; set; }

    }

    public class WeatherStationObservation
    {

        public string id
        {
            get
            {
                return $"{weather_station_id}-{issueTimeUtc.ToString("o")}";
            }
        }

        public string weather_station_id { get; set; }

        public string name { get; set; }

        public string region { get; set; }

        public DateTime issueTimeUtc { get; set; }

        public string description { get; set; }

        public string latitude { get; set; }

        public string longitude { get; set; }

        public string timezone { get; set; }

        public string height { get; set; }

        public IEnumerable<ObservationPeriod> observations { get; set; }

    }
}
