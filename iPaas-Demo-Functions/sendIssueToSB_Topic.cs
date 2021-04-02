using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
//using Newtonsoft.Json;
using ImageDetails;

namespace iPaas_Demo_Functions
{
    public static class sendIssueToSB_Topic
    {

        [FunctionName("sendIssueToSB_Topic")]
        public static async Task Run([CosmosDBTrigger(
            databaseName: "ImageMetadataDB",
            collectionName: "ImageDetailsContainer",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
        {
            ImageMetadata imageData = new ImageMetadata();

            if (input != null && input.Count > 0)
            {
                imageData = (ImageMetadata)(dynamic)input[0];

                log.LogInformation("Documents modified " + input.Count);
                //log.LogInformation("First document Id " + imageData.id);
                //log.LogInformation("Doc Details " + imageData.addressDetails.adminDistrict);
                //log.LogInformation("Doc Intersection Details " + imageData.addressDetails.intersection.baseStreet);

                await SendMessageAsync(imageData);
            }

        }

        static async Task SendMessageAsync(ImageMetadata imageDetails)
        {
            string issueType = imageDetails.issueType;
            string imageDetailsJson = System.Text.Json.JsonSerializer.Serialize<ImageMetadata>(imageDetails);
            var message = new Message(Encoding.UTF8.GetBytes(imageDetailsJson));
            message.UserProperties.Add("issueType", issueType);
            message.ContentType = "application/json";
            message.Label = issueType;

            string ServiceBusConnectionString = Environment.GetEnvironmentVariable("TopicServiceBusConnection");
            string TopicName = Environment.GetEnvironmentVariable("ServiceBusTopic");
            TopicClient topicClient = new TopicClient(ServiceBusConnectionString, TopicName);

            await topicClient.SendAsync(message);
            Console.WriteLine($"Sent Message:: Label: {message.Label}");
        }
    }
}
