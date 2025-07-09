using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace LarryWisherMan.ApiUtils.Domain.Models
{
    public class ResolvedApiRequest
    {
        public Uri Uri { get; set; }
        public string Method { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public string UserAgent { get; set; }
        public string ContentType { get; set; }
        public object Body { get; set; }
        public NetworkCredential Credentials { get; set; }
        public string AuthenticationToken { get; set; }
        public string AuthenticationScheme { get; set; }
        public string ApiKeyHeaderName { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public TimeSpan Timeout { get; set; }
        public int MaxRedirections { get; set; }
        public bool SkipCertificateValidation { get; set; }
        public string OutputFilePath { get; set; }
        public string SessionName { get; set; }
    }
}
