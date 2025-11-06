using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SendToKindle.Models;
using SendToKindle.Services;
using System.Net;
using System.Text.Json;

namespace SendToKindle.Functions;

/// <summary>
/// Production function - sends EPUB to Kindle email (requires function key authentication).
///
/// This function receives HTML content that has already been scraped by the browser extension
/// in the user's browser context. This means:
/// - Paywalled content is accessible (user is already logged in)
/// - VPN/geo-restrictions are bypassed (content is already loaded)
/// - JavaScript-rendered content is captured (scraped after page load)
/// - No need to fetch the URL - we already have the rendered HTML
/// </summary>
public class ConvertToEpub
{
    private readonly ILogger<ConvertToEpub> _logger;
    private readonly IEpubConverter _epubConverter;
    private readonly IEmailService _emailService;

    public ConvertToEpub(
        ILogger<ConvertToEpub> logger,
        IEpubConverter epubConverter,
        IEmailService emailService)
    {
        _logger = logger;
        _epubConverter = epubConverter;
        _emailService = emailService;
    }

    [Function("ConvertToEpub")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "convert")] HttpRequestData req)
    {
        _logger.LogInformation("EPUB conversion request received");

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

            // Get Kindle email from headers (set by the web app)
            if (!req.Headers.TryGetValues("X-Kindle-Email", out var kindleEmails))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await unauthorizedResponse.WriteStringAsync("Kindle email not provided");
                return unauthorizedResponse;
            }

            var kindleEmail = kindleEmails.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(kindleEmail))
            {
                var badEmailResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badEmailResponse.WriteStringAsync("Invalid Kindle email");
                return badEmailResponse;
            }

            _logger.LogInformation($"Converting article: {request.Title} for {kindleEmail}");

            // Convert HTML to EPUB
            var epubData = await _epubConverter.ConvertHtmlToEpub(
                request.Html,
                request.Title,
                request.Author,
                request.SourceUrl
            );

            // Create filename
            var safeFileName = string.Join("_", request.Title.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeFileName}.epub";

            // Send to Kindle
            await _emailService.SendToKindle(epubData, fileName, kindleEmail);

            _logger.LogInformation($"EPUB sent to Kindle: {kindleEmail}");

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ConvertResponse
            {
                Success = true,
                Message = "Article sent to your Kindle successfully"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to EPUB or sending to Kindle");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}
