using System;
using System.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    public class CompositeContentParser : IContentParser
    {
        private readonly System.Collections.Generic.List<IContentParser> _parsers;

        public CompositeContentParser()
        {
            _parsers = new System.Collections.Generic.List<IContentParser>
            {
                new JsonContentParser(),
                new XmlContentParser()
            };
        }

        public bool CanParse(string contentType)
        {
            return _parsers.Any(p => p.CanParse(contentType));
        }

        public object Parse(string content, string contentType)
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(contentType));
            return parser?.Parse(content, contentType) ?? content;
        }
    }
}
