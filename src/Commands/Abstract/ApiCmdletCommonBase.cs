using System;
using System.Collections;
using System.Management.Automation;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Defines all common parameters for API cmdlets.
    /// </summary>
    public abstract class ApiCmdletCommonBase : PSCmdlet
    {
        [Parameter(Position = 1)]
        [ValidateNotNull]
        public Uri Uri { get; set; }

        [Parameter]
        [ValidateSet("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", IgnoreCase = true)]
        public string Method { get; set; } = "GET";
        // Restricts to only HTTP verbs

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        public string UserAgent { get; set; }

        [Parameter]
        [ValidateSet("application/json", "application/xml", "application/x-www-form-urlencoded", "text/plain", "multipart/form-data")]
        public string ContentType { get; set; }
        // Tab-completes common types, but user can always override in the pipeline

        [Parameter(ValueFromPipeline = true)]
        public object Body { get; set; }

        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public string AuthToken { get; set; }

        [Parameter]
        [ValidateSet("Bearer", "Basic", "ApiKey", IgnoreCase = true)]
        public string AuthScheme { get; set; }
        // Most APIs use Bearer, Basic, or API Key (case-insensitive)

        [Parameter]
        public SwitchParameter UseDefaultCredentials { get; set; }

        [Parameter]
        [ValidateRange(1, 3600)]
        public int TimeoutSec { get; set; }
        // Prevents accidental zero/negative timeouts; 1s to 1 hour

        [Parameter]
        [ValidateRange(1, 100)]
        public int MaximumRedirection { get; set; }
        // 1 to 100 is a safe, normal range for most APIs

        [Parameter]
        public SwitchParameter SkipCertificateCheck { get; set; }

        [Parameter]
        [ValidatePattern(@"^.*\.(json|xml|txt|csv|log)$")]
        public string OutFile { get; set; }
        // Only allows file names with standard extensions (user can override with pipeline)

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter EnableDebugLogging { get; set; }
    }
}
