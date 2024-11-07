using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inholland.SSPJelleKoomen;

public static class QueueTriggerFunctions
{
    private static readonly HttpClient httpClient = new HttpClient();

    [FunctionName("ProcessJobQueue")]
    public static async Task ProcessJobQueue(
        [QueueTrigger("start-job-queue", Connection = "AzureWebJobsStorage")]
        string jobId,
        [Queue("image-processing-queue", Connection = "AzureWebJobsStorage")]
        IAsyncCollector<string> imageQueueCollector,
        ILogger log)
    {
        log.LogInformation("ProcessJobQueue function started.");

        var weatherService = new WeatherService();
        var weatherStations = await weatherService.GetWeatherDataAsync();

        if (weatherStations == null || !weatherStations.Any())
        {
            log.LogError("No weather data available.");
            return;
        }

        log.LogInformation($"Retrieved {weatherStations.Count} weather stations.");

        var tasks = weatherStations.Select(async station =>
        {
            var message = new QueueMessage
            {
                JobId = jobId,
                StationId = station.StationId,
                StationName = station.StationName,
                Temperature = station.Temperature,
                WindSpeed = station.WindSpeed,
                WeatherDescription = station.WeatherDescription
            };

            await imageQueueCollector.AddAsync(JsonConvert.SerializeObject(message));
        });

        await Task.WhenAll(tasks);
        

        log.LogInformation("ProcessJobQueue function completed.");
    }

    [FunctionName("ProcessImageQueue")]
    public static async Task ProcessImageQueue(
        [QueueTrigger("image-processing-queue", Connection = "AzureWebJobsStorage")]
        QueueMessage data,
        [Blob("images/{JobId}/{StationId}.png", FileAccess.Write, Connection = "AzureWebJobsStorage")]
        Stream blobStream,
        ILogger log)
    {
        log.LogInformation("ProcessImageQueue function started.");
    
        var UnsplashApiKey = Environment.GetEnvironmentVariable("UnsplashApiKey");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", UnsplashApiKey);
        var imageResponse = await httpClient.GetAsync($"https://api.unsplash.com/photos/random?client_id={UnsplashApiKey}");
        var jsonResponse = await imageResponse.Content.ReadAsStringAsync();
        log.LogInformation($"Unsplash response: {jsonResponse}");

        var jsonObject = JObject.Parse(jsonResponse);
        var imageUrl = jsonObject["urls"]?["full"]?.ToString();
        log.LogInformation($"Fetching image from URL: {imageUrl}");

        try
        {
            var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            var imageData = await response.Content.ReadAsByteArrayAsync();
            using var imageStream = new MemoryStream(imageData);

            var processedImageStream = ImageHelper.AddTextToImage(imageStream,
                (data.StationName, (10, 10), 48, "#FFFFFF"),
                ($"Temp: {data.Temperature}°C", (10, 40), 36, "#FFFFFF"),
                ($"Wind: {data.WindSpeed} km/h", (10, 70), 36, "#FFFFFF"),
                (data.WeatherDescription, (10, 100), 36, "#FFFFFF"));

            await processedImageStream.CopyToAsync(blobStream);
        }
        catch (HttpRequestException ex)
        {
            log.LogError(
                $"Error fetching image from URL: {imageUrl}. Status Code: {ex.StatusCode}. Message: {ex.Message}");
            throw;
        }

        log.LogInformation("ProcessImageQueue function completed.");
    }
}

public class QueueMessage
{
    public string JobId { get; set; }
    public string StationId { get; set; }
    public string StationName { get; set; }
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public string WeatherDescription { get; set; }
}