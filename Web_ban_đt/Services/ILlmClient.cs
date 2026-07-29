namespace TechStoreWeb.Services
{
    public class LlmClientResult
    {
        public string? Text { get; set; }
        public bool IsServiceUnavailable { get; set; }

        /// <summary>
        /// Lỗi tạm thời (502/503/timeout) thì gọi lại thường thành công ngay, khác hẳn
        /// lỗi hết số dư hay sai khoá - thử lại chỉ tốn thêm thời gian chờ của khách.
        /// </summary>
        public bool IsRetryable { get; set; }
    }

    public interface ILlmClient
    {
        Task<LlmClientResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
    }
}
