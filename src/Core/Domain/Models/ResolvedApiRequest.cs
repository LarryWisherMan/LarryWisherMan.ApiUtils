using System;
using System.Collections.Generic;
using System.Net;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Represents a fully constructed API request, ready to be executed.
    /// This model includes resolved values for authentication, headers, timeout, and content handling.
    /// </summary>
    public class ResolvedApiRequest
    {
        /// <summary>
        /// The target URI of the request.
        /// </summary>
        public Uri Uri { get; set; } = new Uri("http://localhost");

        /// <summary>
        /// The HTTP method to use (e.g., GET, POST).
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// The complete set of HTTP headers to include.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The User-Agent string to send with the request.
        /// </summary>
        public string UserAgent { get; set; } = "PowerShell/ApiUtils";

        /// <summary>
        /// The content type of the request body (e.g., application/json).
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// The raw body content of the request.
        /// </summary>
        public object? Body { get; set; }

        /// <summary>
        /// Optional credentials for basic authentication.
        /// </summary>
        public NetworkCredential? Credentials { get; set; }

        /// <summary>
        /// Token string for bearer or custom authentication schemes.
        /// </summary>
        public string? AuthenticationToken { get; set; }

        /// <summary>
        /// The authentication scheme to use (e.g., Bearer, Token, Basic).
        /// </summary>
        public string AuthenticationScheme { get; set; } = "Bearer";

        /// <summary>
        /// The name of the header to use for API key authentication.
        /// </summary>
        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

        /// <summary>
        /// Whether to use default credentials from the system.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// Timeout for the request execution.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of allowed redirections.
        /// </summary>
        public int MaxRedirections { get; set; } = 5;

        /// <summary>
        /// Whether to skip certificate validation (e.g., for self-signed certs).
        /// </summary>
        public bool SkipCertificateValidation { get; set; }

        /// <summary>
        /// Optional path to write the response output to a file.
        /// </summary>
        public string? OutputFilePath { get; set; }

        /// <summary>
        /// The logical name of the session this request is associated with.
        /// </summary>
        public string SessionName { get; set; } = string.Empty;
    }
}
