using DocAnalyst.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocAnalyst.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IPdfService _pdfService;
    private readonly IAiService _aiService; // <--- NEW: The Brain
    private readonly IWebHostEnvironment _env;

    // We now ask for both the PDF Service AND the AI Service
    public DocumentsController(IPdfService pdfService, IAiService aiService, IWebHostEnvironment env)
    {
        _pdfService = pdfService;
        _aiService = aiService;
        _env = env;
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

        return Ok(new
        {
            Message = "File saved!",
            SavedPath = filePath,
            ExtractedText = text
        });
    }

    // --- NEW ENDPOINT: Chat with the text ---
    [HttpPost("ask")]
    public async Task<IActionResult> AskQuestion([FromBody] AskRequest request)
    {
        // We send the document text + the question to Ollama
        var answer = await _aiService.AnalyzeDocumentAsync(request.DocumentText, request.Question);

        return Ok(new { Answer = answer });
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

// Simple helper class to hold the data sent from Swagger
public class AskRequest
{
    public string DocumentText { get; set; }
    public string Question { get; set; }
}