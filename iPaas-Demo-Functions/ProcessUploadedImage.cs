using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageDetails;

namespace iPaaSDemoProj
{
    public static class ProcessUploadedImage
    {
        [FunctionName("ProcessUploadedImage")]
        public static async Task Run([BlobTrigger("images/{name}", Connection = "NewImageSourceStorage")]CloudBlockBlob myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Properties.Length} Bytes");

            // Download Image File from Blob Storage
            try
            {
                ImageMetadata imageData = new ImageMetadata();

                
                    string sourceStorage = Environment.GetEnvironmentVariable("NewImageSourceStorage");

                    CloudStorageAccount sourceStorageAccount = CloudStorageAccount.Parse(sourceStorage);
                    
                    string metaContainerName = Environment.GetEnvironmentVariable("ImageMetadataContainer");
                    CloudBlobClient metaBlobClient = sourceStorageAccount.CreateCloudBlobClient();
                    CloudBlobContainer metaContainer = metaBlobClient.GetContainerReference(metaContainerName);

                    //string metaname = name.Split('.').First() + ".json";
                    string metaname = Path.GetFileNameWithoutExtension(name) + ".json";
                    string ext = Path.GetExtension(name);

                    CloudBlockBlob metaBlob = metaContainer.GetBlockBlobReference(metaname);

                    string curi = metaBlob.StorageUri.ToString();
                    log.LogInformation("Container URI: " + curi);

                    string jsonMetaData = await metaBlob.DownloadTextAsync();

                    imageData = JsonConvert.DeserializeObject<ImageMetadata>(jsonMetaData);

                    //metaBlob.Download()

                    //imageData = await System.Text.Json.JsonSerializer.DeserializeAsync<ImageMetadata>(blobMetaMemStream);

                    log.LogInformation("Image Metadata issueType: " + imageData.issueType + " issueDescription: " + imageData.issueDescription);
                    


                using(MemoryStream blobMemStream = new MemoryStream())
                {

                    await myBlob.DownloadToStreamAsync(blobMemStream);

                    string destinationStorage = Environment.GetEnvironmentVariable("ValidImageDestStorage");

                    CloudStorageAccount destStorageAccount = CloudStorageAccount.Parse(destinationStorage);
                    CloudBlobClient destBlobClient = destStorageAccount.CreateCloudBlobClient();

                    byte[] byteData =blobMemStream.ToArray();
                    
                    log.LogInformation("Image Byte Array:" + byteData);

                    var client = new HttpClient();

                    // Request headers - replace this example key with your valid Prediction-Key.

                    client.DefaultRequestHeaders.Add("Prediction-Key", "124c2ec410304f499ee39fe957d5afb9");

                    // Prediction URL - replace this example URL with your valid Prediction URL.

                    string url = "https://eastus.api.cognitive.microsoft.com/customvision/v3.0/Prediction/3d81746b-d0f3-4216-a6f1-024c25f2c070/classify/iterations/Iteration7/image";

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

                        log.LogInformation("Uploaded Image is an Issue and is now being uploaded for further processing...");
                        
                        string destinationContainer = Environment.GetEnvironmentVariable("ValidImageDestContainer");
                        CloudBlobContainer destContainer = destBlobClient.GetContainerReference(destinationContainer);

                        await destContainer.CreateIfNotExistsAsync();

                        //string uri = destStorageAccount.BlobStorageUri.ToString();
                        //log.LogInformation("Storage URI: " + uri);
                 
                        CloudBlockBlob image = destContainer.GetBlockBlobReference(imageData.id + ext);

                        //string curi = image.StorageUri.ToString();
                        //log.LogInformation("Container URI: " + curi);

                        //Console.WriteLine("Length:" + byteData.Length);
                        //Console.WriteLine("ByteData:" + byteData);

                        //await image.UploadFromByteArrayAsync(byteData,0,byteData.Length);
                        blobMemStream.Position = 0;
                        await image.UploadFromStreamAsync(blobMemStream);
                    }
                    else if(imageData.probability > .75)
                    {
                        log.LogInformation("Uploaded Image is not an Issue, but was successfully identified as: " + imageData.tagName + ". Archiving for future reference....");

                        string destinationContainer = Environment.GetEnvironmentVariable("ArchiveImageDestContainer");
                        CloudBlobContainer destContainer = destBlobClient.GetContainerReference(destinationContainer);

                        await destContainer.CreateIfNotExistsAsync();

                        CloudBlockBlob image = destContainer.GetBlockBlobReference(imageData.id + ext);

                        blobMemStream.Position = 0;
                        await image.UploadFromStreamAsync(blobMemStream);
                    }
                    else
                    {
                        log.LogInformation("Uploaded Image is not an Issue and not identified successfully. Archiving for future reference....");

                        string destinationContainer = Environment.GetEnvironmentVariable("ArchiveImageDestContainer");
                        CloudBlobContainer destContainer = destBlobClient.GetContainerReference(destinationContainer);

                        await destContainer.CreateIfNotExistsAsync();

                        CloudBlockBlob image = destContainer.GetBlockBlobReference(imageData.id + ext);

                        blobMemStream.Position = 0;
                        await image.UploadFromStreamAsync(blobMemStream);

                    }

                    string newMetaJson = System.Text.Json.JsonSerializer.Serialize<ImageMetadata>(imageData);
                    CloudBlockBlob newMetaBlob = metaContainer.GetBlockBlobReference(imageData.id + ".json");
                    await newMetaBlob.UploadTextAsync(newMetaJson);

                    log.LogInformation("Removing Image from Upload Container");
                    await myBlob.DeleteIfExistsAsync();
                    await metaBlob.DeleteIfExistsAsync();

                }
            }
            catch(Exception ex){
                log.LogInformation($"Error! Something went wrong: {ex.Message}");

            }

        }

        public static ImageMetadata ProcessCustomVisionResults(String responseBody, ImageMetadata metadata)
        {
           ProcessUploadedImage.Root root = new ProcessUploadedImage.Root();

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
                        metadata.id = root.id;
                        metadata.probability = item.probability;
                        metadata.tagName = item.tagName;

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
