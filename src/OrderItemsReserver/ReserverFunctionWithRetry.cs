using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

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
            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    var outboundBlob = new BlobAttribute("orders/{DateTime}.json") { Connection = "BlobConnection" };
                    var blobClient = await blobBinder.BindAsync<BlobClient>(outboundBlob);
                    log.LogInformation("Start function.");
                    await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem)));
                    log.LogInformation("Finish function.");
                });
            }
            catch (Exception ex)
            {
                var outboundBus = new ServiceBusAttribute("failedorders") { Connection = "ServiceBusConnection" };
                var collector = await busBinder.BindAsync<IAsyncCollector<string>>(outboundBus).ConfigureAwait(false);
                await collector.AddAsync(myQueueItem);
            }
        }
    }
}
