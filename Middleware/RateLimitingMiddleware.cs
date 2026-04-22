using System.Collections.Concurrent;
using System.Net;

namespace AIContentGenerator.Middleware
{
    /// <summary>
    /// Rate limiting middleware to prevent abuse and ensure fair resource usage.
    /// Implements token bucket algorithm with IP-based rate limiting.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly int _requestsPerMinute;
        private static readonly ConcurrentDictionary<string, RateLimitInfo> ClientRequests = new();

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, int requestsPerMinute = 60)
        {
            _next = next;
            _logger = logger;
            _requestsPerMinute = requestsPerMinute;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            var rateLimitInfo = ClientRequests.AddOrUpdate(
                clientId,
                new RateLimitInfo { Count = 1, ResetTime = now.AddMinutes(1) },
                (key, existing) =>
                {
                    // Reset if time window has passed
                    if (now >= existing.ResetTime)
                    {
                        return new RateLimitInfo { Count = 1, ResetTime = now.AddMinutes(1) };
                    }
                    existing.Count++;
                    return existing;
                });

            if (rateLimitInfo.Count > _requestsPerMinute)
            {
                _logger.LogWarning($"Rate limit exceeded for client: {clientId}. Requests: {rateLimitInfo.Count}");
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                
                var remainingSeconds = (int)(rateLimitInfo.ResetTime - now).TotalSeconds;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    retryAfter = remainingSeconds,
                    message = $"Please try again in {remainingSeconds} seconds"
                });
                return;
            }

            context.Response.Headers["X-RateLimit-Limit"] = _requestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = (_requestsPerMinute - rateLimitInfo.Count).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();

            await _next(context);
        }

        private class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTime ResetTime { get; set; }
        }
    }
}
