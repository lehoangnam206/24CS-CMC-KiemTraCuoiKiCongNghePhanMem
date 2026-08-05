namespace TechStoreWeb.Services
{
    public class LlmClientResult
    {
        public string? Text { get; set; }
        public bool IsServiceUnavailable { get; set; }

        public bool IsRetryable { get; set; }
    }

    public interface ILlmClient
    {
        Task<LlmClientResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
    }
}
