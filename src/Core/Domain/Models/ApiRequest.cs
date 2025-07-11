#nullable enable
using System;
using System.Collections.Generic;
using System.Net;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Represents a request to be made to a REST API, including headers, body, and authentication details.
    /// </summary>
    public class ApiRequest
    {
        /// <summary>
        /// The name of the API session context to use for the request.
        /// </summary>
        public string? SessionName { get; set; }

        /// <summary>
        /// The target URI of the API endpoint.
        /// </summary>
        public Uri? Uri { get; set; }

        /// <summary>
        /// The HTTP method (e.g., GET, POST, PUT). Defaults to "GET".
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// A dictionary of headers to include in the request.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The User-Agent string to use in the request.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// The content type of the request body, if any.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// The body content to include in the request.
        /// </summary>
        public object? Body { get; set; }

        /// <summary>
        /// Credentials to use for the request, if applicable.
        /// </summary>
        public NetworkCredential? Credentials { get; set; }

        /// <summary>
        /// A bearer or custom authentication token to use.
        /// </summary>
        public string? AuthenticationToken { get; set; }

        /// <summary>
        /// The scheme used for authentication (e.g., "Bearer").
        /// </summary>
        public string? AuthenticationScheme { get; set; }

        /// <summary>
        /// Whether to use default credentials.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// The timeout duration for the request.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// The maximum number of redirects to follow.
        /// </summary>
        public int? MaxRedirections { get; set; }

        /// <summary>
        /// Whether to skip SSL certificate validation.
        /// </summary>
        public bool? SkipCertificateValidation { get; set; }

        /// <summary>
        /// Optional path to save the response body to a file.
        /// </summary>
        public string? OutputFilePath { get; set; }
    }
}
