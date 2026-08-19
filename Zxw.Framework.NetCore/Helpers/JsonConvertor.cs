using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zxw.Framework.NetCore.Helpers
{
    /// <summary>
    /// JSON serialization helper based on System.Text.Json.
    /// </summary>
    public static class JsonConvertor
    {
        private static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

        private static JsonSerializerOptions CreateDefaultOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public static string Serialize(object source, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(source, options ?? DefaultOptions);
        }

        public static string Serialize<T>(T source, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(source, options ?? DefaultOptions);
        }

        public static T Deserialize<T>(string source, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize<T>(source, options ?? DefaultOptions);
        }

        public static object Deserialize(string source, Type destinationType, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize(source, destinationType, options ?? DefaultOptions);
        }
    }
}
