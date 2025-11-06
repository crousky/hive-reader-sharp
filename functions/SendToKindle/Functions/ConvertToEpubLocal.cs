using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SendToKindle.Models;
using SendToKindle.Services;
using System.Net;
using System.Text.Json;

namespace SendToKindle.Functions;

/// <summary>
/// Local testing function - saves EPUB to local directory without authentication.
///
/// This function receives HTML content that has already been scraped by the browser extension
/// in the user's browser context. This means:
/// - Paywalled content is accessible (user is already logged in)
/// - VPN/geo-restrictions are bypassed (content is already loaded)
/// - JavaScript-rendered content is captured (scraped after page load)
/// - No need to fetch the URL - we already have the rendered HTML
/// </summary>
public class ConvertToEpubLocal
{
    private readonly ILogger<ConvertToEpubLocal> _logger;
    private readonly IEpubConverter _epubConverter;
    private readonly string _outputDirectory;

    public ConvertToEpubLocal(
        ILogger<ConvertToEpubLocal> logger,
        IEpubConverter epubConverter)
    {
        _logger = logger;
        _epubConverter = epubConverter;
        _outputDirectory = Environment.GetEnvironmentVariable("LOCAL_OUTPUT_DIR") ??
                          Path.Combine(Directory.GetCurrentDirectory(), "output");

        // Ensure output directory exists
        Directory.CreateDirectory(_outputDirectory);
    }

    [Function("ConvertToEpubLocal")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "convert-local")] HttpRequestData req)
    {
        _logger.LogInformation("Local EPUB conversion request received");

        try
        {
            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ConvertRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrWhiteSpace(request.Html) || string.IsNullOrWhiteSpace(request.Title))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request. HTML and Title are required.");
                return badResponse;
            }

            _logger.LogInformation($"Converting article: {request.Title}");

            // Convert HTML to EPUB
            var epubData = await _epubConverter.ConvertHtmlToEpub(
                request.Html,
                request.Title,
                request.Author,
                request.SourceUrl
            );

            // Save to local file
            var safeFileName = string.Join("_", request.Title.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.epub";
            var filePath = Path.Combine(_outputDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, epubData);

            _logger.LogInformation($"EPUB saved to: {filePath}");

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ConvertResponse
            {
                Success = true,
                Message = "EPUB created successfully",
                FilePath = filePath
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to EPUB");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}
