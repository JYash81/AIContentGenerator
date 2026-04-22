namespace AIContentGenerator.Utilities
{
    /// <summary>
    /// Token optimization utility to reduce API costs by estimating and managing token usage.
    /// Uses approximation based on research: ~4 characters = 1 token for English text.
    /// </summary>
    public static class TokenOptimizer
    {
        private const float TokensPerCharacter = 0.25f; // ~4 characters per token
        private const int MaxTokensPerRequest = 4000;
        private const int ResponseTokensReserve = 1000; // Reserve for response

        /// <summary>
        /// Estimates token count for text input.
        /// </summary>
        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // OpenAI estimation: ~1 token per 4 characters
            return Math.Max(1, (int)Math.Ceiling(text.Length * TokensPerCharacter));
        }

        /// <summary>
        /// Optimizes prompt by truncating if necessary to stay within token limits.
        /// </summary>
        public static string OptimizePrompt(string prompt, int maxWordCount = 5000)
        {
            if (string.IsNullOrEmpty(prompt))
                return prompt;

            // Calculate available tokens for input
            var availableTokens = MaxTokensPerRequest - ResponseTokensReserve;
            var promptTokens = EstimateTokens(prompt);

            if (promptTokens <= availableTokens)
                return prompt;

            // If too long, truncate intelligently
            var maxChars = (int)(availableTokens / TokensPerCharacter);
            var truncated = prompt.Substring(0, Math.Min(prompt.Length, maxChars));

            // Try to truncate at word boundary
            var lastSpace = truncated.LastIndexOf(' ');
            if (lastSpace > maxChars * 0.8) // Only if we're not losing too much
            {
                truncated = truncated.Substring(0, lastSpace) + "...";
            }

            return truncated;
        }

        /// <summary>
        /// Calculates estimated cost for API request.
        /// </summary>
        public static (int inputTokens, int estimatedOutputTokens, decimal estimatedCost) CalculateCost(
            string prompt,
            int wordCount,
            decimal inputCostPer1kTokens = 0.15m,
            decimal outputCostPer1kTokens = 0.60m)
        {
            var inputTokens = EstimateTokens(prompt);
            
            // Rough estimate: 10 words ≈ 13 tokens
            var estimatedOutputTokens = Math.Max(100, (int)(wordCount * 1.3));
            
            var totalTokens = inputTokens + estimatedOutputTokens;
            var estimatedCost = (decimal)((inputTokens * inputCostPer1kTokens + estimatedOutputTokens * outputCostPer1kTokens) / 1000);

            return (inputTokens, estimatedOutputTokens, estimatedCost);
        }

        /// <summary>
        /// Warns if token usage is abnormally high.
        /// </summary>
        public static void LogTokenWarning(int tokenCount, int threshold = 3500)
        {
            if (tokenCount > threshold)
            {
                var remaining = MaxTokensPerRequest - tokenCount;
                if (remaining < ResponseTokensReserve)
                {
                    throw new InvalidOperationException(
                        $"Request exceeds token limits. Used: {tokenCount}, Limit: {MaxTokensPerRequest}");
                }
            }
        }
    }
}
