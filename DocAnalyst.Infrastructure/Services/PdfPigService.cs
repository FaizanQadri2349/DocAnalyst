using DocAnalyst.Core.Interfaces;
using UglyToad.PdfPig;
using System.Text;

namespace DocAnalyst.Infrastructure.Services;

public class PdfPigService : IPdfService
{
    public Task<string> ExtractTextAsync(Stream pdfStream)
    {
        // We run this on a background thread so the API doesn't freeze
        return Task.Run(() =>
        {
            var sb = new StringBuilder();

            // Open the PDF using the PdfPig library
            using (var pdf = PdfDocument.Open(pdfStream))
            {
                // Loop through every page and grab the text
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }

            return sb.ToString();
        });
    }
}