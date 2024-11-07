using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Inholland.SSPJelleKoomen;

public class WeatherService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<List<WeatherStation>> GetWeatherDataAsync()
    {
        var response = await httpClient.GetStringAsync("https://data.buienradar.nl/2.0/feed/json");
        var weatherData = JsonConvert.DeserializeObject<WeatherData>(response);
        return weatherData?.Actual?.StationMeasurements;
    }
}

public class WeatherStation
{
    public string StationId { get; set; }
    public string StationName { get; set; }
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public string WeatherDescription { get; set; }
}

public class WeatherData
{
    public Actual Actual { get; set; }
}

public class Actual
{
    public List<WeatherStation> StationMeasurements { get; set; }
}