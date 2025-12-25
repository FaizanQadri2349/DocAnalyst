using DocAnalyst.Core.Interfaces;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace DocAnalyst.Infrastructure.Services;

public class QdrantService : IVectorDbService
{
    private readonly QdrantClient _client;

    public QdrantService(string host = "localhost", int port = 6334)
    {
        _client = new QdrantClient(host, port);
    }

    public async Task InitializeCollectionAsync(string collectionName, int vectorSize = 768)
    {
        try
        {
            // Check if collection exists
            var collections = await _client.ListCollectionsAsync();
            if (collections.Contains(collectionName))
            {
                return; // Collection already exists
            }

            // Create new collection
            await _client.CreateCollectionAsync(
                collectionName: collectionName,
                vectorsConfig: new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine
                }
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize collection '{collectionName}': {ex.Message}", ex);
        }
    }

    public async Task<Guid> StoreDocumentAsync(string collectionName, string text, ReadOnlyMemory<float> embedding, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var pointId = Guid.NewGuid();

            var payload = new Dictionary<string, Value>
            {
                ["text"] = text
            };

            if (metadata != null)
            {
                foreach (var (key, value) in metadata)
                {
                    payload[key] = ConvertToValue(value);
                }
            }

            var point = new PointStruct
            {
                Id = pointId,
                Vectors = embedding.ToArray(),
                Payload = { payload }
            };

            await _client.UpsertAsync(collectionName, new[] { point });
            return pointId;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to store document in '{collectionName}': {ex.Message}", ex);
        }
    }

    public async Task<List<(string Text, float Score, string Filename)>> SearchSimilarAsync(string collectionName, ReadOnlyMemory<float> queryEmbedding, int limit = 5)
    {
        try
        {
            var searchResult = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)limit,
                payloadSelector: true
            );

            return searchResult
                .Select(result => (
                    Text: result.Payload["text"].StringValue,
                    Score: result.Score,
                    Filename: result.Payload.ContainsKey("filename") ? result.Payload["filename"].StringValue : "Unknown"
                ))
                .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search in collection '{collectionName}': {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteDocumentAsync(string collectionName, Guid documentId)
    {
        try
        {
            await _client.DeleteAsync(collectionName, documentId);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete document from '{collectionName}': {ex.Message}", ex);
        }
    }

    private static Value ConvertToValue(object obj)
    {
        return obj switch
        {
            string s => s,
            int i => i,
            long l => l,
            double d => d,
            bool b => b,
            _ => obj.ToString() ?? string.Empty
        };
    }
}
