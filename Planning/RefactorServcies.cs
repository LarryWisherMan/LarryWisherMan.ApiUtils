using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Application.Services
{
    /// <summary>
    /// Default implementation of <see cref="IApiSessionService"/>.
    /// Manages API session persistence and handles HTTP request invocation using session context.
    /// </summary>
    public class ApiSessionService : IApiSessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionHttpService _httpService;
        private readonly IRequestResolver _requestResolver;
        private readonly IContentParser _contentParser;
        private readonly IFileService _fileService;
        private readonly ILogger<ApiSessionService>? _logger;

        /// <summary>
        /// Constructs a new <see cref="ApiSessionService"/>.
        /// </summary>
        public ApiSessionService(
            ISessionRepository sessionRepository,
            ISessionHttpService httpService,
            IRequestResolver requestResolver,
            IContentParser contentParser,
            IFileService fileService,
            ILogger<ApiSessionService>? logger = null)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _requestResolver = requestResolver ?? throw new ArgumentNullException(nameof(requestResolver));
            _contentParser = contentParser ?? throw new ArgumentNullException(nameof(contentParser));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ApiResponse> InvokeAsync(ApiRequest request, ApiRequestOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            options ??= new ApiRequestOptions();

            _logger?.LogDebug("Resolving API request for URI: {RelativePath}", request.RelativePath);

            var resolved = await _requestResolver.ResolveRequestAsync(request, cancellationToken).ConfigureAwait(false);
            var response = await _httpService.SendAsync(resolved, cancellationToken).ConfigureAwait(false);

            if (options.UpdateSessionLastUsed && !string.IsNullOrWhiteSpace(resolved.SessionName))
            {
                await _sessionRepository.UpdateLastUsedAsync(resolved.SessionName).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(resolved.OutputFilePath))
            {
                await SaveResponseToFileAsync(response, resolved.OutputFilePath, cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("Response written to file: {Path}", resolved.OutputFilePath);
            }

            var apiResponse = await BuildApiResponseAsync(response, resolved, options, cancellationToken).ConfigureAwait(false);

            if (options.ThrowOnError && !apiResponse.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Throwing HTTP exception: {StatusCode} - {Description}", apiResponse.StatusCode, apiResponse.StatusDescription);
                throw new HttpRequestException($"HTTP {(int)apiResponse.StatusCode} - {apiResponse.StatusDescription}");
            }

            return apiResponse;
        }

        /// <inheritdoc />
        public async Task<ApiSession> CreateSessionAsync(string name, Uri baseUri, string? authToken = null, string scheme = "Bearer", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Session name cannot be empty", nameof(name));
            if (baseUri is null) throw new ArgumentNullException(nameof(baseUri));

            var session = new ApiSession
            {
                Name = name,
                BaseUri = baseUri,
                AuthenticationToken = authToken,
                AuthenticationScheme = scheme
            };

            await _sessionRepository.SaveSessionAsync(session).ConfigureAwait(false);
            return session;
        }

        /// <inheritdoc />
        public Task<ApiSession> GetSessionAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Session name cannot be null or empty", nameof(name));
            return _sessionRepository.GetSessionAsync(name);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ApiSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
            => _sessionRepository.GetAllSessionsAsync();

        /// <inheritdoc />
        public async Task<bool> DeleteSessionAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            await _sessionRepository.DeleteSessionAsync(name).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public Task<bool> UpdateSessionAsync(ApiSession session, CancellationToken cancellationToken = default)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            return _sessionRepository.SaveSessionAsync(session).ContinueWith(_ => true, cancellationToken);
        }

        private async Task<ApiResponse> BuildApiResponseAsync(HttpResponseMessage httpResponse, ResolvedApiRequest request, ApiRequestOptions options, CancellationToken cancellationToken)
        {
            var content = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;

            var response = new ApiResponse
            {
                StatusCode = httpResponse.StatusCode,
                StatusDescription = httpResponse.ReasonPhrase,
                RawContent = content,
                IsSuccessStatusCode = httpResponse.IsSuccessStatusCode,
                ContentType = contentType,
                SessionName = request.SessionName,
                Headers = httpResponse.Headers
                    .Concat(httpResponse.Content.Headers)
                    .ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };

            response.ParsedContent = (options.ParseContent && _contentParser.CanParse(contentType))
                ? _contentParser.Parse(content, contentType)
                : content;

            return response;
        }

        private async Task SaveResponseToFileAsync(HttpResponseMessage httpResponse, string filePath, CancellationToken cancellationToken)
        {
            using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await _fileService.SaveStreamToFileAsync(stream, filePath, cancellationToken).ConfigureAwait(false);
        }
    }
}


