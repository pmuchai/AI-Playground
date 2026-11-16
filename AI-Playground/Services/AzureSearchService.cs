using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using MathNet.Numerics;
using Microsoft.Extensions.Configuration;
using Model;
using OpenAI.Embeddings;

namespace Services
{
    public class AzureSearchService : IAzureSearchService
    {
        AzureOpenAIClient azureOpenAIClient;
        EmbeddingClient embeddingClient;
        SearchIndexClient searchIndexClient;

        public AzureSearchService(AzureOpenAIClient azureOpenAIClient, IConfiguration configuration)
        {
            var embeddingModel = configuration["AZURE_TEXT_EMBEDDING_MODEL"] ?? throw new InvalidOperationException("AZURE_TEXT_EMBEDDING_MODEL missing");
            var searchServiceEndpoint = configuration["AZURE_SEARCH_SERVICE_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_SEARCH_SERVICE_ENDPOINT missing");
            var searchServiceKey = configuration["AZURE_SEARCH_SERVICE_KEY"] ?? throw new InvalidOperationException("AZURE_SEARCH_SERVICE_KEY missing");

            this.azureOpenAIClient = azureOpenAIClient;
            embeddingClient = this.azureOpenAIClient.GetEmbeddingClient(embeddingModel);
            searchIndexClient = new SearchIndexClient(new Uri(searchServiceEndpoint), new AzureKeyCredential(searchServiceKey));
        }

        public async Task<Dictionary<string, IReadOnlyList<float>>> GenerateFactEmbeddingVectors()
        {
            var vectors = new Dictionary<string, IReadOnlyList<float>>();
            foreach (var information in KnowledgeBase.Facts)
            {
                var response = await embeddingClient.GenerateEmbeddingsAsync(new[] { information.FactDescription });
                // Extract the embedding vector from the response collection
                var embedding = response.Value.FirstOrDefault();
                if (embedding != null)
                {
                    vectors[information.FactName] = embedding.ToFloats().ToArray();
                    Console.WriteLine($"Generated embedding for {information.FactName}");
                }
                else
                {
                    Console.WriteLine($"No embedding generated for {information.FactId}");
                }
            }

            return vectors;
        }

        public async Task<Dictionary<string, float>> CalculateCosineSimilarity(string query)
        {
            var vectors = await GenerateFactEmbeddingVectors();

            Console.WriteLine("Calculating cosine similarity...");
            var queryEmbeddings = embeddingClient.GenerateEmbeddings(new[] { query });
            var queryVector = queryEmbeddings.Value.FirstOrDefault()?.ToFloats().ToArray();

            var results = new Dictionary<string, float>();
            foreach (var vector in vectors)
            {
                float distance = Distance.Cosine(queryVector, vector.Value.ToArray());
                results[vector.Key] = distance;
            }

            // order by ascending distance (lower distance means higher similarity)
            var orderedResults = results.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            Console.WriteLine("Calculating cosine similarity completed...");

            return orderedResults;
        }

        public async Task<Pageable<SearchResult<Fact>>> SearchAsync(string query)
        {
            const string indexName = "fact-index";
            const string searchProfileName = "fact-profile";
            const string searchAlgorithmName = "fact-algorithm";

            // Batch-generate embeddings for all facts in one API call to reduce latency and rate-limit pressure.
            var facts = KnowledgeBase.Facts;
            var factCount = facts.Length;
            var descriptions = new string[factCount];
            for (int i = 0; i < factCount; i++)
            {
                descriptions[i] = facts[i].FactDescription ?? string.Empty;
            }

            var batchResponse = await embeddingClient.GenerateEmbeddingsAsync(descriptions);

            // Determine vector dimension from the first returned embedding (model defined).
            var firstEmbedding = batchResponse.Value.FirstOrDefault();
            if (firstEmbedding == null)
                throw new InvalidOperationException("Embedding model returned no vectors.");

            int indexVectorDimension = firstEmbedding.ToFloats().Length;

            // Create index definition with the discovered vector dimension.
            var searchIndex = new SearchIndex(indexName)
            {
                Fields =
                {
                    new SimpleField("FactId", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true },
                    new SearchableField("FactName") { IsFilterable = true, IsSortable = true },
                    new SearchableField("FactDescription") { IsFilterable = true },
                    new SearchField("FactVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = indexVectorDimension,
                        VectorSearchProfileName = searchProfileName,
                    }
                },
                VectorSearch = new VectorSearch
                {
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration(searchAlgorithmName)
                    },
                    Profiles =
                    {
                        new VectorSearchProfile(searchProfileName, searchAlgorithmName)
                    }
                }
            };

            // Ensure the index exists (create if missing).
            try
            {
                await searchIndexClient.GetIndexAsync(indexName);
                Console.WriteLine($"Index '{indexName}' already exists.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                await searchIndexClient.CreateIndexAsync(searchIndex); 
                Console.WriteLine("Index created successfully.");
            }

            // Map returned embeddings back onto facts (preserving input order).
            // The embedding service returns a collection whose order matches the input list.
            for (int i = 0; i < factCount; i++)
            {
                var emb = batchResponse.Value[i];
                facts[i].FactVector = emb.ToFloats().ToArray();
            }

            var searchClient = searchIndexClient.GetSearchClient(indexName);

            // Index/update documents in a single batch call.
            await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(facts));

            // Delegate to the vector query logic to return results.
            return await QueryAzureAISearchAsync(searchClient, query);
        }

        /// Querying Azure AI Search using Vector Search
        public async Task<Pageable<SearchResult<Fact>>> QueryAzureAISearchAsync(SearchClient searchClient, string query)
        {
            var queryEmbeddings = embeddingClient.GenerateEmbeddings(new[] { query });
            var vectorizedResult = queryEmbeddings.Value.FirstOrDefault()?.ToFloats().ToArray();

            // Top-k sampling vs Top-p (nucleus) sampling
            var vectorizedQuery = new VectorizedQuery(vectorizedResult)
            {
                // Greedy-decoding(1) return the single closest match vs balanced creativity(50) and coherence
                // vs high diversity (500) but risk coherence
                KNearestNeighborsCount = 2
            };
            vectorizedQuery.Fields.Add("FactVector");

            SearchResults<Fact> results = await searchClient.SearchAsync<Fact>(
                searchText: "*",
                new SearchOptions
                {
                    VectorSearch = new VectorSearchOptions
                    {
                        Queries = { vectorizedQuery }
                    }
                });

            return results.GetResults();
        }
    }
}
