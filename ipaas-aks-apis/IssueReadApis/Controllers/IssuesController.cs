//using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
//using Newtonsoft.Json;
using ImageDetails;

namespace IssueReadApis.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IssuesController : ControllerBase
    {

        private readonly ILogger<IssuesController> _logger;

        private readonly IConfiguration _configuration;

        private readonly string _cosmosEndpoint;
        private readonly string _cosmosKey;
        private readonly string _cosmosDBId;
        private readonly string _cosmosContainerId;
        public IssuesController(ILogger<IssuesController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosEndpoint = _configuration["CosmosDBEndpoint"];
            _cosmosKey = _configuration["CosmosDBReadOnlyKey"];
            _cosmosDBId = _configuration["CosmosDBId"];
            _cosmosContainerId = _configuration["CosmosDBContainerId"];
        }

        [HttpGet("getbyid/{id}")]
        public async Task<ImageMetadata> GetById(string id)
        {
            ImageMetadata imageData = new ImageMetadata();

            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            List<ImageMetadata> results = new List<ImageMetadata>();

            _logger.LogInformation("Cosmos Endpoint: " + _cosmosEndpoint);

            using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
            {
                Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);
                
                using (FeedIterator<ImageMetadata> resultSetIterator = container.GetItemQueryIterator<ImageMetadata>(query))
                {
                    while (resultSetIterator.HasMoreResults)
                    {
                        FeedResponse<ImageMetadata> response = await resultSetIterator.ReadNextAsync();
                        results.AddRange(response);
                        if (response.Diagnostics != null)
                        {
                            _logger.LogInformation($"\nQueryWithSqlParameters Diagnostics: {response.Diagnostics.ToString()}");
                        }
                    }
                }
            }

            _logger.LogInformation($"\nResult Count: {results.Count.ToString()}");

            if(results.Count > 0)
            {
                imageData = results.First();
            }

            return imageData;         

        }


        [HttpGet("getbyissuetype/{issueType}")]
        public async Task<List<ImageMetadata>> GetIssuesForIssueType(string issueType)
        {
            ImageMetadata imageData = new ImageMetadata();

            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.issueType = @issueType")
                .WithParameter("@issueType", issueType);

            List<ImageMetadata> results = new List<ImageMetadata>();
            using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
            {
                Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);
                
                using (FeedIterator<ImageMetadata> resultSetIterator = container.GetItemQueryIterator<ImageMetadata>(query))
                {
                    while (resultSetIterator.HasMoreResults)
                    {
                        FeedResponse<ImageMetadata> response = await resultSetIterator.ReadNextAsync();
                        results.AddRange(response);
                        if (response.Diagnostics != null)
                        {
                            _logger.LogInformation($"\nQueryWithSqlParameters Diagnostics: {response.Diagnostics.ToString()}");
                        }
                    }

                }                

            }

            _logger.LogInformation($"\nResult Count: {results.Count.ToString()}");
            
            return results;
           
        }

        [HttpGet("getbypostalcode/{postalCode}")]
        public async Task<List<ImageMetadata>> GetIssuesByPostalCode(string postalCode)
        {
            ImageMetadata imageData = new ImageMetadata();

            _logger.LogInformation("Querying results for Postal Code: " + postalCode);

            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.addressDetails.postalCode = @postalCode")
                .WithParameter("@postalCode", postalCode);

            List<ImageMetadata> results = new List<ImageMetadata>();
            using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
            {
                Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);
                
                using (FeedIterator<ImageMetadata> resultSetIterator = container.GetItemQueryIterator<ImageMetadata>(query))
                {
                    while (resultSetIterator.HasMoreResults)
                    {
                        FeedResponse<ImageMetadata> response = await resultSetIterator.ReadNextAsync();
                        results.AddRange(response);
                        if (response.Diagnostics != null)
                        {
                            _logger.LogInformation($"\nQueryWithSqlParameters Diagnostics: {response.Diagnostics.ToString()}");
                        }
                    }

                }                

            }

            _logger.LogInformation($"\nResult Count: {results.Count.ToString()}");
            
            return results;
           
        }

        [HttpGet("getbycountry/{country}")]
        public async Task<List<ImageMetadata>> GetIssuesForCountry(string country)
        {
            ImageMetadata imageData = new ImageMetadata();

            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.addressDetails.countryRegionIso2 = @country")
                .WithParameter("@country", country);

            List<ImageMetadata> results = new List<ImageMetadata>();
            using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
            {
                Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);
                
                using (FeedIterator<ImageMetadata> resultSetIterator = container.GetItemQueryIterator<ImageMetadata>(query))
                {
                    while (resultSetIterator.HasMoreResults)
                    {
                        FeedResponse<ImageMetadata> response = await resultSetIterator.ReadNextAsync();
                        results.AddRange(response);
                        if (response.Diagnostics != null)
                        {
                            _logger.LogInformation($"\nQueryWithSqlParameters Diagnostics: {response.Diagnostics.ToString()}");
                        }
                    }

                }                

            }

            _logger.LogInformation($"\nResult Count: {results.Count.ToString()}");
            
            return results;
           
        }

        [HttpGet("getbyuser/{userId}")]
        public async Task<List<ImageMetadata>> GetIssuesByUser(string userId)
        {
            ImageMetadata imageData = new ImageMetadata();

            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.uploadUserName = @userId")
                .WithParameter("@userId", userId);

            List<ImageMetadata> results = new List<ImageMetadata>();
            using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
            {
                Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);
                
                using (FeedIterator<ImageMetadata> resultSetIterator = container.GetItemQueryIterator<ImageMetadata>(query))
                {
                    while (resultSetIterator.HasMoreResults)
                    {
                        FeedResponse<ImageMetadata> response = await resultSetIterator.ReadNextAsync();
                        results.AddRange(response);
                        if (response.Diagnostics != null)
                        {
                            _logger.LogInformation($"\nQueryWithSqlParameters Diagnostics: {response.Diagnostics.ToString()}");
                        }
                    }

                }                

            }

            _logger.LogInformation($"\nResult Count: {results.Count.ToString()}");
            
            return results;
           
        }

    }
}
