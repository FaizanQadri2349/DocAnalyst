using DocAnalyst.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocAnalyst.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IPdfService _pdfService;
    private readonly IAiService _aiService;
    private readonly IVectorDbService _vectorDb;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly string _collectionName;

    public DocumentsController(
        IPdfService pdfService,
        IAiService aiService,
        IVectorDbService vectorDb,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        _pdfService = pdfService;
        _aiService = aiService;
        _vectorDb = vectorDb;
        _env = env;
        _configuration = configuration;
        _collectionName = configuration["Qdrant:CollectionName"] ?? "documents";

        // Initialize collection on startup
        Task.Run(async () => await _vectorDb.InitializeCollectionAsync(_collectionName, 768));
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        // 1. Save File
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, $"{Guid.NewGuid()}_{file.FileName}");
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 2. Extract Text
        using var fileStream = System.IO.File.OpenRead(filePath);
        var text = await _pdfService.ExtractTextAsync(fileStream);

        // 3. Chunk the text (simple chunking by paragraph or character limit)
        var chunkSize = int.Parse(_configuration["ChunkSize"] ?? "500");
        var chunks = ChunkText(text, chunkSize);

        // 4. Generate embeddings and store in Qdrant
        var storedChunkIds = new List<Guid>();
        foreach (var chunk in chunks)
        {
            var embedding = await _aiService.GenerateEmbeddingAsync(chunk);
            var chunkId = await _vectorDb.StoreDocumentAsync(
                _collectionName,
                chunk,
                embedding,
                new Dictionary<string, object>
                {
                    ["filename"] = file.FileName,
                    ["filepath"] = filePath
                }
            );
            storedChunkIds.Add(chunkId);
        }

        return Ok(new
        {
            Message = "File processed and stored in vector database!",
            SavedPath = filePath,
            ChunksStored = storedChunkIds.Count,
            ChunkIds = storedChunkIds
        });
    }

    [HttpPost("ask")]
    public async Task<IActionResult> AskQuestion([FromBody] AskRequest request)
    {
        // 1. Generate embedding for the question
        var questionEmbedding = await _aiService.GenerateEmbeddingAsync(request.Question);

        // 2. Search for relevant chunks in Qdrant
        var relevantChunks = await _vectorDb.SearchSimilarAsync(_collectionName, questionEmbedding, limit: 3);

        // 3. Combine chunks as context
        var context = string.Join("\n\n", relevantChunks.Select(c => c.Text));

        // 4. Ask AI with context (RAG)
        var answer = await _aiService.AnalyzeDocumentAsync(context, request.Question);

        return Ok(new
        {
            Answer = answer,
            RelevantChunks = relevantChunks.Count,
            Sources = relevantChunks.Select(c => new { c.Text, c.Score, c.Filename })
        });
    }

    private List<string> ChunkText(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = "";

        foreach (var sentence in sentences)
        {
            if ((currentChunk + sentence).Length > maxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.Trim());
                currentChunk = sentence;
            }
            else
            {
                currentChunk += sentence + ". ";
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
            chunks.Add(currentChunk.Trim());

        return chunks;
    }

    [HttpGet("health/qdrant")]
    public async Task<IActionResult> CheckQdrantConnection()
    {
        try
        {
            await _vectorDb.InitializeCollectionAsync(_collectionName, 768);
            return Ok(new
            {
                Status = "Connected",
                Message = "Successfully connected to Docker Qdrant!",
                Collection = _collectionName,
                QdrantHost = _configuration["Qdrant:Host"],
                QdrantPort = _configuration["Qdrant:Port"]
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Status = "Failed",
                Message = "Cannot connect to Docker Qdrant",
                Error = ex.Message
            });
        }
    }

    [HttpPost("test-embedding")]
    public async Task<IActionResult> TestEmbedding(string text)
    {
        var vector = await _aiService.GenerateEmbeddingAsync(text);
        return Ok(new
        {
            Message = "Text converted to numbers!",
            VectorLength = vector.Length,
            FirstFewNumbers = vector.Take(5)
        });
    }
}

public class AskRequest
{
    public required string Question { get; set; }
}