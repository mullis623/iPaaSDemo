using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleAp
{
    public static class Program
    {
        //This console app invokes the CustomVision service providing it a locally stored image. The CustomVision service returns a JSON object with the results of the prediction for the provided image.
        //This logic will have to be moved to an Azure Function with a Blob trigger.  
        public static void Main()
        {
            Console.Write("Enter image file path: ");
            string imageFilePath = Console.ReadLine();

            MakePredictionRequest(imageFilePath).Wait();

            Console.WriteLine("\n\nHit ENTER to exit...");
            Console.ReadLine();
        }

        public static async Task MakePredictionRequest(string imageFilePath)
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

                Program.Root root = new Program.Root();

                await ProcessCustomVisionResults(response, root);
 
                Console.WriteLine(await response.Content.ReadAsStringAsync());                     
            }
        }

        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }


        public static async Task ProcessCustomVisionResults(HttpResponseMessage response, Root root)
        {
           root = JsonConvert.DeserializeObject <Root> (await response.Content.ReadAsStringAsync());

            root.imageValid = false;
            
           foreach (var item in root.predictions)
            {
                if ((item.tagName == "issues") && (item.probability > .75))
                {
                    root.imageValid = true;
                    break;
                }
            }

           //if root.imageValid = true, then write this to the images container which will trigger the Logic App. 
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Prediction
        {
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
        }
    }
}