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

namespace IssueWriteApis.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IssuesController : ControllerBase
    {
        private readonly ILogger<IssuesController> _logger;

        private readonly IConfiguration _configuration;

        private readonly string _cosmosEndpoint;
        private readonly string _cosmosReadKey;
        private readonly string _cosmosKey;
        private readonly string _cosmosDBId;
        private readonly string _cosmosContainerId;
        public IssuesController(ILogger<IssuesController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosEndpoint = _configuration["CosmosDBEndpoint"];
            _cosmosReadKey = _configuration["CosmosDBReadOnlyKey"];
            _cosmosKey = _configuration["CosmosDBWriteKey"];
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

            using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosReadKey))
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

        // POST: api/NewIssue
        [HttpPost("newIssue")]
        public async Task<ActionResult<ImageMetadata>> PostIssue(ImageMetadata newIssue)
        {

            ImageMetadata existingIssue = await GetById(newIssue.id);

            if(existingIssue.id == newIssue.id)
            {
                
                _logger.LogInformation("Issue with id: " + newIssue.id + " already exists");
                ImageMetadata issueNotCreated = new ImageMetadata();
                
                return issueNotCreated;
            }
            else
            {
                _logger.LogInformation("Creating new issue");

                using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
                {
                    Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);
                    
                    ItemResponse<ImageMetadata> response = await container.CreateItemAsync(newIssue, new PartitionKey(newIssue.issueType));
                    ImageMetadata issueCreated = response;
                    _logger.LogInformation($"\nIssue created {issueCreated.id}");

                }

                return CreatedAtAction(nameof(GetById), new { id = newIssue.id }, newIssue);
                
            }

        }

        [HttpDelete("deleteById/{id}")]
        public async Task<string> DeleteById(string id)
        {
            ImageMetadata imageData = new ImageMetadata();
            string retMessage = null;

            imageData = await GetById(id);

            if(imageData.id == id)
            {
                _logger.LogInformation("Deleting issue with id: " + id);
                
                using (CosmosClient csmsClient = new CosmosClient(_cosmosEndpoint, _cosmosKey))
                {
                    Container container = csmsClient.GetContainer(_cosmosDBId, _cosmosContainerId);

                    ItemResponse<ImageMetadata> response = await container.DeleteItemAsync<ImageMetadata>(
                        partitionKey: new PartitionKey(imageData.issueType),
                        id: id);                                

                    _logger.LogInformation("Request charge of delete operation: {0}", response.RequestCharge);
                    _logger.LogInformation("Status Code of delete operation: {0}", response.StatusCode);

                }

                retMessage = "Issue with id: " + id + " has been deleted.";
            }
            else
            {
                retMessage = "Issue with id: " + id + " does not exist. Nothing deleted.";
            }

            _logger.LogInformation(retMessage);
            return retMessage;
            
        }

    }
}
