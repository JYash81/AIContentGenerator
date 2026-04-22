namespace AIContentGenerator.Models
{
    public class ContentRequest
    {
        public required string Topic { get; set; }
        public required string Tone { get; set; }
        public int WordCount { get; set; }
    }
}