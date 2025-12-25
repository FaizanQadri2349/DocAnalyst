namespace DocAnalyst.Core.Interfaces;

public interface IAiService
{
    Task<string> AnalyzeDocumentAsync(string documentText, string userQuestion);

    // <--- Add this new line
    Task<float[]> GenerateEmbeddingAsync(string text);
}