using System;
using System.Collections.Generic;
using System.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Delegates parsing to the first contained parser that supports
    /// a given content type; defaults to PlainTextParser when none match.
    /// </summary>
    public sealed class CompositeContentParser : IContentParser
    {
        private readonly IReadOnlyList<IContentParser> _parsers;

        public CompositeContentParser(IEnumerable<IContentParser>? parsers = null)
        {
            _parsers = (parsers ?? DefaultParsers).ToList().AsReadOnly();
        }

        private static readonly IContentParser[] DefaultParsers =
        {
            new JsonContentParser(),
            new XmlContentParser(),
            new PlainTextParser()   // always returns raw string
        };

        public bool CanParse(string? contentType) =>
            _parsers.Any(p => p.CanParse(contentType));

        public object? Parse(string content, string? contentType)
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(contentType))
                      ?? _parsers.Last(); // PlainTextParser fallback
            return parser.Parse(content, contentType);
        }
    }
}
