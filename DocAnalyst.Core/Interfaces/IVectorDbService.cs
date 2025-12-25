namespace DocAnalyst.Core.Interfaces;

public interface IVectorDbService
{
    Task InitializeCollectionAsync(string collectionName, int vectorSize = 768);
    Task<Guid> StoreDocumentAsync(string collectionName, string text, ReadOnlyMemory<float> embedding, Dictionary<string, object>? metadata = null);
    Task<List<(string Text, float Score, string Filename)>> SearchSimilarAsync(string collectionName, ReadOnlyMemory<float> queryEmbedding, int limit = 5);
    Task<bool> DeleteDocumentAsync(string collectionName, Guid documentId);
}
