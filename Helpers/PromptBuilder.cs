using AIContentGenerator.Models;

namespace AIContentGenerator.Helpers
{
    public static class PromptBuilder
    {
        public static string Build(ContentRequest request)
        {
            return $@"
            Generate a {request.WordCount}-word blog on '{request.Topic}'.

            Tone: {request.Tone}

            Structure:
            - Title
            - Introduction
            - 3 Key Points
            - Conclusion

            Keep it professional and engaging.
            ";
        }
    }
}
