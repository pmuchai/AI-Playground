using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace Services
{
    /// <summary>
    /// Minimal service that talks to Azure OpenAI (Chat completions) to produce legal-advice style responses.
    /// </summary>
    public class LegalAdvisorService : ILegalAdvisorService
    {
        private readonly AzureOpenAIClient _azureOpenAiClient;
        private readonly ChatClient _chatClient;
        private readonly string _deployment;
        private readonly string _systemMessage;

        public LegalAdvisorService(AzureOpenAIClient azureOpenAiClient, IConfiguration configuration)
        {
            _azureOpenAiClient = azureOpenAiClient ?? throw new ArgumentNullException(nameof(azureOpenAiClient));
            _deployment = configuration["AZURE_OPENAI_DEPLOYMENT"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT missing");
            _chatClient = _azureOpenAiClient.GetChatClient(_deployment);

            // Load system message once at construction time (portable, configurable path)
            var configuredPath = configuration["LEGAL_SYSTEM_MESSAGE_PATH"];
            var defaultPath = Path.Combine(AppContext.BaseDirectory, "Data", "LegalSystemMessage.txt");
            var systemMessagePath = !string.IsNullOrWhiteSpace(configuredPath) ? configuredPath : defaultPath;

            try
            {
                if (File.Exists(systemMessagePath))
                {
                    _systemMessage = File.ReadAllText(systemMessagePath);
                }
                else
                {
                    // Fallback concise system message if file is missing
                    _systemMessage = "You are an AI assistant that provides short, neutral, responsible legal answers. Always remind users to seek a qualified lawyer for binding advice.";
                    Console.WriteLine($"Legal system message not found at '{systemMessagePath}'. Using fallback system message.");
                }
            }
            catch (Exception ex)
            {
                _systemMessage = "You are an AI assistant that provides short, neutral, responsible legal answers. Always remind users to seek a qualified lawyer for binding advice.";
                Console.WriteLine($"Failed to load legal system message from '{systemMessagePath}': {ex.Message}. Using fallback system message.");
            }
        }

        public string Advise(string userPrompt)
        {
            if (string.IsNullOrWhiteSpace(userPrompt))
                return "Please provide a question or prompt.";

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(_systemMessage),
                    new UserChatMessage(userPrompt)
                };

                var response = _chatClient.CompleteChatAsync(messages).GetAwaiter().GetResult();
                var content = response.Value?.Content;
                var text = (content != null && content.Count > 0) ? content[0].Text : null;
                return text?.Trim() ?? "No response returned from the model.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating legal advice: {ex.Message}");
                return "An error occurred while generating the response. Please try again later.";
            }
        }
    }
}
