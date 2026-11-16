
using Azure;
using Azure.Search.Documents.Models;

namespace Model
{
    public class AdviceResponse
    {
        public string Advice { get; set; } = string.Empty;
        public Pageable<SearchResult<Fact>> SearchResults { get; set; } = null!;
        public Dictionary<string, float> CosineSimilarity { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, IReadOnlyList<float>> Embeddings { get; internal set; } = new Dictionary<string, IReadOnlyList<float>>();
    }
}
