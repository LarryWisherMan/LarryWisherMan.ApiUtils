using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Fallback parser: returns the raw response body as a <see cref="string"/>,
    /// preserving line‑breaks and whitespace. Registered last in
    /// <see cref="CompositeContentParser"/> so it will only be chosen when
    /// no other specialised parser claims the <c>Content‑Type</c>.
    /// </summary>
    public sealed class PlainTextParser : IContentParser
    {
        // We accept *anything* the specialised parsers did not – therefore we
        // always return true.  CompositeContentParser ensures ordering.
        public bool CanParse(string contentType) => true;

        public object Parse(string content, string _)
            => content ?? string.Empty;            // normalise null → empty
    }
}
