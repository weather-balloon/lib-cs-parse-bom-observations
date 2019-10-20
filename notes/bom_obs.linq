<Query Kind="Program">
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
</Query>

void Main()
{
	var xml = XElement.Load($"{Path.GetDirectoryName(Util.CurrentQueryPath)}/test-obs.xml");

	var stations = 
		from station in xml.Descendants("station")
		select new WeatherStation 
		{
			id = station.Attribute("bom-id").Value,
			name = station.Attribute("stn-name").Value,
			description = station.Attribute("description").Value,
			latitude = station.Attribute("lat").Value,
			longitude = station.Attribute("lon").Value,
			height = station.Attribute("stn-height").Value,
			observations = from period in station.Descendants("period")
							select new ObservationPeriod {
								index = Int32.Parse(period.Attribute("index").Value),
								period = DateTime.Parse(period.Attribute("time-utc").Value),
								measurements = from measure in period.Descendants("element")
												select new Measurement {
													unit = measure.Attribute("units")?.Value,
													type = measure.Attribute("type").Value,
													value = measure.Value
												}
							}
		};
	
	string json = JsonSerializer.Serialize(stations);
		
	Console.WriteLine(json);
	
	/*
	foreach (XElement station in stations) {
		
		var observations = 
			from obs in station.Descendants("period")
			select obs;
		
		var observationList = new List<Observation>();
		
		foreach (XElement observation in observations) {
			observationList.Add(new Observation {
				Period = DateTime.Parse(observation.Attribute("time-utc").Value)
			});
		}
		
		var weatherStation = new WeatherStation {
			Id = station.Attribute("bom-id").Value,
			Name = station.Attribute("stn-name").Value,
			Description = station.Attribute("description").Value,
			Latitude = station.Attribute("lat").Value,
			Longitude = station.Attribute("lon").Value,
			Height = station.Attribute("stn-height").Value,
			Observations = observationList
		};
		
		string json = JsonSerializer.Serialize(weatherStation);
		
		Console.WriteLine(json);
	}
	*/
}

public class Measurement {
	public string unit { get; set; }
	
	public string type { get; set; }
	
	public string value { get; set; }
	
	public Nullable<int> duration { get; set; }
}

public class ObservationPeriod {

	public int index { get; set; }

	public DateTime period { get; set; }
	
	public IEnumerable<Measurement> measurements { get; set; }

}

public class WeatherStation {
	
	public string id { get; set; }
	
	public string name { get; set; }
	
	public string description { get; set; }
	
	public string latitude { get; set; }
	
	public string longitude { get; set; }
	
	public string height { get; set; }
	
	public IEnumerable<ObservationPeriod> observations { get; set; }
	
}