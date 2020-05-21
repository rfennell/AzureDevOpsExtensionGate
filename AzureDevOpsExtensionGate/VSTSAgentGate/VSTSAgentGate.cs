using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FunctionApp
{
    public static class VSTSAgentGate
    {
        [FunctionName("VSTSAgentGate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("VSTSAgentGate HTTP trigger function processed a request.");


            // Get request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string pat = data?.pat;
            string instance = data?.instance;
            string taskGuid = data?.taskguid;
            string version = data?.version;

            if (string.IsNullOrEmpty(pat) ||
                string.IsNullOrEmpty(instance) ||
                string.IsNullOrEmpty(taskGuid) ||
                string.IsNullOrEmpty(version))
            {
                log.LogInformation("Invalid parameters passed");
                return new BadRequestObjectResult("Please pass a Azure DevOps instance name, PAT and TaskGuid in the request body");
            }

            try
            {
                log.LogInformation($"Requesting deployed task with GUID {taskGuid} to see if it is version {version} from Azure DevOps instance {instance}");

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", pat))));

                client.BaseAddress = new Uri($"https://dev.azure.com/{instance}/_apis/distributedtask/tasks/{taskGuid}");
                var result = await client.GetAsync("");
                string resultContent = await result.Content.ReadAsStringAsync();

                var o = JObject.Parse(resultContent);
                var count = (int)o.SelectToken("count");

                log.LogInformation($"There are {count} versions of the task present");

                var isDeployed = false;
                for (int i = 0; i < count; i++)
                {
                    log.LogInformation($"Checking {o.SelectToken("value")[i].SelectToken("contributionVersion").ToString()}");
                    isDeployed = isDeployed || version.Equals(o.SelectToken("value")[i].SelectToken("contributionVersion").ToString()); ;
                }

                var returnValue = new { Deployed = isDeployed };
                log.LogInformation($"The response payload is {returnValue}");

                return (ActionResult)new OkObjectResult(returnValue);
            }
            catch (Exception ex)
            {
                return (ActionResult)new OkObjectResult($"Exception thrown making API call or Parsing result. {ex.Message}");
            }

        }
    }
}

