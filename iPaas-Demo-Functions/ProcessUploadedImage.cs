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
using PredictionDetails;

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
                using(MemoryStream blobMemStream = new MemoryStream())
                {

                    await myBlob.DownloadToStreamAsync(blobMemStream);

                    bool validImage = false;

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

                        validImage = ProcessCustomVisionResults(responseBody);

                        Console.WriteLine(responseBody);
                        
                    }

                    if(validImage)
                    {
                        log.LogInformation("Uploaded Image is an Issue and is now being uploaded for further processing...");
                        string destinationStorage = Environment.GetEnvironmentVariable("ValidImageDestStorage");
                        string destinationContainer = Environment.GetEnvironmentVariable("ValidImageDestContainer");

                        CloudStorageAccount destStorageAccount = CloudStorageAccount.Parse(destinationStorage);
                        CloudBlobClient destBlobClient = destStorageAccount.CreateCloudBlobClient();
                        CloudBlobContainer destContainer = destBlobClient.GetContainerReference(destinationContainer);

                        await destContainer.CreateIfNotExistsAsync();

                        //string uri = destStorageAccount.BlobStorageUri.ToString();
                        //log.LogInformation("Storage URI: " + uri);

                        CloudBlockBlob image = destContainer.GetBlockBlobReference(name);

                        //string curi = image.StorageUri.ToString();
                        //log.LogInformation("Container URI: " + curi);

                        //Console.WriteLine("Length:" + byteData.Length);
                        //Console.WriteLine("ByteData:" + byteData);

                        //await image.UploadFromByteArrayAsync(byteData,0,byteData.Length);
                        blobMemStream.Position = 0;
                        await image.UploadFromStreamAsync(blobMemStream);

                    }
                    else
                    {

                        log.LogInformation("Uploaded Image is not an Issue");

                    }

                    /*using(ZipArchive archive = new ZipArchive(blobMemStream))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            log.LogInformation($"Now processing {entry.FullName}");

                            //Replace all NO digits, letters, or "-" by a "-" Azure storage is specific on valid characters
                            string valideName = Regex.Replace(entry.Name,@"[^a-zA-Z0-9\-]","-").ToLower();

                            CloudBlockBlob blockBlob = container.GetBlockBlobReference(valideName);
                            using (var fileStream = entry.Open())
                            {
                                await blockBlob.UploadFromStreamAsync(fileStream);
                            }
                        }
                    }*/
                }
            }
            catch(Exception ex){
                log.LogInformation($"Error! Something went wrong: {ex.Message}");

            }

        }

        public static bool ProcessCustomVisionResults(String responseBody)
        {
           ProcessUploadedImage.Root root = new ProcessUploadedImage.Root();

           root = JsonConvert.DeserializeObject<Root>(responseBody);

           root.imageValid = false;
            
           foreach (var item in root.predictions)
            {
                //if ((item.tagName == "issues") && (item.probability > .75))
                if ((item.tagName == "issues") && (item.probability > .75))
                {
                    root.imageValid = true;
                    Prediction ValidPrediction = new Prediction
                    {
                        probability = item.probability,
                        tagId = item.tagId,
                        tagName = item.tagName
                    };

                    Console.WriteLine("Issue successfully identified with probablility: " + ValidPrediction.probability);
                    break;
                }
            }

            return root.imageValid;

        }

        public class Root
        {
            public string id { get; set; }
            public string project { get; set; }
            public string iteration { get; set; }
            public DateTime created { get; set; }
            public List<Prediction> predictions { get; set; }
            public bool imageValid { get; set; }
        }

    }
}
