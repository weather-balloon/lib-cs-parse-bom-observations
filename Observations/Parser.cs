using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WeatherBalloon.Observations
{
    public interface IObservationParser
    {
        IEnumerable<WeatherStationObservation> parseXml(Stream observationXml);
    }

    public class ObservationParser : IObservationParser
    {

        public IEnumerable<WeatherStationObservation> parseXml(Stream observationXml)
        {
            var xml = XElement.Load(observationXml);

            var amoc = xml.Element("amoc");

            var source = amoc.Element("source");

            DateTime obsIssueTime = DateTime.Parse(amoc.Element("issue-time-utc").Value);

            string obsRegion = source.Element("region").Value;

            var stations =
                from station in xml.Descendants("station")
                select new WeatherStationObservation
                {
                    weather_station_id = station.Attribute("bom-id").Value,
                    name = station.Attribute("stn-name").Value,
                    region = obsRegion,
                    issueTimeUtc = obsIssueTime,
                    description = station.Attribute("description").Value,
                    latitude = station.Attribute("lat").Value,
                    longitude = station.Attribute("lon").Value,
                    timezone = station.Attribute("tz").Value,
                    height = station.Attribute("stn-height").Value,
                    observations = from period in station.Descendants("period")
                                   select new ObservationPeriod
                                   {
                                       period = DateTime.Parse(period.Attribute("time-utc").Value),
                                       measurements = from measure in period.Descendants("element")
                                                      select new Measurement
                                                      {
                                                          unit = measure.Attribute("units")?.Value,
                                                          type = measure.Attribute("type").Value,
                                                          value = measure.Value,
                                                          duration = measure.Attribute("duration") is null ? (int?)null : Int32.Parse(measure.Attribute("duration").Value),
                                                          startTimestampUtc = measure.Attribute("start-time-utc") is null ? (DateTime?)null : DateTime.Parse(measure.Attribute("start-time-utc").Value),
                                                          endTimestampUtc = measure.Attribute("end-time-utc") is null ? (DateTime?)null : DateTime.Parse(measure.Attribute("end-time-utc").Value)
                                                      }
                                   }
                };

            return stations;
        }
    }
}
