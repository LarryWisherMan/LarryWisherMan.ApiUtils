// ============================================================================
// DOMAIN LAYER - Core Business Logic and Session Management
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;





// ============================================================================
// INFRASTRUCTURE LAYER - Session Persistence and HTTP Services
// ============================================================================

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using LarryWisherMan.ApiUtils.Domain.Interfaces;
    using LarryWisherMan.ApiUtils.Domain.Models;

    /// <summary>
    /// File-based session repository for persistence
    /// </summary>
    public class FileSessionRepository : ISessionRepository
    {
        private readonly string _sessionDirectory;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public FileSessionRepository(string sessionDirectory = null)
        {
            _sessionDirectory = sessionDirectory ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            ".apiutils", "sessions");

            Directory.CreateDirectory(_sessionDirectory);
        }

        public async Task<ApiSession> GetSessionAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ApiSession>(json, JsonOptions);
        }

        public async Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            var sessions = new List<ApiSession>();
            var files = Directory.GetFiles(_sessionDirectory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var session = JsonSerializer.Deserialize<ApiSession>(json, JsonOptions);
                    sessions.Add(session);
                }
                catch
                {
                    // Skip corrupted session files
                }
            }

            return sessions;
        }

        public async Task SaveSessionAsync(ApiSession session)
        {
            var filePath = GetSessionFilePath(session.Name);
            var json = JsonSerializer.Serialize(session, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task DeleteSessionAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            await Task.CompletedTask;
        }

        public async Task<bool> SessionExistsAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            return File.Exists(filePath);
        }

        public async Task UpdateLastUsedAsync(string name)
        {
            var session = await GetSessionAsync(name);
            if (session != null)
            {
                session.LastUsed = DateTime.UtcNow;
                await SaveSessionAsync(session);
            }
        }

        private string GetSessionFilePath(string name)
        {
            var fileName = $"{name}.json";
            return Path.Combine(_sessionDirectory, fileName);
        }
    }


}

