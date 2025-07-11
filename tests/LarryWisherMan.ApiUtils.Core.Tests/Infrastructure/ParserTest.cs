using Xunit;
using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class ParserTests
    {
        [Fact]
        public void JsonParser_Should_Parse_Object_To_Dictionary()
        {
            var parser = new JsonContentParser();
            string json = "{\"name\":\"Mike\",\"age\":30}";

            var result = parser.Parse(json, "application/json") as Dictionary<string, object?>;

            Assert.NotNull(result);
            Assert.Equal("Mike", result!["name"]);
            Assert.Equal(30L, result["age"]);
        }

        [Fact]
        public void JsonParser_Should_Return_EmptyString_For_Empty()
        {
            var parser = new JsonContentParser();
            var result = parser.Parse("", "application/json");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void XmlParser_Should_Return_XmlDocument()
        {
            var parser = new XmlContentParser();
            string xml = "<root><name>Mike</name></root>";

            var result = parser.Parse(xml, "application/xml");

            var doc = Assert.IsType<XmlDocument>(result);
            Assert.Equal("Mike", doc.SelectSingleNode("//name")?.InnerText);
        }

        [Fact]
        public void XmlParser_Should_Return_Raw_On_Malformed_Xml()
        {
            var parser = new XmlContentParser();
            string malformed = "<xml><oops></xml>";

            var result = parser.Parse(malformed, "application/xml");

            Assert.IsType<string>(result);
            Assert.Equal(malformed, result);
        }

        [Fact]
        public void PlainTextParser_Should_Normalize_Null()
        {
            var parser = new PlainTextParser();
            var result = parser.Parse(null!, "text/plain");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CompositeParser_Should_Use_First_Matching_Parser()
        {
            var json = new JsonContentParser();
            var xml = new XmlContentParser();
            var plain = new PlainTextParser();
            var composite = new CompositeContentParser(new IContentParser[] { json, xml, plain });

            string input = "{\"hello\":\"world\"}";
            var result = composite.Parse(input, "application/json");

            Assert.IsType<Dictionary<string, object?>>(result);
        }

        [Fact]
        public void CompositeParser_Should_Fallback_When_No_Parser_Matches()
        {
            var fallback = new PlainTextParser();
            var composite = new CompositeContentParser(new[] { fallback });

            string input = "just some plain text";
            var result = composite.Parse(input, "application/unknown");

            Assert.Equal(input, result);
        }

        [Fact]
        public void CompositeParser_Should_Throw_If_No_Parsers_Provided()
        {
            Assert.Throws<ArgumentException>(() => new CompositeContentParser(null));
            Assert.Throws<ArgumentException>(() => new CompositeContentParser(new List<IContentParser>()));
        }
    }
}
