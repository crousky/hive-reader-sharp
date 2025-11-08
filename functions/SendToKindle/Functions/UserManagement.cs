using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;

namespace SendToKindle.Functions;

public class UserManagement
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly bool _isLocal;

    // Cosmos DB Emulator settings
    private const string EmulatorEndpoint = "https://localhost:8081";
    private const string EmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public UserManagement(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UserManagement>();

        // Detect local environment
        _isLocal = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development"
                   || Environment.GetEnvironmentVariable("USE_EMULATOR") == "true";

        // Initialize Cosmos Client
        var endpoint = _isLocal ? EmulatorEndpoint : Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
        var key = _isLocal ? EmulatorKey : Environment.GetEnvironmentVariable("COSMOS_KEY");

        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // Disable SSL validation for emulator
        if (_isLocal)
        {
            options.HttpClientFactory = () =>
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(httpMessageHandler);
            };
        }

        _cosmosClient = new CosmosClient(endpoint, key, options);
    }

    [Function("GetUser")]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{userId}")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation($"Getting user: {userId}");

        try
        {
            var container = _cosmosClient.GetContainer("SendToKindleDB", "Users");
            var response = await container.ReadItemAsync<User>(userId, new PartitionKey(userId));

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response.Resource);
            return httpResponse;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"User not found: {userId}");
            return req.CreateResponse(HttpStatusCode.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("CreateOrUpdateUser")]
    public async Task<HttpResponseData> CreateOrUpdateUser(
        [HttpTrigger(AuthorizationLevel.Function, "post", "put", Route = "users")] HttpRequestData req)
    {
        _logger.LogInformation("Creating or updating user");

        try
        {
            var user = await req.ReadFromJsonAsync<User>();
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            if (string.IsNullOrEmpty(user.CreatedAt))
            {
                user.CreatedAt = user.UpdatedAt;
            }

            var container = _cosmosClient.GetContainer("SendToKindleDB", "Users");
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.Id));

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response.Resource);
            return httpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating user");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("UpdateKindleEmail")]
    public async Task<HttpResponseData> UpdateKindleEmail(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "users/{userId}/kindle-email")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation($"Updating Kindle email for user: {userId}");

        try
        {
            var body = await req.ReadFromJsonAsync<UpdateKindleEmailRequest>();
            if (body == null || string.IsNullOrEmpty(body.KindleEmail))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var container = _cosmosClient.GetContainer("SendToKindleDB", "Users");
            var userResponse = await container.ReadItemAsync<User>(userId, new PartitionKey(userId));
            var user = userResponse.Resource;

            user.KindleEmail = body.KindleEmail;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");

            var updateResponse = await container.ReplaceItemAsync(user, userId, new PartitionKey(userId));

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(updateResponse.Resource);
            return httpResponse;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"User not found: {userId}");
            return req.CreateResponse(HttpStatusCode.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Kindle email");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("DeleteUser")]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{userId}")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation($"Deleting user: {userId}");

        try
        {
            var container = _cosmosClient.GetContainer("SendToKindleDB", "Users");
            await container.DeleteItemAsync<User>(userId, new PartitionKey(userId));

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"User not found: {userId}");
            return req.CreateResponse(HttpStatusCode.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? KindleEmail { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class UpdateKindleEmailRequest
{
    public string KindleEmail { get; set; } = string.Empty;
}
