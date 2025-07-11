using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Fallback parser for unknown or unsupported content types.
    /// Always returns the raw response body as a plain <see cref="string"/>,
    /// preserving all formatting and line breaks.
    /// </summary>
    /// <remarks>
    /// This parser is typically registered last and only used if no specialized parser (e.g., JSON) accepts the content type.
    /// </remarks>
    public sealed class PlainTextParser : IContentParser
    {
        /// <inheritdoc />
        public string ContentType => "text/plain";

        /// <inheritdoc />
        public bool CanParse(string contentType) => true;

        /// <inheritdoc />
        public object Parse(string content, string _) =>
            content ?? string.Empty; // normalize null to empty string
    }
}
