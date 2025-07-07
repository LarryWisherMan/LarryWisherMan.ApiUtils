using System;
using System.Collections.Generic;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Represents an HTTP response with parsed content
    /// </summary>
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string RawContent { get; set; }
        public object ParsedContent { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string ContentType { get; set; }
        public string SessionName { get; set; }
        public DateTime ResponseTime { get; set; }

    }
}
