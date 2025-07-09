using System;
using System.Xml;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    public sealed class XmlContentParser : IContentParser
    {
        public bool CanParse(string ct) =>
            !string.IsNullOrEmpty(ct) &&
            ct.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0;

        public object Parse(string raw, string _)   // same signature
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(raw);
                return doc;
            }
            catch
            {
                // Malformed XML → return raw text so caller can decide.
                return raw;
            }
        }
    }
}
