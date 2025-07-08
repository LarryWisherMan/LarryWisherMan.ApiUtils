namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Net.Http;
    using LarryWisherMan.ApiUtils.Domain.Models;
    using LarryWisherMan.ApiUtils.Infrastructure.Logging;

    /// <summary>
    /// Base class for HTTP API cmdlets with lightweight logging
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
                Logger.LogInformation("Processing HTTP request...");

                // Pre-resolve session to avoid nested async calls
                var session = ResolveSession();

                var request = CreateApiRequest();
                LogRequestDetails(request);

                var options = CreateRequestOptions();
                Logger.LogDebug("Request options: ParseContent={0}, ThrowOnError={1}",
                    options.ParseContent, options.ThrowOnError);

                // Use ConfigureAwait(false) to prevent deadlock in PS 5.1
                var response = SessionService.InvokeAsync(request, options)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                LogResponseDetails(response);
                ProcessApiResponse(response, options);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HTTP request failed: {0}", ex.Message);
                WriteError(new ErrorRecord(ex, GetErrorId(), ErrorCategory.NotSpecified, this));
            }
        }

        private void LogRequestDetails(ApiRequest request)
        {
            // Build full URL for logging
            var session = ResolveSession();
            var baseUri = session?.BaseUri?.ToString() ?? "NO-SESSION";
            var fullUrl = request.Uri != null ?
                new Uri(new Uri(baseUri), request.Uri).ToString() :
                $"{baseUri}/NO-URI";

            Logger.LogHttpRequest(request.Method, fullUrl);

            // Log authentication info (without exposing tokens)
            if (!string.IsNullOrEmpty(request.AuthenticationToken))
            {
                var tokenPreview = request.AuthenticationToken.Length > 10 ?
                    request.AuthenticationToken.Substring(0, 10) + "..." :
                    "[SHORT-TOKEN]";
                Logger.LogDebug("Auth: {0} {1}", request.AuthenticationScheme, tokenPreview);
            }
            else if (session?.AuthenticationToken != null)
            {
                var sessionTokenPreview = session.AuthenticationToken.Length > 10 ?
                    session.AuthenticationToken.Substring(0, 10) + "..." :
                    "[SHORT-TOKEN]";
                Logger.LogDebug("Session Auth: {0} {1}", session.AuthenticationScheme, sessionTokenPreview);
            }
            else
            {
                Logger.LogDebug("No authentication configured");
            }

            // Log headers (be careful with sensitive data)
            if (request.Headers?.Count > 0)
            {
                Logger.LogDebug("Request Headers:");
                foreach (var header in request.Headers)
                {
                    var value = header.Key.ToLowerInvariant().Contains("auth") ||
                               header.Key.ToLowerInvariant().Contains("token") ?
                               "[REDACTED]" : header.Value;
                    Logger.LogDebug("  {0}: {1}", header.Key, value);
                }
            }

            // Log body (truncated for safety)
            if (request.Body != null)
            {
                var bodyStr = request.Body.ToString();
                if (bodyStr.Length > 500)
                {
                    bodyStr = bodyStr.Substring(0, 500) + "... (truncated)";
                }
                Logger.LogDebug("Request Body: {0}", bodyStr);
            }

            // Log other settings
            if (request.Timeout.HasValue)
            {
                Logger.LogDebug("Timeout: {0}s", request.Timeout.Value.TotalSeconds);
            }

            if (!string.IsNullOrEmpty(request.ContentType))
            {
                Logger.LogDebug("Content-Type: {0}", request.ContentType);
            }
        }

        private void LogResponseDetails(ApiResponse response)
        {
            Logger.LogHttpResponse(response.StatusCode, response.StatusDescription, response.RawContent, response.Headers);

            if (!string.IsNullOrEmpty(response.ContentType))
            {
                Logger.LogDebug("Response Content-Type: {0}", response.ContentType);
            }

            if (response.RawContent != null)
            {
                Logger.LogDebug("Response Size: {0} characters", response.RawContent.Length);
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

            // --- NEW LOGIC: Default ContentType if POST/PUT and body ---
            string contentType = ContentType;
            if (string.IsNullOrWhiteSpace(contentType) &&
                (Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)) &&
                Body != null &&
                !(Body is byte[]))
            {
                contentType = "application/json";
            }

            var request = new ApiRequest
            {
                SessionName = ResolveSessionName(),
                Uri = Uri,
                Method = Method,
                Headers = headers,
                UserAgent = UserAgent,
                ContentType = contentType,
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

            Logger.LogDebug("Created API request for session: {0} (Content-Type: {1})", request.SessionName ?? "NONE", contentType ?? "(null)");
            return request;
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

                // Include response content in error for debugging
                if (!string.IsNullOrEmpty(response.RawContent) && response.RawContent.Length < 1000)
                {
                    errorMsg += $"\nResponse: {response.RawContent}";
                    Logger.LogWarning("Error response content: {0}", response.RawContent);
                }

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
                var output = GetResponseOutput(response);
                Logger.LogDebug("Returning response output (Type: {0})", output?.GetType().Name ?? "null");
                WriteObject(output);
            }
            else
            {
                Logger.LogInformation("Response saved to file: {0}", OutFile);
            }
        }

        // Abstract/virtual methods for customization
        protected abstract bool ShouldParseContent();
        protected abstract object GetResponseOutput(ApiResponse response);
        protected abstract string GetErrorId();
    }
}