namespace LarryWisherMan.ApiUtils.Infrastructure.Services
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using LarryWisherMan.ApiUtils.Domain.Interfaces;
    using LarryWisherMan.ApiUtils.Domain.Models;

    /// <summary>
    /// Session-aware HTTP client service
    /// </summary>
    public class SessionHttpService : ISessionHttpService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _handler;
        private bool _disposed = false;

        public SessionHttpService()
        {
            _handler = new HttpClientHandler();
            _httpClient = new HttpClient(_handler);
        }

        public async Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request)
        {
            ConfigureHandler(request);
            ConfigureClient(request);

            using var httpRequest = CreateHttpRequestMessage(request);
            return await _httpClient.SendAsync(httpRequest);
        }

        private void ConfigureHandler(ResolvedApiRequest request)
        {
            _handler.MaxAutomaticRedirections = request.MaxRedirections;

            if (request.SkipCertificateValidation)
            {
                _handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }

            if (request.Credentials != null)
            {
                _handler.Credentials = request.Credentials;
            }
            else if (request.UseDefaultCredentials)
            {
                _handler.UseDefaultCredentials = true;
            }
        }

        private void ConfigureClient(ResolvedApiRequest request)
        {
            _httpClient.Timeout = request.Timeout;

            // Clear and set user agent
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(request.UserAgent);
        }

        private HttpRequestMessage CreateHttpRequestMessage(ResolvedApiRequest request)
        {
            var httpMethod = new HttpMethod(request.Method.ToUpperInvariant());
            var httpRequest = new HttpRequestMessage(httpMethod, request.Uri);

            // Add all headers including authentication
            AddHeaders(httpRequest, request);
            AddAuthentication(httpRequest, request);

            // Add content if present
            if (request.Body != null)
            {
                httpRequest.Content = CreateHttpContent(request.Body, request.ContentType);
            }

            return httpRequest;
        }

        private void AddHeaders(HttpRequestMessage httpRequest, ResolvedApiRequest request)
        {
            foreach (var header in request.Headers)
            {
                try
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
                catch
                {
                    // Will try to add as content header if needed
                }
            }
        }

        private void AddAuthentication(HttpRequestMessage httpRequest, ResolvedApiRequest request)
        {
            if (!string.IsNullOrEmpty(request.AuthenticationToken))
            {
                switch (request.AuthenticationScheme?.ToLowerInvariant())
                {
                    case "bearer":
                        httpRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.AuthenticationToken);
                        break;
                    case "basic":
                        httpRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", request.AuthenticationToken);
                        break;
                    case "apikey":
                        httpRequest.Headers.Add(request.ApiKeyHeaderName ?? "X-API-Key", request.AuthenticationToken);
                        break;
                    default:
                        httpRequest.Headers.Add("Authorization", $"{request.AuthenticationScheme} {request.AuthenticationToken}");
                        break;
                }
            }
        }

        private HttpContent CreateHttpContent(object body, string contentType)
        {
            HttpContent content = body switch
            {
                string stringBody => new StringContent(stringBody, System.Text.Encoding.UTF8),
                byte[] byteBody => new ByteArrayContent(byteBody),
                IDictionary dictionaryBody => CreateFormContent(dictionaryBody),
                _ => new StringContent(body.ToString(), System.Text.Encoding.UTF8)
            };

            if (!string.IsNullOrEmpty(contentType))
            {
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }

            return content;
        }

        private HttpContent CreateFormContent(IDictionary dictionary)
        {
            var formData = new List<string>();
            foreach (DictionaryEntry kvp in dictionary)
            {
                var key = System.Web.HttpUtility.UrlEncode(kvp.Key?.ToString() ?? string.Empty);
                var value = System.Web.HttpUtility.UrlEncode(kvp.Value?.ToString() ?? string.Empty);
                formData.Add($"{key}={value}");
            }

            var content = string.Join("&", formData);
            var stringContent = new StringContent(content, System.Text.Encoding.UTF8);
            stringContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
            return stringContent;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _handler?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// File service implementation
    /// </summary>
    public class FileService : IFileService
    {
        public async Task SaveStreamToFileAsync(Stream stream, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
        }

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetDirectoryName(string filePath) => Path.GetDirectoryName(filePath);
    }
}

// ============================================================================
// APPLICATION LAYER - Session Management and Request Resolution
// ============================================================================

