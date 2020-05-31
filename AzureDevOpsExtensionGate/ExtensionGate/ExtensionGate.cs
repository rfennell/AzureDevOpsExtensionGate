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
using System.Linq;

namespace FunctionApp
{
    public static class ExtensionGate
    {
        private static HttpClient CreateHttpClient(string pat)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                    string.Format("{0}:{1}", "", pat))));

            return client;
        }

        [FunctionName("CheckExtensionAvailable")]
        public static async Task<IActionResult> RunCheckExtensionAvailable(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CheckExtensionAvailable HTTP trigger function processed a request.");


            // Get request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string pat = data?.pat;
            string organization = data?.organization;
            string taskGuid = data?.taskguid;
            string version = data?.version;

            if (string.IsNullOrEmpty(pat) ||
                string.IsNullOrEmpty(organization) ||
                string.IsNullOrEmpty(taskGuid) ||
                string.IsNullOrEmpty(version))
            {
                log.LogInformation("Invalid parameters passed");
                return new BadRequestObjectResult("Please pass a Azure DevOps instance name, PAT and TaskGuid in the request body");
            }

            try
            {
                log.LogInformation($"Requesting deployed task with GUID {taskGuid} to see if it is version {version} from Azure DevOps instance {organization}");

                var client = CreateHttpClient(pat);

                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/_apis/distributedtask/tasks/{taskGuid}");
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

                var returnValue = new { deployed = isDeployed };
                log.LogInformation($"The response payload is {returnValue}");

                return (ActionResult)new OkObjectResult(returnValue);
            }
            catch (Exception ex)
            {
                return (ActionResult)new OkObjectResult($"Exception thrown making API call or Parsing result. {ex.Message}");
            }

        }

        [FunctionName("CheckWorkItemInCorrectState")]
        public static async Task<IActionResult> RunCheckWorkItemInCorrectState(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            log.LogInformation("CheckWorkItemInCorrectState HTTP trigger function processed a request.");


            // Get request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string pat = data?.pat;
            string organization = data?.organization;
            string buildId = data?.buildid;
            string project = data?.project;
            string statesString = data?.states;

            if (string.IsNullOrEmpty(pat) ||
                string.IsNullOrEmpty(organization) ||
                string.IsNullOrEmpty(buildId) ||
                string.IsNullOrEmpty(statesString) ||
                string.IsNullOrEmpty(project))
            {
                log.LogInformation("Invalid parameters passed");
                return new BadRequestObjectResult("Please pass a Azure DevOps instance name, PAT and other parameters in the request body");
            }

            try
            {
                log.LogInformation($"The valid states to check for are [{statesString}");
                var states = statesString.Split(',');

                log.LogInformation($"Getting the WI associated with build {buildId}");

                var client = CreateHttpClient(pat);

                // get the list of WI               
                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/{project}/_apis/build/builds/{buildId}/workitems?api-version=5.1");
                var result = await client.GetAsync("");
                string resultContent = await result.Content.ReadAsStringAsync();

                var o = JObject.Parse(resultContent);
                var count = (int)o.SelectToken("count");
                var value = o.SelectToken("value");

                log.LogInformation($"There are {count} workitems associated with the build");
                var canProceed = true;

                foreach (var item in value)
                {
                    var url = item.SelectToken("url").ToString();
                    var id = item.SelectToken("id").ToString();

                    var itemClient = CreateHttpClient(pat);
                    itemClient.BaseAddress = new Uri(url);
                    var itemResult = await itemClient.GetAsync("");
                    var itemResultContent = await itemResult.Content.ReadAsStringAsync();

                    o = JObject.Parse(itemResultContent);
                    string state = o.SelectToken("fields.['System.State']").ToString();

                    log.LogInformation($"The WI {id} is in the state {state}");

                    if (!states.Contains(state))
                    {
                        // if we see any of these we can't release
                        canProceed = false;
                        break;
                    }

                }
                var returnValue = new { canProceed = canProceed };
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
