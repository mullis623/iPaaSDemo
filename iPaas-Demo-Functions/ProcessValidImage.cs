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
    public static class ProcessValidImage
    {
        [FunctionName("ProcessValidImage")]
        [return: ServiceBus("validimagequeue", Connection = "ServiceBusConnection")]
        //public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        public static async Task<ImageMetadata> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string id = req.Query["id"];
            string issueType = req.Query["issueType"];
            string issueDescription = req.Query["issueDescription"];
            string geoLatCoordinate = req.Query["geoLatCoordinate"];
            string geoLongCoordinate = req.Query["geoLongCoordinate"];
            string uploadUserName = req.Query["uploadUserName"];
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            id = id ?? data?.id;
            //issueType = issueType ?? data?.issueType;
            //issueDescription = issueDescription ?? data?.issueDescription;
            //geoLatCoordinate = geoLatCoordinate ?? data?.geoLatCoordinate;
            //geoLongCoordinate = geoLongCoordinate ?? data?.geoLongCoordinate;
            //uploadUserName = uploadUserName ?? data?.uploadUserName;

            ImageMetadata imageData = new ImageMetadata();
            string sourceStorage = Environment.GetEnvironmentVariable("NewImageSourceStorage");
            string destStorage = Environment.GetEnvironmentVariable("ValidImageDestStorage");
            string metaContainerName = Environment.GetEnvironmentVariable("ImageMetadataContainer");

            string metaname = id + ".json";

            try
            {   

                CloudStorageAccount sourceStorageAccount = CloudStorageAccount.Parse(sourceStorage);
                CloudBlobClient metaBlobClient = sourceStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer metaContainer = metaBlobClient.GetContainerReference(metaContainerName);
                CloudBlockBlob metaBlob = metaContainer.GetBlockBlobReference(metaname);
                bool metaBlobExists = await metaBlob.ExistsAsync();

                if(metaBlobExists)
                {
                    string jsonMetaData = await metaBlob.DownloadTextAsync();
                    imageData = JsonConvert.DeserializeObject<ImageMetadata>(jsonMetaData);
                    log.LogInformation("Image Metadata timestamp: " + imageData.timestamp + " uploadedFileName: " + imageData.uploadedFileName);
                }
                else
                {
                    log.LogInformation("No Metadata exists for uploaded image. Exiting Process.");
                    throw new System.InvalidOperationException("Image has not yet been validated");
                }

                string blobName = imageData.uploadedFileName;
                
                string imageContainerName = Environment.GetEnvironmentVariable("ImageContainerName");
                CloudBlobClient imageBlobClient = sourceStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer imageContainer = imageBlobClient.GetContainerReference(imageContainerName);
                CloudBlockBlob imageBlob = imageContainer.GetBlockBlobReference(blobName);
                bool imageBlobExists = await imageBlob.ExistsAsync();
                
                if(imageBlobExists)
                {
                    log.LogInformation("Moving Blob: " + blobName + " to ProcessedImages container for further actions.");
                    CloudStorageAccount destStorageAccount = CloudStorageAccount.Parse(destStorage);
                    CloudBlobClient destBlobClient = destStorageAccount.CreateCloudBlobClient();
                    string destContainerName = Environment.GetEnvironmentVariable("ValidImageDestContainer");                    
                    CloudBlobContainer destContainer = destBlobClient.GetContainerReference(destContainerName);
                    await destContainer.CreateIfNotExistsAsync();
                    
                    string ext = Path.GetExtension(blobName);
                    CloudBlockBlob newImageBlob = destContainer.GetBlockBlobReference(imageData.id + ext);
                    await newImageBlob.StartCopyAsync(imageBlob);
                    string blobUrl = newImageBlob.StorageUri.PrimaryUri.AbsoluteUri;
                    log.LogInformation("Blob Url: " + blobUrl);
                    await imageBlob.DeleteIfExistsAsync();

                    imageData.blobUrl = blobUrl;
                    imageData.issueType = issueType ?? data?.issueType;
                    imageData.issueDescription = issueDescription ?? data?.issueDescription;
                    imageData.geoLatCoordinate = geoLatCoordinate ?? data?.geoLatCoordinate;
                    imageData.geoLongCoordinate = geoLongCoordinate ?? data?.geoLongCoordinate;
                    imageData.uploadUserName = uploadUserName ?? data?.uploadUserName;

                    string metaJson = System.Text.Json.JsonSerializer.Serialize<ImageMetadata>(imageData);
                    await metaBlob.UploadTextAsync(metaJson);

                }
                else
                {
                    throw new FileNotFoundException("Blob: " + blobName + " does not exist in container: " + imageContainer + ".");
                }

            }
            catch(Exception ex)
            {
                log.LogInformation($"Error! Something went wrong: {ex.Message}");
            }

            //string responseMessage = "Ok";
            return imageData;

            //return new OkObjectResult(responseMessage);
        }
    }
}
