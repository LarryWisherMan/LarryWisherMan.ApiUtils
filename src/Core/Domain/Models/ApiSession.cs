using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Represents a persistent API session that encapsulates base URI, authentication, and default request configuration.
    /// </summary>
    public class ApiSession
    {
        /// <summary>
        /// Unique identifier for the session instance.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Friendly name used to reference this session.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The root URI of the API endpoint for this session.
        /// </summary>
        public Uri BaseUri { get; set; } = new Uri("http://localhost");

        /// <summary>
        /// Default headers that will be applied to all requests using this session.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The User-Agent string sent with requests.
        /// </summary>
        public string UserAgent { get; set; } = "PowerShell/ApiUtils";

        /// <summary>
        /// Optional basic authentication credentials.
        /// </summary>
        public NetworkCredential? Credentials { get; set; }

        /// <summary>
        /// Token used for Bearer or custom token-based authentication.
        /// </summary>
        public string? AuthenticationToken { get; set; }

        /// <summary>
        /// Authentication scheme to use (e.g., Bearer, Basic, Token).
        /// </summary>
        public string AuthenticationScheme { get; set; } = "Bearer";

        /// <summary>
        /// If using an API key, the name of the HTTP header to use (e.g., X-Api-Key).
        /// </summary>
        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

        /// <summary>
        /// Default timeout applied to HTTP requests.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of redirections allowed for requests.
        /// </summary>
        public int MaxRedirections { get; set; } = 5;

        /// <summary>
        /// Whether to skip SSL certificate validation for HTTPS requests.
        /// </summary>
        public bool SkipCertificateCheck { get; set; } = false;

        /// <summary>
        /// The UTC timestamp when the session was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The UTC timestamp of the last time the session was used.
        /// </summary>
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Custom key-value pairs for storing extra metadata or options associated with the session.
        /// </summary>
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}
