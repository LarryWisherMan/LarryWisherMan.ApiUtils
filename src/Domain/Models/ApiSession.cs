using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Represents a persistent API session with authentication and configuration
    /// </summary>
    public class ApiSession
    {
        public string Name { get; set; }
        public Uri BaseUri { get; set; }
        public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
        public string UserAgent { get; set; } = "Powershell/ApiUtils";
        public NetworkCredential Credentials { get; set; }
        public string AuthenticationToken { get; set; }
        public string AuthenticationScheme { get; set; } = "Bearer";
        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRedirections { get; set; } = 5;
        public bool SkipCertificateCheck { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}
