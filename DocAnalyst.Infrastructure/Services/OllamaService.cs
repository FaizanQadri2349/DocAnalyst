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
        history.AddSystemMessage(@"You are a document analysis assistant. Follow these rules strictly:
1. Answer ONLY using information from the provided CONTEXT
2. If the answer is not in the CONTEXT, respond with: 'I cannot answer that based on the provided documents. Please ask questions related to the uploaded PDFs.'
3. Do NOT use your general knowledge
4. Do NOT answer personal questions like 'how are you'
5. Always cite which part of the context you used");

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