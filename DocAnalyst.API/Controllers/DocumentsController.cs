using DocAnalyst.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocAnalyst.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IPdfService _pdfService;

    // We ask for the IPdfService, and the app gives us the PdfPig version automatically
    public DocumentsController(IPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    [HttpPost("extract-text")]
    public async Task<IActionResult> ExtractText(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // Convert the uploaded file into a stream and send it to our service
        using var stream = file.OpenReadStream();
        var text = await _pdfService.ExtractTextAsync(stream);

        return Ok(new { FileName = file.FileName, ExtractedText = text });
    }
}