public class ApiRequestExecutor : IApiRequestExecutor
{
    private readonly IApiRequestResolver _resolver;
    private readonly IApiTransportService _transport;
    private readonly IApiResponseBuilder _responseBuilder;
    private readonly IApiResponseSaver _saver;
    private readonly IApiSessionTouchService _sessionToucher;

    public ApiRequestExecutor(
        IApiRequestResolver resolver,
        IApiTransportService transport,
        IApiResponseBuilder responseBuilder,
        IApiResponseSaver saver,
        IApiSessionTouchService sessionToucher)
    {
        _resolver = resolver;
        _transport = transport;
        _responseBuilder = responseBuilder;
        _saver = saver;
        _sessionToucher = sessionToucher;
    }

    public async Task<ApiResponse> ExecuteAsync(ApiRequest request, ApiRequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new ApiRequestOptions();

        var resolved = await _resolver.ResolveRequestAsync(request, cancellationToken);
        var response = await _transport.SendAsync(resolved, cancellationToken);

        if (options.UpdateSessionLastUsed && !string.IsNullOrEmpty(resolved.SessionName))
            await _sessionToucher.TouchAsync(resolved.SessionName);

        if (!string.IsNullOrEmpty(resolved.OutputFilePath))
            await _saver.SaveAsync(response.Content, resolved.OutputFilePath, cancellationToken);

        var apiResponse = await _responseBuilder.BuildAsync(response, resolved, options, cancellationToken);

        if (options.ThrowOnError && !apiResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP Error {apiResponse.StatusCode}: {apiResponse.StatusDescription}");

        return apiResponse;
    }
}


// Interfaces

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    public interface IApiRequestExecutor
    {
        Task<ApiResponse> ExecuteAsync(ApiRequest request, ApiRequestOptions? options = null, CancellationToken cancellationToken = default);
    }

    public interface IApiRequestResolver
    {
        Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request, CancellationToken cancellationToken = default);
    }

    public interface IApiTransportService
    {
        Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request, CancellationToken cancellationToken = default);
    }

    public interface IApiSessionTouchService
    {
        Task TouchAsync(string sessionName);
    }

    public interface IApiResponseSaver
    {
        Task SaveAsync(HttpContent content, string filePath, CancellationToken cancellationToken = default);
    }

    public interface IApiResponseBuilder
    {
        Task<ApiResponse> BuildAsync(HttpResponseMessage response, ResolvedApiRequest request, ApiRequestOptions options, CancellationToken cancellationToken = default);
    }
}

// Sample implementations

namespace LarryWisherMan.ApiUtils.Application.Services
{
    public class ApiRequestResolver : IApiRequestResolver
    {
        private readonly IRequestResolver _resolver;

        public ApiRequestResolver(IRequestResolver resolver)
        {
            _resolver = resolver;
        }

        public Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request, CancellationToken cancellationToken = default)
            => _resolver.ResolveRequestAsync(request, cancellationToken);
    }

    public class ApiTransportService : IApiTransportService
    {
        private readonly ISessionHttpService _httpService;

        public ApiTransportService(ISessionHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request, CancellationToken cancellationToken = default)
            => _httpService.SendAsync(request, cancellationToken);
    }

    public class ApiSessionTouchService : IApiSessionTouchService
    {
        private readonly ISessionRepository _repo;

        public ApiSessionTouchService(ISessionRepository repo)
        {
            _repo = repo;
        }

        public Task TouchAsync(string sessionName)
            => _repo.UpdateLastUsedAsync(sessionName);
    }

    public class ApiResponseSaver : IApiResponseSaver
    {
        private readonly IFileService _fileService;

        public ApiResponseSaver(IFileService fileService)
        {
            _fileService = fileService;
        }

        public async Task SaveAsync(HttpContent content, string filePath, CancellationToken cancellationToken = default)
        {
            using var stream = await content.ReadAsStreamAsync(cancellationToken);
            await _fileService.SaveStreamToFileAsync(stream, filePath);
        }
    }

    public class ApiResponseBuilder : IApiResponseBuilder
    {
        private readonly IContentParser _parser;

        public ApiResponseBuilder(IContentParser parser)
        {
            _parser = parser;
        }

        public async Task<ApiResponse> BuildAsync(HttpResponseMessage response, ResolvedApiRequest request, ApiRequestOptions options, CancellationToken cancellationToken = default)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType;

            var parsed = (options.ParseContent && _parser.CanParse(contentType))
                ? _parser.Parse(content, contentType)
                : content;

            return new ApiResponse
            {
                StatusCode = (int)response.StatusCode,
                StatusDescription = response.ReasonPhrase,
                RawContent = content,
                ParsedContent = parsed,
                ContentType = contentType,
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                SessionName = request.SessionName,
                Headers = response.Headers.Concat(response.Content.Headers)
                    .ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };
        }
    }
}
