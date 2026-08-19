using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Zxw.Framework.AI.Abstractions;
using Zxw.Framework.AI.Options;

namespace Zxw.Framework.AI.Clients
{
    /// <summary>
    /// OrcaRouter OpenAI 兼容 Chat Completions 客户端。
    /// </summary>
    public class OrcaRouterChatClient : IChatClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OrcaRouterOptions _options;

        public OrcaRouterChatClient(IHttpClientFactory httpClientFactory, IOptions<OrcaRouterOptions> options)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<ChatCompletionResponse> CompleteAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            Validate(request);

            var client = CreateClient();
            using var httpRequest = CreateHttpRequest(request, stream: false);
            using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"OrcaRouter chat completion failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(body, 500)}");
            }

            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonOptions);
            if (result == null)
                throw new InvalidOperationException("OrcaRouter 返回了空的 chat completion 响应。");
            return result;
        }

        public async IAsyncEnumerable<string> CompleteStreamingAsync(
            ChatCompletionRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Validate(request);

            var client = CreateClient();
            using var httpRequest = CreateHttpRequest(request, stream: true);
            using var response = await client
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"OrcaRouter streaming failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(errorBody, 500)}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                    yield break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (!line.StartsWith("data:", StringComparison.Ordinal))
                    continue;

                var payload = line.Substring("data:".Length).Trim();
                if (payload == "[DONE]")
                    yield break;

                ChatCompletionResponse chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<ChatCompletionResponse>(payload, JsonOptions);
                }
                catch (JsonException)
                {
                    continue;
                }

                var delta = chunk?.Choices != null && chunk.Choices.Count > 0
                    ? chunk.Choices[0].Delta?.Content
                    : null;
                if (!string.IsNullOrEmpty(delta))
                    yield return delta;
            }
        }

        private static void Validate(ChatCompletionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Messages == null || request.Messages.Count == 0)
                throw new ArgumentException("Messages 不能为空。", nameof(request));
        }

        private HttpClient CreateClient()
        {
            var apiKey = _options.ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    $"未配置 OrcaRouter API Key。请设置 {nameof(OrcaRouterOptions)}.{nameof(OrcaRouterOptions.ApiKey)} 或环境变量 {OrcaRouterOptions.ApiKeyEnvironmentVariable}。");
            }

            var client = _httpClientFactory.CreateClient(OrcaRouterOptions.HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return client;
        }

        private HttpRequestMessage CreateHttpRequest(ChatCompletionRequest request, bool stream)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _options.DefaultModel : request.Model;
            var fallback = request.FallbackModels != null && request.FallbackModels.Count > 0
                ? request.FallbackModels
                : _options.FallbackModels;

            var payload = new ChatCompletionPayload
            {
                Model = model,
                Messages = request.Messages,
                Stream = stream,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Models = fallback != null && fallback.Count > 0 ? fallback : null
            };

            var baseUrl = (_options.BaseUrl ?? OrcaRouterOptions.DefaultBaseUrl).TrimEnd('/');
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };

            var referer = _options.ResolveHttpReferer();
            if (!string.IsNullOrWhiteSpace(referer))
                httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", referer);
            if (!string.IsNullOrWhiteSpace(_options.AppTitle))
                httpRequest.Headers.TryAddWithoutValidation("X-Title", _options.AppTitle);

            return httpRequest;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value;
            return value.Substring(0, max) + "...";
        }

        private sealed class ChatCompletionPayload
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("messages")]
            public IList<ChatMessage> Messages { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }

            [JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; set; }

            /// <summary>
            /// OrcaRouter fallback 链（对应 SDK extra_body.models）。
            /// </summary>
            [JsonPropertyName("models")]
            public IList<string> Models { get; set; }
        }
    }
}
