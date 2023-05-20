using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Polly;

namespace OrderItemsReserver
{
    public class ReserverFunctionWithRetry
    {
        private static IAsyncPolicy policy = Policy
            .Handle<Exception>()
            .RetryAsync(3);

        [FunctionName("ReserverFunctionWithRetry")]
        public async Task Run([ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] string myQueueItem,
            IBinder busBinder,
            IBinder blobBinder,
            ILogger log)
        {
            log.LogInformation("Start function.");
            await policy.ExecuteAsync(async () =>
                {
                    log.LogInformation("start attempt");
                    var outboundBlob = new BlobAttribute("orders/{DateTime}.json") { Connection = "BlobConnection" };
                    var blobClient = await blobBinder.BindAsync<BlobClient>(outboundBlob);

                    await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem)));
                });
            log.LogInformation("Finish function.");
        }
    }
}
