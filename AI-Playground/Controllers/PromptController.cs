using Services;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptController : ControllerBase
    {
        private readonly ILegalAdvisorService _advisorService;
        private readonly IAzureSearchService azureSearchService;

        public PromptController(ILegalAdvisorService advisorService, IAzureSearchService azureSearchService)
        {
            _advisorService = advisorService;
            this.azureSearchService = azureSearchService;
        }

        /// <summary>
        /// Ask the AI Legal advisor a question. Example body: { "prompt": "What are the legal requirements and steps to register a private limited company in Kenya under the Companies Act 2015?" }
        /// </summary>
        [HttpPost("advice")]
        [ProducesResponseType(typeof(AdviceResponse), 200)]
        [ProducesResponseType(400)]
        public IActionResult GetAdvice([FromBody] AdviceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt is required.");

            var answer = _advisorService.Advise(request.Prompt);
            var htmlAnswer = ToHtml(answer);
            return Ok(new AdviceResponse { Advice = htmlAnswer });
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(AdviceResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSearchResultsAsync([FromBody] AdviceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt is required.");

            var cosine = await azureSearchService.CalculateCosineSimilarity(request.Prompt);
            var searchResult = await azureSearchService.SearchAsync(request.Prompt);

            return Ok(new AdviceResponse { CosineSimilarity = cosine, SearchResults = searchResult });
        }

        [HttpGet("embeddings")]
        [ProducesResponseType(typeof(AdviceResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetEmbeddingsAsync()
        {
            var embeddings = await azureSearchService.GenerateFactEmbeddingVectors();
            return Ok(new AdviceResponse { Embeddings = embeddings });
        }

        /// <summary>
        /// Simple health check endpoint.
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health() => Ok(new { Status = "Healthy" });

        private static string ToHtml(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var htmlBuilder = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("- "))
                {
                    // Assuming consecutive list items, a more complex parser would be needed for multiple lists.
                    htmlBuilder.Append("<ul><li>").Append(trimmedLine.AsSpan(2)).Append("</li></ul>");
                }
                else
                {
                    htmlBuilder.Append("<p>").Append(trimmedLine).Append("</p>");
                }
            }

            return htmlBuilder.ToString();
        }
    }
}
