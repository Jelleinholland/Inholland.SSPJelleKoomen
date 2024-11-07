using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Inholland.SSPJelleKoomen
{
    public static class StatusFunctions
    {
        [FunctionName("GetJobStatus")]
        public static async Task<IActionResult> GetJobStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "job-status/{jobId}")] HttpRequest req,
            string jobId,
            ILogger log)
        {
            log.LogInformation($"Fetching status for JobId: {jobId}");

            var status = await GetStatusAsync(jobId);
            if (status == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(status);
        }

        public static async Task SaveStatusAsync(string jobId, string status)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("JobStatus");

            await table.CreateIfNotExistsAsync();

            var statusEntity = new StatusEntity(jobId, status);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(statusEntity);
            await table.ExecuteAsync(insertOrMergeOperation);
        }

        private static async Task<StatusEntity> GetStatusAsync(string jobId)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("JobStatus");

            var retrieveOperation = TableOperation.Retrieve<StatusEntity>("JobStatus", jobId);
            var result = await table.ExecuteAsync(retrieveOperation);
            return result.Result as StatusEntity;
        }
    }

    public class StatusEntity : TableEntity
    {
        public StatusEntity(string jobId, string status)
        {
            PartitionKey = "JobStatus";
            RowKey = jobId;
            Status = status;
        }

        public StatusEntity() { }

        public string Status { get; set; }
    }
}