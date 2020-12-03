using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using ImageDetails;

namespace iPaas_Demo_Functions
{
    public static class ValidateImage
    {
        [FunctionName("ValidateImage")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string responseMessage;

            string blobName = req.Query["blobName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            blobName = blobName ?? data?.blobName;

            string sourceStorage = Environment.GetEnvironmentVariable("NewImageSourceStorage");

            CloudStorageAccount sourceStorageAccount = CloudStorageAccount.Parse(sourceStorage);
                
            string imageContainerName = Environment.GetEnvironmentVariable("ImageContainerName");
            CloudBlobClient imageBlobClient = sourceStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer imageContainer = imageBlobClient.GetContainerReference(imageContainerName);
            CloudBlockBlob imageBlob = imageContainer.GetBlockBlobReference(blobName);

            ImageMetadata imageData = new ImageMetadata(){
                timestamp = DateTime.Now,
                uploadedFileName = blobName
            };            

            using(MemoryStream blobMemStream = new MemoryStream())
            {

                await imageBlob.DownloadToStreamAsync(blobMemStream);

                
                byte[] byteData =blobMemStream.ToArray();
                
                log.LogInformation("Image Byte Array:" + byteData);

                var client = new HttpClient();

                // Request headers - replace this example key with your valid Prediction-Key.

                client.DefaultRequestHeaders.Add("Prediction-Key", Environment.GetEnvironmentVariable("CustomVisionPredictionKey"));

                // Prediction URL - replace this example URL with your valid Prediction URL.

                string rootUrl = Environment.GetEnvironmentVariable("CustomVisionRootUrl");
                string iteration = Environment.GetEnvironmentVariable("CustomVisionIteration");
                string url = rootUrl + iteration + "/image";

                HttpResponseMessage response;

                // Request body. Try this sample with a locally stored image.                    

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(url, content);

                    string responseBody = await response.Content.ReadAsStringAsync();

                    imageData = ProcessCustomVisionResults(responseBody, imageData);

                    Console.WriteLine(responseBody);
                    
                }

                if(imageData.isValidatedIssue)
                {

                    log.LogInformation("Uploaded Image has been identified as an issue");

                    string metaContainerName = Environment.GetEnvironmentVariable("ImageMetadataContainer");
                    CloudBlobClient metaBlobClient = sourceStorageAccount.CreateCloudBlobClient();
                    CloudBlobContainer metaContainer = metaBlobClient.GetContainerReference(metaContainerName);
                    await metaContainer.CreateIfNotExistsAsync();
                    
                    string newMetaJson = System.Text.Json.JsonSerializer.Serialize<ImageMetadata>(imageData);
                    CloudBlockBlob newMetaBlob = metaContainer.GetBlockBlobReference(imageData.id + ".json");
                    await newMetaBlob.UploadTextAsync(newMetaJson);

                    responseMessage = imageData.id;

                }
                else
                {
                    log.LogInformation("Uploaded Image was not identified as an Issue. Removing image from upload container...");
                    await imageBlob.DeleteIfExistsAsync();
                    responseMessage = "-1";

                }

            }

            return new OkObjectResult(responseMessage);
        }

        public static ImageMetadata ProcessCustomVisionResults(String responseBody, ImageMetadata metadata)
        {
           ValidateImage.Root root = new ValidateImage.Root();

           root = JsonConvert.DeserializeObject<Root>(responseBody);
            
           foreach (var item in root.predictions)
           {
                //if ((item.tagName == "issues") && (item.probability > .75))
                if ((item.tagName == "issues") && (item.probability > .75))
                {
                    metadata.id = root.id;
                    metadata.probability = item.probability;
                    metadata.tagName = item.tagName;
                    metadata.isValidatedIssue = true;

                    Console.WriteLine("Issue successfully identified with probablility: " + metadata.probability);
                    break;
                }
                
            }

            if(!(metadata.isValidatedIssue))
            {
                foreach (var item in root.predictions)
                {    
                    if(item.probability > .75)
                    {
                        Console.WriteLine("Item successfully identified as: " + metadata.tagName + " with probablility: " + metadata.probability);
                        break;
                    }
                    
                }
            }

            return metadata;

        }

        public class Root
        {
            public string id { get; set; }
            public string project { get; set; }
            public string iteration { get; set; }
            public DateTime created { get; set; }
            public List<ImageMetadata> predictions { get; set; }
            public bool isIssue { get; set; }
        }
    }
}
