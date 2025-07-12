
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Application.Services
{
    public class ApiSessionService : IApiSessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionHttpService _httpService;
        private readonly IApiRequestResolver _requestResolver;
        private readonly IContentParser _contentParser;
        private readonly IFileService _fileService;

        public ApiSessionService(
            ISessionRepository sessionRepository,
            ISessionHttpService httpService,
            IApiRequestResolver requestResolver,
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
