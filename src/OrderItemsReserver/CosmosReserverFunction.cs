using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class CosmosReserverFunction
    {
        [FunctionName("CosmosReserverFunction")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "Module5Database",
                containerName: "Orders",
                Connection = "CosmosDBConnection")]out dynamic document,
            ILogger log)
        {
            log.LogInformation("Start function.");

            var body = new StreamReader(req.Body).ReadToEndAsync().Result;
            document = JsonConvert.DeserializeObject<dynamic>(body);
            document["id"] = Guid.NewGuid().ToString();


            //document = new { body[""] }
            log.LogInformation("Finish function.");

            return new OkResult();
        }
    }
}
