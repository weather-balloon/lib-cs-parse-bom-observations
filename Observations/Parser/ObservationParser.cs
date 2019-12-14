//-----------------------------------------------------------------------
// <copyright file="ObservationParser.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------
namespace WeatherBalloon.Observations.Parser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WeatherBalloon.Observations.Model;

    /// <summary>
    /// Observation parser for Australian Bureau of Meteorology (BoM) data.
    /// </summary>
    public class ObservationParser : IObservationParser
    {
        /// <summary>
        /// Parses observation from BoM-based weather stations.
        /// </summary>
        /// <param name="observationXml">The observation data.</param>
        /// <returns>List of observations.</returns>
        /// <see>http://reg.bom.gov.au/catalogue/Observations-XML.pdf .</see>
        public IEnumerable<WeatherStationObservation> ParseXml(Stream observationXml)
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
                    WeatherStationId = station.Attribute("bom-id").Value,
                    Name = station.Attribute("stn-name").Value,
                    Region = obsRegion,
                    IssueTimeUtc = obsIssueTime,
                    Description = station.Attribute("description").Value,
                    Latitude = station.Attribute("lat").Value,
                    Longitude = station.Attribute("lon").Value,
                    Timezone = station.Attribute("tz").Value,
                    Height = station.Attribute("stn-height").Value,
                    Observations = from period in station.Descendants("period")
                                   select new ObservationPeriod
                                   {
                                       Period = DateTime.Parse(period.Attribute("time-utc").Value),
                                       Measurements = from measure in period.Descendants("element")
                                                      select new Measurement
                                                      {
                                                          Unit = measure.Attribute("units")?.Value,
                                                          Type = measure.Attribute("type").Value,
                                                          Value = measure.Value,
                                                          Duration = measure.Attribute("duration") is null ? (int?)null : int.Parse(measure.Attribute("duration").Value),
                                                          StartTimestampUtc = measure.Attribute("start-time-utc") is null ? (DateTime?)null : DateTime.Parse(measure.Attribute("start-time-utc").Value),
                                                          EndTimestampUtc = measure.Attribute("end-time-utc") is null ? (DateTime?)null : DateTime.Parse(measure.Attribute("end-time-utc").Value),
                                                      },
                                   },
                };
            return stations;
        }
    }
}
