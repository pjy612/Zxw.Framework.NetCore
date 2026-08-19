using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zxw.Framework.AI.Abstractions
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public static ChatMessage System(string content) => new ChatMessage { Role = "system", Content = content };
        public static ChatMessage User(string content) => new ChatMessage { Role = "user", Content = content };
        public static ChatMessage Assistant(string content) => new ChatMessage { Role = "assistant", Content = content };
    }

    public class ChatCompletionRequest
    {
        /// <summary>
        /// 为空则使用 <see cref="Options.OrcaRouterOptions.DefaultModel"/>。
        /// </summary>
        public string Model { get; set; }

        public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public double? Temperature { get; set; }

        public int? MaxTokens { get; set; }

        /// <summary>
        /// Fallback 模型链；为空则使用 Options 中的 FallbackModels。
        /// </summary>
        public IList<string> FallbackModels { get; set; }
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public IList<ChatChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public ChatUsage Usage { get; set; }

        public string GetContent()
        {
            if (Choices == null || Choices.Count == 0 || Choices[0]?.Message == null)
                return null;
            return Choices[0].Message.Content;
        }
    }

    public class ChatChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public ChatMessage Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }

        [JsonPropertyName("delta")]
        public ChatMessage Delta { get; set; }
    }

    public class ChatUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
