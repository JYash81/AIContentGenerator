using Microsoft.AspNetCore.Mvc;
using AIContentGenerator.Models;
using AIContentGenerator.Services;

namespace AIContentGenerator.Controllers
{
    /// <summary>
    /// Content generation API controller with comprehensive logging and error handling.
    /// </summary>
    [ApiController]
    [Route("api/content")]
    public class ContentController : ControllerBase
    {
        private readonly IContentService _service;
        private readonly ILogger<ContentController> _logger;

        public ContentController(IContentService service, ILogger<ContentController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Generates AI content based on provided parameters.
        /// </summary>
        /// <param name="request">The content generation request</param>
        /// <returns>Generated content with token and cost information</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> Generate(ContentRequest request)
        {
            var requestId = HttpContext.TraceIdentifier;
            _logger.LogInformation($"[{requestId}] Received content generation request from {HttpContext.Connection.RemoteIpAddress}");

            try
            {
                // Validate input
                if (request == null)
                {
                    _logger.LogWarning($"[{requestId}] Request body is null");
                    return BadRequest(new { error = "Request body is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Topic))
                {
                    _logger.LogWarning($"[{requestId}] Topic is missing");
                    return BadRequest(new { error = "Topic is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Tone))
                {
                    _logger.LogWarning($"[{requestId}] Tone is missing");
                    return BadRequest(new { error = "Tone is required" });
                }

                if (request.WordCount <= 0)
                {
                    _logger.LogWarning($"[{requestId}] Invalid word count: {request.WordCount}");
                    return BadRequest(new { error = "WordCount must be greater than 0" });
                }

                var result = await _service.GenerateContent(request);
                
                _logger.LogInformation($"[{requestId}] Content generation succeeded. Tokens: {result.TokensUsed}, Cost: ${result.EstimatedCost:F4}");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"[{requestId}] Validation error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"[{requestId}] Operation error: {ex.Message}");
                return StatusCode(503, new { error = "Service temporarily unavailable. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{requestId}] Unexpected error: {ex.GetType().Name} - {ex.Message}");
                _logger.LogError(ex, $"[{requestId}] Stack trace:");
                
                // Check for configuration issues
                if (ex.Message.Contains("API key"))
                    return StatusCode(500, new { error = ex.Message });
                
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later.", details = ex.Message });
            }
        }
    }
}