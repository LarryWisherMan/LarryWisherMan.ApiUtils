using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net.Http;
using LarryWisherMan.ApiUtils.Domain.Models;



namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Invoke-ApiRestMethod - Session-aware REST method (like Invoke-RestMethod)
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ApiRestMethod")]
    public class InvokeApiRestMethodCommand : SessionApiCmdletBase
    {
        #region Parameters

        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string SessionName { get; set; }

        [Parameter(Position = 1)]
        public Uri Uri { get; set; }

        [Parameter]
        public string Method { get; set; } = "GET";

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        public string UserAgent { get; set; }

        [Parameter]
        public string ContentType { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public object Body { get; set; }

        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public string AuthToken { get; set; }

        [Parameter]
        public string AuthScheme { get; set; }

        [Parameter]
        public SwitchParameter UseDefaultCredentials { get; set; }

        [Parameter]
        public int TimeoutSec { get; set; }

        [Parameter]
        public int MaximumRedirection { get; set; }

        [Parameter]
        public SwitchParameter SkipCertificateCheck { get; set; }

        [Parameter]
        public string OutFile { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion

        protected override void ProcessRecord()
        {
            try
            {
                var request = CreateApiRequest();
                var options = new ApiRequestOptions
                {
                    PassThrough = PassThru,
                    ThrowOnError = false,
                    ParseContent = true // Parse content for RestMethod-style cmdlet
                };

                var response = SessionService.InvokeAsync(request, options)
                    .GetAwaiter()
                    .GetResult();

                ProcessApiResponse(response, options);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ApiRestMethodError", ErrorCategory.NotSpecified, this));
            }
        }

        private ApiRequest CreateApiRequest()
        {
            var headers = new Dictionary<string, string>();
            if (Headers != null)
            {
                foreach (DictionaryEntry header in Headers)
                {
                    headers[header.Key.ToString()] = header.Value?.ToString();
                }
            }

            return new ApiRequest
            {
                SessionName = SessionName,
                Uri = Uri,
                Method = Method,
                Headers = headers,
                UserAgent = UserAgent,
                ContentType = ContentType,
                Body = Body,
                Credentials = Credential?.GetNetworkCredential(),
                AuthenticationToken = AuthToken,
                AuthenticationScheme = AuthScheme,
                UseDefaultCredentials = UseDefaultCredentials,
                Timeout = TimeoutSec > 0 ? TimeSpan.FromSeconds(TimeoutSec) : null,
                MaxRedirections = MaximumRedirection > 0 ? MaximumRedirection : null,
                SkipCertificateValidation = SkipCertificateCheck.IsPresent ? (bool?)true : null,
                OutputFilePath = OutFile
            };
        }

        private void ProcessApiResponse(ApiResponse response, ApiRequestOptions options)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"HTTP Error {response.StatusCode}: {response.StatusDescription}";
                var errorRecord = new ErrorRecord(
                    new HttpRequestException(errorMsg),
                    "HttpError",
                    ErrorCategory.ProtocolError,
                    response
                );
                WriteError(errorRecord);
                return;
            }

            if (string.IsNullOrEmpty(OutFile) || PassThru)
            {
                WriteObject(response.ParsedContent);
            }
        }
    }
}

