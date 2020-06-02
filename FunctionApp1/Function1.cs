using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;

namespace FunctionApp1
{
    public static class Function1
    {
        [FunctionName("HttpTrigger_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get")]HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            // ProcessRequest requestData = await req.Content.ReadAsAsync<ProcessRequest>();
            ProcessRequest requestData = new ProcessRequest() { data = "andres" };
            // Starting a new orchestrator with request data
            string instanceId = await starter.StartNewAsync("HttpTrigger_Orchestrator", requestData);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var response = starter.CreateCheckStatusResponse(req, instanceId);

            // I specify a response interval so the Logic App doesn't check the status
            // until after 10 seconds has passed. If work will be longer you can change this 
            // value as needed.
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            return response;
        }

        [FunctionName("HttpTrigger_Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // In this case my orchestrator is only calling a single function - HttpTrigger_DoWork
            outputs.Add(await context.CallActivityAsync<string>("HttpTrigger_DoWork", context.GetInput<ProcessRequest>()));

            return outputs;
        }

        [FunctionName("HttpTrigger_DoWork")]
        public static string DoWork([ActivityTrigger] ProcessRequest requestData, ILogger log)
        {
            log.LogInformation($"Doing work on data {requestData.data}.");
            Thread.Sleep(TimeSpan.FromSeconds(3));
            return "some response data";
        }
    }
    public class ProcessRequest
    {
        public string data { get; set; }
    }
}