namespace LarryWisherMan.ApiUtils.Application.Services
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using LarryWisherMan.ApiUtils.Domain.Interfaces;
    using LarryWisherMan.ApiUtils.Domain.Models;

    /// <summary>
    /// Resolves API requests by merging session data with request overrides
    /// </summary>
    public class RequestResolver : IRequestResolver
    {
        private readonly ISessionRepository _sessionRepository;

        public RequestResolver(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request)
        {
            ApiSession session = null;

            if (!string.IsNullOrEmpty(request.SessionName))
            {
                session = await _sessionRepository.GetSessionAsync(request.SessionName);
                if (session == null)
                {
                    throw new ArgumentException($"Session '{request.SessionName}' not found");
                }
            }

            return MergeRequestWithSession(request, session);
        }

        private ResolvedApiRequest MergeRequestWithSession(ApiRequest request, ApiSession session)
        {
            var resolved = new ResolvedApiRequest
            {
                SessionName = request.SessionName,
                Method = request.Method,
                OutputFilePath = request.OutputFilePath
            };

            // Resolve URI (combine base URI from session with request URI)
            resolved.Uri = ResolveUri(request.Uri, session?.BaseUri);

            // Merge headers (session defaults + request overrides)
            resolved.Headers = MergeHeaders(session?.DefaultHeaders, request.Headers);

            // Use request values, fall back to session, then defaults
            resolved.UserAgent = request.UserAgent ?? session?.UserAgent ?? "PowerShell/ApiUtils";
            resolved.ContentType = request.ContentType;
            resolved.Body = request.Body;
            resolved.Timeout = request.Timeout ?? session?.DefaultTimeout ?? TimeSpan.FromSeconds(30);
            resolved.MaxRedirections = request.MaxRedirections ?? session?.MaxRedirections ?? 5;
            resolved.SkipCertificateValidation = request.SkipCertificateValidation ?? session?.SkipCertificateValidation ?? false;

            // Authentication resolution
            ResolveAuthentication(request, session, resolved);

            return resolved;
        }

        private Uri ResolveUri(Uri requestUri, Uri sessionBaseUri)
        {
            if (requestUri == null)
                throw new ArgumentException("Request URI is required");

            if (requestUri.IsAbsoluteUri)
                return requestUri;

            if (sessionBaseUri != null)
                return new Uri(sessionBaseUri, requestUri);

            throw new ArgumentException("Relative URI requires a session with BaseUri");
        }

        private Dictionary<string, string> MergeHeaders(IDictionary<string, string> sessionHeaders, IDictionary<string, string> requestHeaders)
        {
            var merged = new Dictionary<string, string>();

            // Add session headers first
            if (sessionHeaders != null)
            {
                foreach (var header in sessionHeaders)
                {
                    merged[header.Key] = header.Value;
                }
            }

            // Override with request headers
            if (requestHeaders != null)
            {
                foreach (var header in requestHeaders)
                {
                    merged[header.Key] = header.Value;
                }
            }

            return merged;
        }

        private void ResolveAuthentication(ApiRequest request, ApiSession session, ResolvedApiRequest resolved)
        {
            // Request authentication takes precedence
            if (request.Credentials != null)
            {
                resolved.Credentials = request.Credentials;
            }
            else if (!string.IsNullOrEmpty(request.AuthenticationToken))
            {
                resolved.AuthenticationToken = request.AuthenticationToken;
                resolved.AuthenticationScheme = request.AuthenticationScheme ?? "Bearer";
            }
            else if (request.UseDefaultCredentials)
            {
                resolved.UseDefaultCredentials = true;
            }
            // Fall back to session authentication
            else if (session != null)
            {
                resolved.Credentials = session.Credentials;
                resolved.AuthenticationToken = session.AuthenticationToken;
                resolved.AuthenticationScheme = session.AuthenticationScheme;
                resolved.ApiKeyHeaderName = session.ApiKeyHeaderName;
            }
        }
    }

    /// <summary>
    /// Main API session service that orchestrates session management and requests
    /// </summary>
    public class ApiSessionService : IApiSessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionHttpService _httpService;
        private readonly IRequestResolver _requestResolver;
        private readonly IContentParser _contentParser;
        private readonly IFileService _fileService;

        public ApiSessionService(
            ISessionRepository sessionRepository,
            ISessionHttpService httpService,
            IRequestResolver requestResolver,
            IContentParser contentParser,
            IFileService fileService)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _requestResolver = requestResolver ?? throw new ArgumentNullException(nameof(requestResolver));
            _contentParser = contentParser ?? throw new ArgumentNullException(nameof(contentParser));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        public async Task<ApiResponse> InvokeAsync(ApiRequest request, ApiRequestOptions options = null)
        {
            options ??= new ApiRequestOptions();

            var resolvedRequest = await _requestResolver.ResolveRequestAsync(request);
            var httpResponse = await _httpService.SendAsync(resolvedRequest);

            // Update session last used if using a session
            if (options.UpdateSessionLastUsed && !string.IsNullOrEmpty(resolvedRequest.SessionName))
            {
                await _sessionRepository.UpdateLastUsedAsync(resolvedRequest.SessionName);
            }

            var apiResponse = await CreateApiResponseAsync(httpResponse, resolvedRequest, options);

            if (!string.IsNullOrEmpty(resolvedRequest.OutputFilePath))
            {
                await SaveResponseToFileAsync(httpResponse, resolvedRequest.OutputFilePath);
            }

            if (options.ThrowOnError && !apiResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP Error {apiResponse.StatusCode}: {apiResponse.StatusDescription}");
            }

            return apiResponse;
        }

        public async Task<ApiSession> CreateSessionAsync(string name, Uri baseUri, string authToken = null, string scheme = "Bearer")
        {
            var session = new ApiSession
            {
                Name = name,
                BaseUri = baseUri,
                AuthenticationToken = authToken,
                AuthenticationScheme = scheme
            };

            await _sessionRepository.SaveSessionAsync(session);
            return session;
        }

        public async Task<ApiSession> GetSessionAsync(string name)
        {
            return await _sessionRepository.GetSessionAsync(name);
        }

        public async Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            return await _sessionRepository.GetAllSessionsAsync();
        }

        public async Task DeleteSessionAsync(string name)
        {
            await _sessionRepository.DeleteSessionAsync(name);
        }

        public async Task UpdateSessionAsync(ApiSession session)
        {
            await _sessionRepository.SaveSessionAsync(session);
        }

        private async Task<ApiResponse> CreateApiResponseAsync(HttpResponseMessage httpResponse, ResolvedApiRequest request, ApiRequestOptions options)
        {
            var content = await httpResponse.Content.ReadAsStringAsync();
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;

            var response = new ApiResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                StatusDescription = httpResponse.ReasonPhrase,
                RawContent = content,
                IsSuccessStatusCode = httpResponse.IsSuccessStatusCode,
                ContentType = contentType,
                SessionName = request.SessionName,
                Headers = httpResponse.Headers
                    .Concat(httpResponse.Content.Headers)
                    .ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };

            if (options.ParseContent && _contentParser.CanParse(contentType))
            {
                response.ParsedContent = _contentParser.Parse(content, contentType);
            }
            else
            {
                response.ParsedContent = content;
            }

            return response;
        }

        private async Task SaveResponseToFileAsync(HttpResponseMessage httpResponse, string filePath)
        {
            using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
            await _fileService.SaveStreamToFileAsync(responseStream, filePath);
        }
    }
}

