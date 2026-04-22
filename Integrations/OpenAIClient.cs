using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.CircuitBreaker;
using AIContentGenerator.Utilities;

namespace AIContentGenerator.Integrations
{
    /// <summary>
    /// Enhanced OpenAI client with resilience patterns, retry logic, token optimization, and structured logging.
    /// Implements Polly policies for exponential backoff and circuit breaking.
    /// </summary>
    public class OpenAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenAIClient> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

        public OpenAIClient(HttpClient httpClient, IConfiguration config, ILogger<OpenAIClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("OpenAI API key not configured. Add 'OpenAI:ApiKey' to appsettings.Development.json");
            
            if (_apiKey.Contains("YOUR_OPENAI_API_KEY_HERE"))
                throw new InvalidOperationException("OpenAI API key is not set. Please update appsettings.Development.json with your actual OpenAI API key from https://platform.openai.com/account/api-keys");

            _resiliencePolicy = BuildResiliencePolicy();
        }

        /// <summary>
        /// Builds Polly resilience policy with retry and circuit breaker patterns.
        /// </summary>
        private IAsyncPolicy<HttpResponseMessage> BuildResiliencePolicy()
        {
            // Retry policy: exponential backoff (1s, 2s, 4s)
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => 
                    (int)r.StatusCode == 429 || 
                    (int)r.StatusCode == 500 ||
                    (int)r.StatusCode == 503)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning($"OpenAI API retry #{retryCount} after {timespan.TotalSeconds}s. Status: {outcome.Result?.StatusCode}");
                    });

            // Circuit breaker: open after 5 failures within 30s window
            var circuitBreakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => 
                    (int)r.StatusCode == 500 ||
                    (int)r.StatusCode == 503)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        public async Task<string> GenerateContent(string prompt)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Optimize prompt to reduce token usage
                var optimizedPrompt = TokenOptimizer.OptimizePrompt(prompt);
                var promptTokens = TokenOptimizer.EstimateTokens(optimizedPrompt);
                
                _logger.LogInformation($"Content generation started. Estimated input tokens: {promptTokens}");

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "user", content = optimizedPrompt }
                    },
                    temperature = 0.7,
                    top_p = 0.95
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Execute with resilience policy
                var response = await _resiliencePolicy.ExecuteAsync(
                    async () => await _httpClient.SendAsync(request));

                var responseString = await response.Content.ReadAsStringAsync();
                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation($"OpenAI API response received. Status: {response.StatusCode}, Duration: {duration.TotalSeconds:F2}s");

                // Handle errors
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again later.",
                        System.Net.HttpStatusCode.Unauthorized => "Invalid OpenAI API key.",
                        _ => $"OpenAI API Error ({response.StatusCode})"
                    };
                    
                    _logger.LogError($"OpenAI API Error: {response.StatusCode} - {responseString}");
                    throw new Exception(errorMessage);
                }

                using var doc = JsonDocument.Parse(responseString);

                // Check for OpenAI internal errors
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    var message = error.GetProperty("message").GetString();
                    _logger.LogError($"OpenAI returned error: {message}");
                    throw new Exception($"OpenAI Error: {message}");
                }

                // Extract response
                if (!doc.RootElement.TryGetProperty("choices", out var choices))
                {
                    _logger.LogError($"Invalid OpenAI response structure");
                    throw new Exception("Invalid OpenAI response format");
                }

                var content = choices[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogError($"Received empty content from OpenAI");
                    throw new Exception("OpenAI returned empty content");
                }

                var outputTokens = TokenOptimizer.EstimateTokens(content);
                _logger.LogInformation($"Content generation completed. Output tokens: {outputTokens}, Total duration: {duration.TotalSeconds:F2}s");

                return content;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError($"Circuit breaker is open. Service temporarily unavailable: {ex.Message}");
                throw new InvalidOperationException("OpenAI service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError($"Error generating content after {duration.TotalSeconds:F2}s: {ex.Message}");
                throw;
            }
        }
    }
}