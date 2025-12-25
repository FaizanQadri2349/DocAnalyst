using DocAnalyst.Core.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace DocAnalyst.Infrastructure.Services;

public class OllamaService : IAiService
{
    private readonly IChatCompletionService _chatService;
    private readonly ITextEmbeddingGenerationService _embeddingService; // <--- NEW: The Mathematician

    public OllamaService()
    {
        var builder = Kernel.CreateBuilder();

        // 1. Create a "Network Client" that points to your local Ollama
        // We use this to trick the OpenAI connector into looking at your laptop instead of the cloud
        var ollamaClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434/v1") };

        // 2. The Talker (Llama 3.2)
        builder.AddOpenAIChatCompletion(
            modelId: "llama3.2",
            apiKey: "ignore",
            httpClient: ollamaClient); // <--- CHANGE: Use 'httpClient' here

        // 3. The Mathematician (Nomic Embed)
        builder.AddOpenAITextEmbeddingGeneration(
            modelId: "nomic-embed-text",
            apiKey: "ignore",
            httpClient: ollamaClient); // <--- CHANGE: Use 'httpClient' here

        var kernel = builder.Build();
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }
    public async Task<string> AnalyzeDocumentAsync(string documentText, string userQuestion)
    {
        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful assistant. Answer based ONLY on the provided context.");
        history.AddUserMessage($"CONTEXT: {documentText}\n\nQUESTION: {userQuestion}");

        var result = await _chatService.GetChatMessageContentAsync(history);
        return result.ToString();
    }

    // NEW FUNCTION: Turns text into numbers
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(new[] { text });
        return embeddings[0].ToArray();
    }
}