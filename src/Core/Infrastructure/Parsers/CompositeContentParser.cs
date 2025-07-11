using System;
using System.Collections.Generic;
using System.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Composes multiple content parsers and delegates parsing to the first one
    /// that supports the given content type. Falls back to the last parser when no match is found.
    /// </summary>
    /// <remarks>
    /// This composite is intended to be used via DI with all <see cref="IContentParser"/> implementations registered.
    /// The fallback parser (typically <see cref="PlainTextParser"/>) should be registered last.
    /// </remarks>
    public sealed class CompositeContentParser : IContentParser
    {
        private readonly IReadOnlyList<IContentParser> _parsers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeContentParser"/> class.
        /// </summary>
        /// <param name="parsers">The ordered list of available content parsers. The last parser should act as a fallback.</param>
        /// <exception cref="ArgumentException">Thrown when no parsers are provided.</exception>
        public CompositeContentParser(IEnumerable<IContentParser>? parsers)
        {
            if (parsers is null || !parsers.Any())
                throw new ArgumentException("At least one content parser must be provided.", nameof(parsers));

            _parsers = parsers.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public string ContentType => "composite";

        /// <inheritdoc />
        public bool CanParse(string contentType)
        {
            var safeType = contentType ?? string.Empty;
            return _parsers.Any(p => p.CanParse(safeType));
        }

        /// <inheritdoc />
        public object Parse(string content, string contentType)
        {
            var safeType = contentType ?? string.Empty;

            // Fallback to last parser (typically PlainTextParser) if none match
            var parser = _parsers.FirstOrDefault(p => p.CanParse(safeType))
                    ?? _parsers.Last();

            return parser.Parse(content ?? string.Empty, safeType);
        }
    }
}
