using AIContentGenerator.Models;
using AIContentGenerator.Integrations;
using AIContentGenerator.Helpers;
using AIContentGenerator.Utilities;

namespace AIContentGenerator.Services
{
    /// <summary>
    /// Service for content generation with logging and token optimization.
    /// </summary>
    public class ContentService : IContentService
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<ContentService> _logger;

        public ContentService(OpenAIClient client, ILogger<ContentService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<ContentResponse> GenerateContent(ContentRequest request)
        {
            _logger.LogInformation($"Content generation requested. Topic: {request.Topic}, Tone: {request.Tone}, WordCount: {request.WordCount}");

            // Validate request
            if (request.WordCount < 1 || request.WordCount > 5000)
            {
                _logger.LogWarning($"Invalid word count: {request.WordCount}. Must be between 100-5000.");
                throw new ArgumentException("Word count must be between 100 and 5000.");
            }

            try
            {
                var prompt = PromptBuilder.Build(request);
                
                // Calculate estimated cost before making request
                var (inputTokens, outputTokens, estimatedCost) = TokenOptimizer.CalculateCost(prompt, request.WordCount);
                _logger.LogInformation($"Estimated tokens - Input: {inputTokens}, Output: {outputTokens}, Estimated cost: ${estimatedCost:F4}");

                var result = await _client.GenerateContent(prompt);

                _logger.LogInformation($"Content generation succeeded. Generated {result.Length} characters.");

                return new ContentResponse
                {
                    GeneratedText = result,
                    TokensUsed = inputTokens + outputTokens,
                    EstimatedCost = estimatedCost
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Content generation failed: {ex.Message}");
                throw;
            }
        }
    }
}