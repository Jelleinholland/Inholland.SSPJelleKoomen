using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Inholland.SSPJelleKoomen;

public static class HttpTriggerFunctions
{
    [FunctionName("StartJob")]
    public static async Task<IActionResult> StartJob(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [Queue("start-job-queue", Connection = "AzureWebJobsStorage")] IAsyncCollector<string> startJobQueue,
        ILogger log)
    {
        log.LogInformation("StartJob function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        string jobId = Guid.NewGuid().ToString();

        await startJobQueue.AddAsync(jobId);

        return new OkObjectResult(new { jobId });
    }

    [FunctionName("FetchImages")]
    public static async Task<IActionResult> FetchImages(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "fetch-images/{jobId}")] HttpRequest req,
        string jobId,
        ILogger log)
    {
        log.LogInformation($"Fetching images for JobId: {jobId}");

        var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        var containerClient = blobServiceClient.GetBlobContainerClient("images");
        var blobs = containerClient.GetBlobsAsync(prefix: jobId);

        var imageUrls = new List<string>();
        await foreach (var blob in blobs)
        {
            var blobClient = containerClient.GetBlobClient(blob.Name);
            var imageUrl = blobClient.Uri.ToString();
            log.LogInformation($"Found image: {imageUrl}");
            imageUrls.Add(imageUrl);
        }

        log.LogInformation($"Total images found: {imageUrls.Count}");
        return new OkObjectResult(imageUrls);
    }
}