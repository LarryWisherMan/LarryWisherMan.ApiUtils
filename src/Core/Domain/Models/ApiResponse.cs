using System;
using System.Collections.Generic;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Represents an HTTP response returned by the API request pipeline.
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse"/> class
        /// with default values for all properties.
        /// </summary>
        public ApiResponse()
        {
            StatusDescription = string.Empty;
            RawContent = string.Empty;
            ParsedContent = new object();
            Headers = new Dictionary<string, string>();
            ContentType = string.Empty;
            SessionName = string.Empty;
            ResponseTime = DateTime.UtcNow;
        }

        /// <summary>
        /// The numeric HTTP status code returned by the server (e.g., 200, 404).
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// A short description of the status code (e.g., "OK", "Not Found").
        /// </summary>
        public string StatusDescription { get; set; }

        /// <summary>
        /// The raw, unparsed response body returned from the server.
        /// </summary>
        public string RawContent { get; set; }

        /// <summary>
        /// The parsed response body (e.g., JSON object, XML, etc.).
        /// </summary>
        public object ParsedContent { get; set; }

        /// <summary>
        /// A dictionary of HTTP response headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Indicates whether the response status code is in the 2xx range.
        /// </summary>
        public bool IsSuccessStatusCode { get; set; }

        /// <summary>
        /// The Content-Type returned in the response (e.g., application/json).
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The name of the session that issued the request.
        /// </summary>
        public string SessionName { get; set; }

        /// <summary>
        /// The timestamp when the response was received.
        /// </summary>
        public DateTime ResponseTime { get; set; }
    }
}
