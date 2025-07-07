using System;
using System.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    public class XmlContentParser : IContentParser
    {
        public bool CanParse(string contentType)
        {
            return contentType?.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public object Parse(string content, string contentType)
        {
            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(content);
                return xmlDoc;
            }
            catch
            {
                return content;
            }
        }
    }
}
