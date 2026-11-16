using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configuration = builder.Configuration;
        string endpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT missing");
        string key = configuration["AZURE_OPENAI_KEY"] ?? throw new InvalidOperationException("AZURE_OPENAI_KEY missing");
        string deployment = configuration["AZURE_OPENAI_DEPLOYMENT"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT missing");

        // Register Azure OpenAI client
        AzureOpenAIClient azureClient = new(
            new Uri(endpoint),
            new AzureKeyCredential(key));

        builder.Services.AddSingleton<AzureOpenAIClient>(sp => new(
            new Uri(endpoint),
            new AzureKeyCredential(key)));

        ChatClient chatClient = azureClient.GetChatClient(deployment);
        builder.Services.AddSingleton(sp => chatClient);

        // Register application services
        builder.Services.AddScoped<ILegalAdvisorService, LegalAdvisorService>();
        builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}