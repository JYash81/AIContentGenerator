namespace AIContentGenerator.Models
{
    public class ContentResponse
    {
        public required string GeneratedText { get; set; }
        public int TokensUsed { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}