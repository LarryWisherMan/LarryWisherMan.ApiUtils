using System;
using System.Collections.Generic;
using System.Net;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    public class ApiRequest
    {
        public string SessionName { get; set; }
        public Uri Uri { get; set; }
        public string Method { get; set; } = "GET";
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string UserAgent { get; set; }
        public string ContentType { get; set; }
        public object Body { get; set; }
        public NetworkCredential Credentials { get; set; }
        public string AuthenticationToken { get; set; }
        public string AuthenticationScheme { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public TimeSpan? Timeout { get; set; }
        public int? MaxRedirections { get; set; }
        public bool? SkipCertificateValidation { get; set; }
        public string OutputFilePath { get; set; }
    }
}
