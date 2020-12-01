using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using ConsoleApp.Models;
using ImageDetails;


namespace ConsoleApp
{
    public static class Program
    {
        
        //This console app invokes the CustomVision service providing it a locally stored image. The CustomVision service returns a JSON object with the results of the prediction for the provided image.
        //This logic will have to be moved to an Azure Function with a Blob trigger.  
        public static void Main()
        {

            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();

            string environment = "Prod";
            string sectionName = "StorageAccountConfig:" + environment; 

            IConfigurationRoot configuration = builder.Build();
            var strgConfig = new AzureStorageConfig();
            configuration.GetSection(sectionName).Bind(strgConfig);
            
            ImageMetadata imageData = new ImageMetadata();
            imageData.timestamp = DateTime.Now;
            //imageData.uploadUserName = "shmulli";
            //imageData.geoLatCoordinate =  40.751421;
            //imageData.geoLongCoordinate = -73.991669;

            string imageFilePath = null;

            while(imageFilePath == null)
            {
                Console.Write("Enter image file path: ");
                imageFilePath = Console.ReadLine();

                if(!(File.Exists(imageFilePath)))
                {
                    imageFilePath = null;
                    Console.WriteLine("Image Path entered does not exist.  Please try again.");
                }    
            }

            Console.WriteLine("\r\nChoose an issue type to associate with image:");
            Console.WriteLine("1) RoadDamage-Pothole");
            Console.WriteLine("2) RoadDamage-Crack");
            Console.WriteLine("3) Graffiti");
            Console.WriteLine("4) UtilityInfrastructure");
            Console.Write("\r\nEnter the number for your issue type: ");

            switch (Console.ReadLine())
            {
                case "1":
                    imageData.issueType = "RoadDamage-Pothole";
                    break;
                case "2":
                    imageData.issueType = "RoadDamage-Crack";
                    break;
                case "3":
                    imageData.issueType = "Graffiti";
                    break;
                case "4":
                    imageData.issueType = "UtilityInfrastructure";
                    break;
                default:
                    imageData.issueType = "Not Reported";
                    break;
            }

            Console.Write("Enter issue Description: ");
            imageData.issueDescription = Console.ReadLine();

            //Console.WriteLine(imageFilePath);
            //Console.WriteLine(imageData.issueType);
            //Console.WriteLine(imageData.issueDescription);

            Console.Write("Enter Latitude Coordinate of Issue: ");
            string latCoordinate = Console.ReadLine();
            imageData.geoLatCoordinate = double.Parse(latCoordinate);

            Console.Write("Enter Longitude Coordinate of Issue: ");
            string longCoordinate = Console.ReadLine();
            imageData.geoLongCoordinate = double.Parse(longCoordinate);

            Console.Write("Enter your username: ");
            imageData.uploadUserName = Console.ReadLine();

            TriggerUploadToStorage(imageData, imageFilePath, strgConfig).Wait();

            Console.WriteLine("\n\nHit ENTER to exit...");
            Console.ReadLine();

        }

        public static async Task<bool> TriggerUploadToStorage(ImageMetadata metadata, string filePath, AzureStorageConfig _storageConfig)
        {

            string fileNameNoExtension = Path.GetFileNameWithoutExtension(filePath);
            string fileName = Path.GetFileName(filePath);
            string urlPrefix;

            if(_storageConfig.AccountName == "devstoreaccount1")
            {
                urlPrefix = "http://127.0.0.1:10000/" + _storageConfig.AccountName + "/";
            }
            else
            {
                urlPrefix = "https://" + _storageConfig.AccountName + ".blob.core.windows.net/";
            }

            // Create a URI to the blob
            Uri metaBlobUri = new Uri(urlPrefix +
                                  _storageConfig.MetaContainer +
                                  "/" + fileNameNoExtension + ".json");

            Uri imageBlobUri = new Uri(urlPrefix +
                                  _storageConfig.ImageContainer +
                                  "/" + fileName);

            //Console.WriteLine("UploadURL: " + blobUri);

            // Create StorageSharedKeyCredentials object by reading
            // the values from the configuration (appsettings.json)
            StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create the blob client.
            CloudBlockBlob metaBlobClient = new CloudBlockBlob(metaBlobUri, storageCredentials);
            string metaJson = System.Text.Json.JsonSerializer.Serialize<ImageMetadata>(metadata);
            await metaBlobClient.UploadTextAsync(metaJson);
            
            CloudBlockBlob imageBlobClient = new CloudBlockBlob(imageBlobUri, storageCredentials);

            FileStream imageStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await imageBlobClient.UploadFromStreamAsync(imageStream);

            return await Task.FromResult(true);
        }

        /*public static async Task<bool> UploadToStorage(string contentToUpload, string fileName, AzureStorageConfig _storageConfig)
        {
            // Create a URI to the blob
            Uri blobUri = new Uri("https://" +
                                  _storageConfig.AccountName +
                                  ".blob.core.windows.net/" +
                                  _storageConfig.ImageContainer +
                                  "/" + fileName);

            //Console.WriteLine("UploadURL: " + blobUri);

            // Create StorageSharedKeyCredentials object by reading
            // the values from the configuration (appsettings.json)
            StorageSharedKeyCredential storageCredentials =
                new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create the blob client.
            BlobClient blobClient = new BlobClient(blobUri, storageCredentials);

            if(File.Exists(contentToUpload))
            {
                FileStream imageStream = new FileStream(contentToUpload, FileMode.Open, FileAccess.Read);
                await blobClient.UploadAsync(imageStream);
            }
            else
            {
                await blobClient.UploadAsync(contentToUpload);
            }

            return await Task.FromResult(true);
        }*/

        /*public static async Task MakePredictionRequest(string imageFilePath)
        {
            var client = new HttpClient();

            // Request headers - replace this example key with your valid Prediction-Key.

            client.DefaultRequestHeaders.Add("Prediction-Key", "124c2ec410304f499ee39fe957d5afb9");

            // Prediction URL - replace this example URL with your valid Prediction URL.

            string url = "https://eastus.api.cognitive.microsoft.com/customvision/v3.0/Prediction/3d81746b-d0f3-4216-a6f1-024c25f2c070/classify/iterations/Iteration7/image";

            HttpResponseMessage response;

            // Request body. Try this sample with a locally stored image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);

                string responseBody = await response.Content.ReadAsStringAsync();

                ProcessCustomVisionResults(responseBody, imageFilePath);

                Console.WriteLine(responseBody);

                
            }
        }

        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }


        public static async void ProcessCustomVisionResults(String responseBody, String imagePath)
        {
           Program.Root root = new Program.Root();

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

            

            string fileName = Path.GetFileName(imagePath);
            //Console.WriteLine("FileName: " + fileName);
            FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            await UploadFileToStorage(imageStream, fileName, strgConfig);

            //if root.imageValid = true, then write this to the images container which will trigger the Logic App. 
        }

        
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        /*public class Prediction
        /{
            public double probability { get; set; }
            public string tagId { get; set; }
            public string tagName { get; set; }

        }
        public class Root
        {
            public string id { get; set; }
            public string project { get; set; }
            public string iteration { get; set; }
            public DateTime created { get; set; }
            public List<Prediction> predictions { get; set; }
            public bool imageValid { get; set; }
        }*/
    }
}