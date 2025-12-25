using System;

namespace DocAnalyst.Core.Models
{
    public class DocumentChunk
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}