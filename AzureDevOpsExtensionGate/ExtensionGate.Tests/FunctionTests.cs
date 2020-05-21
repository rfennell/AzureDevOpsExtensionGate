using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace FunctionApp.Tests
{
    public class FunctionsTests
    {
        private readonly ILogger logger = TestFactory.CreateLogger();
        private string pat = "<ENTER A PAT>";

        [Fact]
        public async void Cannot_run_function_with_no_params()
        {
            // arrange
           var p = new Payload()
            {
                pat = "",
                instance = "",
                taskguid = "",
                version = ""
            };

            // act
            var request = TestFactory.CreateHttpRequest(p);
            var response = (BadRequestObjectResult)await ExtensionGate.Run(request, logger);
            
            // assert
            Assert.Equal("Please pass a Azure DevOps instance name, PAT and TaskGuid in the request body", response.Value);
        }

        [Fact]
        public async void Can_get_status_of_task()
        {
            // arrange
            var p = new Payload()
            {
                pat = this.pat,
                instance = "richardfennell",
                taskguid = "6b42ca94-dc11-43dd-8b25-fcbf378b6b89",
                version = "2.2.11"
            };

            // act
            var request = TestFactory.CreateHttpRequest(p);
            var response = (OkObjectResult)await ExtensionGate.Run(request, logger);
            
            // assert
            Assert.Equal("{ Deployed = True }", response.Value.ToString());
        }
     
    }
}
