using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net.Http;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Base class for HTTP API cmdlets with common parameters
    /// </summary>
    public abstract class HttpApiCmdletBase : SessionInputCmdletBase
    {
        #region Common HTTP Parameters

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
                var options = CreateRequestOptions();

                var response = SessionService.InvokeAsync(request, options)
                    .GetAwaiter()
                    .GetResult();

                ProcessApiResponse(response, options);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, GetErrorId(), ErrorCategory.NotSpecified, this));
            }
        }

        protected virtual ApiRequest CreateApiRequest()
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
                SessionName = ResolveSessionName(),
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

        protected virtual ApiRequestOptions CreateRequestOptions()
        {
            return new ApiRequestOptions
            {
                PassThrough = PassThru,
                ThrowOnError = false,
                ParseContent = ShouldParseContent()
            };
        }

        protected virtual void ProcessApiResponse(ApiResponse response, ApiRequestOptions options)
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
                WriteObject(GetResponseOutput(response));
            }
        }

        // Abstract/virtual methods for customization
        protected abstract bool ShouldParseContent();
        protected abstract object GetResponseOutput(ApiResponse response);
        protected abstract string GetErrorId();
    }
}
