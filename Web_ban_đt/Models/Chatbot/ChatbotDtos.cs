namespace TechStoreWeb.Models.Chatbot
{
    public class ChatbotAskRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
    }

    public class ChatbotAskResponse
    {
        public string Answer { get; set; } = string.Empty;
        public IReadOnlyList<ChatbotSourceDto> Sources { get; set; } = Array.Empty<ChatbotSourceDto>();
        public IReadOnlyList<ChatbotProductCardDto> Products { get; set; } = Array.Empty<ChatbotProductCardDto>();
        public ChatbotMemoryDto Memory { get; set; } = new();
    }

    /// <summary>Thẻ máy hiện kèm câu trả lời: ảnh, giá và lối tắt sang trang chi tiết.</summary>
    public class ChatbotProductCardDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string PriceText { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool InStock { get; set; }
    }

    public class ChatbotSourceDto
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public decimal Score { get; set; }
    }

    public class ChatbotMemoryDto
    {
        public string PreferredBrands { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string UseCases { get; set; } = string.Empty;
        public string InterestedProducts { get; set; } = string.Empty;
    }

    public class ChatbotHistoryItemDto
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<ChatbotProductCardDto> Products { get; set; } = Array.Empty<ChatbotProductCardDto>();
    }
}
