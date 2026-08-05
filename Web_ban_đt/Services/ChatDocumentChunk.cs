namespace TechStoreWeb.Services
{
    public enum ChatChunkKind
    {
        ProductSpecs,
        SemanticComparison,
        PolicyChild
    }

    public class ChatDocumentChunk
    {
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ParentContent { get; set; } = string.Empty;
        public ChatChunkKind Kind { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int Stock { get; set; }

        public decimal? PriceFrom { get; set; }

        public decimal? PriceTo { get; set; }
    }

    public class RetrievedChatChunk
    {
        public ChatDocumentChunk Chunk { get; set; } = new();
        public double Score { get; set; }
    }
}
