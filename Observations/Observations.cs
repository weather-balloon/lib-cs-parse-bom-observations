using System;
using System.Collections.Generic;

namespace WeatherBalloon.Observations
{
    public class Measurement
    {
        public string unit { get; set; }

        public string type { get; set; }

        public string value { get; set; }

        public Nullable<int> duration { get; set; }
    }

    public class ObservationPeriod
    {

        public int index { get; set; }

        public DateTime period { get; set; }

        public IEnumerable<Measurement> measurements { get; set; }

    }

    public class WeatherStation
    {

        public string id { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public string latitude { get; set; }

        public string longitude { get; set; }

        public string height { get; set; }

        public IEnumerable<ObservationPeriod> observations { get; set; }

    }
}