// ============================================================================
// PRESENTATION LAYER - Session-Aware PowerShell Cmdlets
// ============================================================================

namespace LarryWisherMan.ApiUtils.Commands
{
    using System.Management.Automation;
    using System.Threading.Tasks;
    using LarryWisherMan.ApiUtils.Application.Services;
    using LarryWisherMan.ApiUtils.Domain.Interfaces;
    using LarryWisherMan.ApiUtils.Domain.Models;
    using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
    using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
    using LarryWisherMan.ApiUtils.Infrastructure.Services;

    /// <summary>
    /// Base class for session-aware API cmdlets
    /// </summary>
    public abstract class SessionApiCmdletBase : PSCmdlet, IDisposable
    {
        protected IApiSessionService SessionService { get; private set; }
        protected ISessionHttpService HttpService { get; private set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            InitializeServices();
        }

        private void InitializeServices()
        {
            var sessionRepository = new FileSessionRepository();
            HttpService = new SessionHttpService();
            var requestResolver = new RequestResolver(sessionRepository);
            var contentParser = new CompositeContentParser();
            var fileService = new FileService();

            SessionService = new ApiSessionService(sessionRepository, HttpService, requestResolver, contentParser, fileService);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            Dispose();
        }

        public void Dispose()
        {
            HttpService?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// New-ApiSession - Creates and saves an API session
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ApiSession")]
    public class NewApiSessionCommand : SessionApiCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public Uri BaseUri { get; set; }

        [Parameter]
        public string AuthToken { get; set; }

        [Parameter]
        public string AuthScheme { get; set; } = "Bearer";

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var session = SessionService.CreateSessionAsync(Name, BaseUri, AuthToken, AuthScheme)
                    .GetAwaiter()
                    .GetResult();

                if (Headers != null)
                {
                    foreach (DictionaryEntry header in Headers)
                    {
                        session.DefaultHeaders[header.Key.ToString()] = header.Value?.ToString();
                    }
                    SessionService.UpdateSessionAsync(session).GetAwaiter().GetResult();
                }

                if (Credential != null)
                {
                    session.Credentials = Credential.GetNetworkCredential();
                    SessionService.UpdateSessionAsync(session).GetAwaiter().GetResult();
                }

                WriteVerbose($"Created API session: {Name}");

                if (PassThru)
                {
                    WriteObject(session);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "CreateSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }

    /// <summary>
    /// Get-ApiSession - Retrieves API sessions
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ApiSession")]
    public class GetApiSessionCommand : SessionApiCmdletBase
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrEmpty(Name))
                {
                    var sessions = SessionService.GetAllSessionsAsync().GetAwaiter().GetResult();
                    WriteObject(sessions, true);
                }
                else
                {
                    var session = SessionService.GetSessionAsync(Name).GetAwaiter().GetResult();
                    if (session != null)
                    {
                        WriteObject(session);
                    }
                    else
                    {
                        WriteError(new ErrorRecord(
                            new ItemNotFoundException($"Session '{Name}' not found"),
                            "SessionNotFound",
                            ErrorCategory.ObjectNotFound,
                            Name));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "GetSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }

    /// <summary>
    /// Remove-ApiSession - Deletes an API session
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "ApiSession", SupportsShouldProcess = true)]
    public class RemoveApiSessionCommand : SessionApiCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (ShouldProcess(Name, "Remove API Session"))
                {
                    SessionService.DeleteSessionAsync(Name).GetAwaiter().GetResult();
                    WriteVerbose($"Removed API session: {Name}");
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "RemoveSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }

    /// <summary>
    /// Invoke-ApiRequest - Session-aware web request (like Invoke-WebRequest)
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ApiRequest")]
    public class InvokeApiRequestCommand : SessionApiCmdletBase
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
                    ParseContent = false // Don't parse content for WebRequest-style cmdlet
                };

                var response = SessionService.InvokeAsync(request, options)
                    .GetAwaiter()
                    .GetResult();

                ProcessApiResponse(response, options);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ApiRequestError", ErrorCategory.NotSpecified, this));
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
                // Create a response object similar to Invoke-WebRequest
                var responseObject = new PSObject();
                responseObject.Properties.Add(new PSNoteProperty("StatusCode", response.StatusCode));
                responseObject.Properties.Add(new PSNoteProperty("StatusDescription", response.StatusDescription));
                responseObject.Properties.Add(new PSNoteProperty("Content", response.RawContent));
                responseObject.Properties.Add(new PSNoteProperty("Headers", response.Headers));
                responseObject.Properties.Add(new PSNoteProperty("SessionName", response.SessionName));

                WriteObject(responseObject);
            }
        }
    }

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

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    using System;
    using System.Linq;
    using LarryWisherMan.ApiUtils.Domain.Interfaces;

    /// <summary>
    /// JSON content parser
    /// </summary>
    public class JsonContentParser : IContentParser
    {
        public bool CanParse(string contentType)
        {
            return contentType?.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public object Parse(string content, string contentType)
        {
            try
            {
                // Using System.Management.Automation for PowerShell compatibility
                using var ps = System.Management.Automation.PowerShell.Create();
                ps.AddScript($"'{content.Replace("'", "''")}' | ConvertFrom-Json");
                var results = ps.Invoke();
                return results.Count > 0 ? results[0] : content;
            }
            catch
            {
                return content;
            }
        }
    }

    /// <summary>
    /// XML content parser
    /// </summary>
    public class XmlContentParser : IContentParser
    {
        public bool CanParse(string contentType)
        {
            return contentType?.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public object Parse(string content, string contentType)
        {
            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(content);
                return xmlDoc;
            }
            catch
            {
                return content;
            }
        }
    }

    /// <summary>
    /// Composite content parser that manages multiple parsers
    /// </summary>
    public class CompositeContentParser : IContentParser
    {
        private readonly System.Collections.Generic.List<IContentParser> _parsers;

        public CompositeContentParser()
        {
            _parsers = new System.Collections.Generic.List<IContentParser>
            {
                new JsonContentParser(),
                new XmlContentParser()
            };
        }

        public bool CanParse(string contentType)
        {
            return _parsers.Any(p => p.CanParse(contentType));
        }

        public object Parse(string content, string contentType)
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(contentType));
            return parser?.Parse(content, contentType) ?? content;
        }
    }
}
