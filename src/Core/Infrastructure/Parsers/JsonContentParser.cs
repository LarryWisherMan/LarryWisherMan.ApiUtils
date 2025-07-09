using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Parses any <c>*json*</c> content-type into plain .NET objects so the
    /// Core library stays independent of System.Management.Automation.
    /// </summary>
    public sealed class JsonContentParser : IContentParser
    {
        public bool CanParse(string? contentType) =>
            !string.IsNullOrEmpty(contentType) &&
            contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;

        public object? Parse(string raw, string? _) =>
            string.IsNullOrWhiteSpace(raw) ? null : ConvertToken(JToken.Parse(raw));

        /* ---------- helpers ---------- */

        private static object? ConvertToken(JToken token) => token.Type switch
        {
            JTokenType.Object => ConvertObject((JObject)token),
            JTokenType.Array => ConvertArray((JArray)token),
            JTokenType.Integer => (long)token,
            JTokenType.Float => (double)token,
            JTokenType.String => (string)token,
            JTokenType.Boolean => (bool)token,
            JTokenType.Null => null,
            _ => token.ToString() // fallback for Date, Guid, etc.
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
