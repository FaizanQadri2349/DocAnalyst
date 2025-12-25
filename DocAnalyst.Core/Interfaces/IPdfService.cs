using System;
using System.Collections.Generic;
using System.Text;

namespace DocAnalyst.Core.Interfaces;

public interface IPdfService
{
    // This defines our rule: "A PDF Service must be able to take a file and give back text."
    Task<string> ExtractTextAsync(Stream pdfStream);
}