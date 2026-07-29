using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TechStoreWeb.Services
{
    public class OpenAiResponsesClient : ILlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly ChatbotOptions _options;
        private readonly ILogger<OpenAiResponsesClient> _logger;

        public OpenAiResponsesClient(HttpClient httpClient, IOptions<ChatbotOptions> options, ILogger<OpenAiResponsesClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<LlmClientResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return new LlmClientResult { Text = null, IsServiceUnavailable = false };
            }

            var endpoint = ResolveEndpoint(_options.ApiUrl);
            var isGoogleApi = endpoint.Contains("generateContent", StringComparison.OrdinalIgnoreCase) ||
                             _options.ApiUrl.Contains("google", StringComparison.OrdinalIgnoreCase);
            var isChatCompletions = endpoint.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase);

            object request;
            if (isGoogleApi)
            {
                request = CreateGoogleRequest(systemPrompt, userPrompt);
            }
            else if (isChatCompletions)
            {
                request = CreateChatCompletionsRequest(systemPrompt, userPrompt);
            }
            else
            {
                request = CreateResponsesRequest(systemPrompt, userPrompt);
            }

            var payload = JsonSerializer.Serialize(request);
            var result = new LlmClientResult { Text = null, IsServiceUnavailable = true, IsRetryable = true };

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                result = await SendOnceAsync(endpoint, payload, isGoogleApi, isChatCompletions, cancellationToken);

                if (!result.IsRetryable || cancellationToken.IsCancellationRequested)
                {
                    return result;
                }

                if (attempt < MaxAttempts)
                {
                    _logger.LogWarning("LLM call failed with a transient error, retrying ({Attempt}/{MaxAttempts}).", attempt, MaxAttempts);
                    await Task.Delay(RetryBackoff * attempt, cancellationToken);
                }
            }

            return result;
        }

        private async Task<LlmClientResult> SendOnceAsync(
            string endpoint,
            string payload,
            bool isGoogleApi,
            bool isChatCompletions,
            CancellationToken cancellationToken)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            if (isGoogleApi)
            {
                // SDK google-genai xac thuc bang header nay; YeScale chap nhan ca hai.
                httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
            }

            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            // Nha cung cap thinh thoang treo hang chuc giay roi moi tra loi, trong khi goi lai
            // thuong xong trong 1-2 giay. Cat ngan tung luot de khach khong ngoi cho vo ich.
            using var attemptTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptTimeout.CancelAfter(AttemptTimeout);

            try
            {
                using var response = await _httpClient.SendAsync(httpRequest, attemptTimeout.Token);
                var body = await response.Content.ReadAsStringAsync(attemptTimeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var isRetryable = IsRetryableStatus(response.StatusCode);
                    _logger.LogWarning("LLM provider returned {StatusCode}: {Body}. Retryable={IsRetryable}",
                        response.StatusCode, body, isRetryable);

                    return new LlmClientResult
                    {
                        Text = null,
                        IsServiceUnavailable = IsServiceUnavailableError(response.StatusCode, body),
                        IsRetryable = isRetryable
                    };
                }

                var text = isGoogleApi ? ExtractGoogleText(body) :
                          isChatCompletions ? ExtractChatCompletionsText(body) :
                          ExtractResponsesText(body);
                return new LlmClientResult { Text = text, IsServiceUnavailable = false, IsRetryable = false };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("LLM call timed out after {Seconds}s.", AttemptTimeout.TotalSeconds);
                return new LlmClientResult { Text = null, IsServiceUnavailable = true, IsRetryable = true };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot call LLM provider.");
                return new LlmClientResult { Text = null, IsServiceUnavailable = true, IsRetryable = true };
            }
        }

        private static bool IsRetryableStatus(System.Net.HttpStatusCode statusCode)
        {
            // 402 het so du va 401/403 sai khoa thi goi lai bao nhieu lan cung the.
            return (int)statusCode >= 500 ||
                   statusCode == System.Net.HttpStatusCode.RequestTimeout ||
                   statusCode == System.Net.HttpStatusCode.TooManyRequests;
        }

        private const int MaxAttempts = 3;
        private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan RetryBackoff = TimeSpan.FromMilliseconds(400);

        private static bool IsServiceUnavailableError(System.Net.HttpStatusCode statusCode, string body)
        {
            if ((int)statusCode >= 500 || statusCode == System.Net.HttpStatusCode.TooManyRequests ||
                statusCode == System.Net.HttpStatusCode.RequestTimeout)
            {
                return true;
            }

            if ((int)statusCode == 429)
            {
                return true;
            }

            if (body.Contains("token", StringComparison.OrdinalIgnoreCase) &&
                (body.Contains("limit", StringComparison.OrdinalIgnoreCase) ||
                 body.Contains("exceeded", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (body.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("quota", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private object CreateGoogleRequest(string systemPrompt, string userPrompt)
        {
            return new
            {
                contents = new object[]
                {
                    new { role = "user", parts = new object[] { new { text = systemPrompt + "\n\n" + userPrompt } } }
                },
                generationConfig = new
                {
                    // Tu van gia va cau hinh la viec chep lai du lieu, khong phai sang tao.
                    temperature = 0.0,
                    maxOutputTokens = 1024
                }
            };
        }

        private object CreateChatCompletionsRequest(string systemPrompt, string userPrompt)
        {
            return new
            {
                model = _options.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3
            };
        }

        private object CreateResponsesRequest(string systemPrompt, string userPrompt)
        {
            return new
            {
                model = _options.Model,
                input = new object[]
                {
                    new
                    {
                        role = "system",
                        content = new object[] { new { type = "input_text", text = systemPrompt } }
                    },
                    new
                    {
                        role = "user",
                        content = new object[] { new { type = "input_text", text = userPrompt } }
                    }
                }
            };
        }

        private static string ResolveEndpoint(string apiUrl)
        {
            var trimmed = apiUrl.TrimEnd('/');
            if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{trimmed}/chat/completions";
            }

            return apiUrl;
        }

        private static string? ExtractGoogleText(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var first = candidates.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Undefined)
                {
                    return null;
                }

                if (!first.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                // Model suy luan tra ve nhieu part; part chi chua thoughtSignature hoac
                // duoc danh dau "thought" khong phai cau tra loi cho khach.
                var builder = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) &&
                        thought.ValueKind == JsonValueKind.True)
                    {
                        continue;
                    }

                    if (part.TryGetProperty("text", out var text))
                    {
                        builder.AppendLine(text.GetString());
                    }
                }

                var result = builder.ToString().Trim();
                return string.IsNullOrWhiteSpace(result) ? null : result;
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractChatCompletionsText(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var first = choices.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }

            return first.TryGetProperty("text", out var text) ? text.GetString() : null;
        }

        private static string? ExtractResponsesText(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("output_text", out var outputText))
            {
                return outputText.GetString();
            }

            if (!document.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var builder = new StringBuilder();
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var text))
                    {
                        builder.AppendLine(text.GetString());
                    }
                }
            }

            var result = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
    }
}
