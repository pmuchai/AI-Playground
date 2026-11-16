using Azure;
using Azure.Search.Documents.Models;
using Model;

namespace Services
{
    public interface IAzureSearchService
    {
        Task<Dictionary<string, IReadOnlyList<float>>> GenerateFactEmbeddingVectors();
        Task<Dictionary<string, float>> CalculateCosineSimilarity(string query);
        Task<Pageable<SearchResult<Fact>>> SearchAsync(string query);
    }
}
