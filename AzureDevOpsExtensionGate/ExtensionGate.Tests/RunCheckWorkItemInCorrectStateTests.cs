using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;
using System.Configuration;

namespace FunctionApp.Tests
{
    public class RunCheckWorkItemInCorrectState
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void Cannot_run_function_with_no_params()
        {
            // arrange
           var p = new 
            {
                pat = "",
               organization = "",
                taskguid = "",
                version = "",
                states = ""
            };

            // act
            var request = TestFactory.CreateHttpRequest(p);
            var response = (BadRequestObjectResult)await ExtensionGate.RunCheckWorkItemInCorrectState(request, logger);
            
            // assert
            Assert.Equal("Please pass a Azure DevOps instance name, PAT and other parameters in the request body", response.Value);
        }

        [Fact]
        public async void Can_get_check_the_state_of_associated_workitems()
        {
            // arrange
            var p = new 
            {
                pat = TestFactory.GetPAT(),
                organization = "richardfennell",
                project = "GitHub",
                buildid = "8112",
                states = "Done, Rejected, Closed"
            };

            // act
            var request = TestFactory.CreateHttpRequest(p);
            var response = (OkObjectResult)await ExtensionGate.RunCheckWorkItemInCorrectState(request, logger);
            
            // assert
            Assert.Equal("{ canProceed = False }", response.Value.ToString());
        }
     
    }
}
