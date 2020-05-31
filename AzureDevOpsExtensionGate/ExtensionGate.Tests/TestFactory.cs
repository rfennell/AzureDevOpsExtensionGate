using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;

namespace FunctionApp.Tests
{
  
    public class TestFactory
    {
        public static string GetPAT()
        {
            var json = File.ReadAllText("appsettings.json");
            var jObject = JObject.Parse(json);
            return jObject.SelectToken("PAT").Value<string>();
        }

        public static DefaultHttpRequest CreateHttpRequest(Object payload)
        {
            var request = new DefaultHttpRequest(new DefaultHttpContext());
            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
            request.Body = new MemoryStream(byteArray);
            return request;
        }

        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;

            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }

            return logger;
        }
    }
}
