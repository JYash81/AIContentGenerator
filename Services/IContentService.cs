using AIContentGenerator.Models;

namespace AIContentGenerator.Services
{
    public interface IContentService
    {
        Task<ContentResponse> GenerateContent(ContentRequest request);
    }
}