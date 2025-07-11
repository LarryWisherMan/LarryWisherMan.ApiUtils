using System;
using System.Xml;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// Parses content with XML media types (e.g., <c>application/xml</c>, <c>text/xml</c>) into an <see cref="XmlDocument"/>.
    /// </summary>
    /// <remarks>
    /// If the XML is malformed or invalid, the raw string content is returned instead of throwing.
    /// </remarks>
    public sealed class XmlContentParser : IContentParser
    {
        /// <inheritdoc />
        public string ContentType => "application/xml";

        /// <inheritdoc />
        public bool CanParse(string ct) =>
            !string.IsNullOrWhiteSpace(ct) &&
            ct.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <inheritdoc />
        public object Parse(string raw, string _)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty; // avoid returning null

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(raw);
                return doc;
            }
            catch
            {
                // Malformed XML → return raw content (ensuring it's non-null)
                return (object)raw;
            }
        }
    }
}
