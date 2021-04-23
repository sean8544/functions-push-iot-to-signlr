using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;


using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;


namespace Company.Function
{
    public static class IotHubTriggerCSharp1
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("IotHubTriggerCSharp1")]
        public static void Run([IoTHubTrigger("messages/events", Connection = "IotHubEventHubString")]EventData message,    [SignalR(HubName = "chat")] IAsyncCollector<SignalRMessage> signalRMessages,ILogger log)
        {
             log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");

            var deviceid = message.SystemProperties["iothub-connection-device-id"].ToString();


            var dbstring = System.Environment.GetEnvironmentVariable("SQLConn");

            Telemetry tmsg = JsonConvert.DeserializeObject<Telemetry>(Encoding.UTF8.GetString(message.Body.Array));



            tmsg.funcsavedt = DateTime.Now;
            tmsg.deviceid = deviceid;

           



            // azure functions output binding, send iot data to signalr, then push to frontend web.
            signalRMessages.AddAsync(
                 new SignalRMessage
                 {
                     // newMessage is a function, web client should handle.
                     Target = "newMessage",
                     Arguments = new[] { new { sender = $"iot hub function from cloud-{DateTime.Now}", text = $"deviceid-{deviceid.Substring(0, 10)},temperature:{tmsg.temperature},humidity:{tmsg.humidity}" } }
                 });
        }

          [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "chat")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("messages")]
        public static Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] object message,
            [SignalR(HubName = "chat")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { message }
                });
        }


        public class Telemetry
        {
            public string deviceid { get; set; }

            public DateTime funcsavedt { get; set; }
            public double temperature { get; set; }

            public double humidity { get; set; }
        }
    }
}