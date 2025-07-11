using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Parses <c>application/json</c> content into .NET primitives, dictionaries, and lists.
    /// </summary>
    public sealed class JsonContentParser : IContentParser
    {
        /// <inheritdoc />
        public string ContentType => "application/json";

        /// <inheritdoc />
        public bool CanParse(string contentType)
        {
            // Defensive null handling even though interface says it's non-nullable
            return !string.IsNullOrWhiteSpace(contentType) &&
                contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <inheritdoc />
        public object Parse(string content, string contentType)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Safe: content is not null/empty here
            var token = JToken.Parse(content);
            return ConvertToken(token);
        }

        private static object ConvertToken(JToken token) => token.Type switch
        {
            JTokenType.Object => ConvertObject((JObject)token),
            JTokenType.Array => ConvertArray((JArray)token),
            JTokenType.Integer => (long)token,
            JTokenType.Float => (double)token,
            JTokenType.String => token.ToString(),
            JTokenType.Boolean => (bool)token,
            JTokenType.Null => string.Empty,
            _ => token.ToString()
        };


        private static Dictionary<string, object?> ConvertObject(JObject obj)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in obj.Properties())
                dict[prop.Name] = ConvertToken(prop.Value);
            return dict;
        }

        private static List<object?> ConvertArray(JArray array)
        {
            var list = new List<object?>(array.Count);
            foreach (var item in array)
                list.Add(ConvertToken(item));
            return list;
        }
    }
}
