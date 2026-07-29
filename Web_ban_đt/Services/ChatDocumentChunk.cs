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

        /// <summary>Giá bán hiện tại, dùng để chấm điểm theo ngân sách khách nêu.</summary>
        public decimal? Price { get; set; }

        /// <summary>Ảnh sản phẩm để khung chat hiện thẻ máy kèm thông số.</summary>
        public string ImageUrl { get; set; } = string.Empty;

        public int Stock { get; set; }

        /// <summary>
        /// Khoảng giá gồm cả các phiên bản dung lượng. Tư vấn viên hay trích giá bản cao
        /// (Xiaomi 17 Ultra bản 512GB là 6.850.000 trong khi bản gốc 4.350.000) nên thẻ máy
        /// phải hiện khoảng giá, nếu chỉ hiện giá gốc khách sẽ tưởng hai nơi báo giá vênh nhau.
        /// </summary>
        public decimal? PriceFrom { get; set; }

        public decimal? PriceTo { get; set; }
    }

    public class RetrievedChatChunk
    {
        public ChatDocumentChunk Chunk { get; set; } = new();
        public double Score { get; set; }
    }
}
