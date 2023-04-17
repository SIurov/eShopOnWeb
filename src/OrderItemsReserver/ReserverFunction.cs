using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace OrderItemsReserver
{
    public static class ReserverFunction
    {
        [FunctionName("ReserverFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Blob("module4/{DateTime}.json", Connection="BlobConnection")] BlobClient blobClient,
            ILogger log)
        {
            log.LogInformation("Start function.");
            await blobClient.UploadAsync(req.Body);
            log.LogInformation("Finish function.");

            return new OkResult();
        }
    }
}
