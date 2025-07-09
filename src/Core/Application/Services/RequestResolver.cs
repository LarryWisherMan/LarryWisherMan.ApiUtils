using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Application.Services
{

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
            resolved.SkipCertificateValidation = request.SkipCertificateValidation ?? session?.SkipCertificateCheck ?? false;

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

        private Dictionary<string,string> MergeHeaders(
            IDictionary<string,string> sessionHeaders,
            IDictionary<string,string> requestHeaders)
        {
            var merged = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

            if (sessionHeaders != null)
                foreach (var kvp in sessionHeaders) merged[kvp.Key] = kvp.Value;

            if (requestHeaders != null)
                foreach (var kvp in requestHeaders) merged[kvp.Key] = kvp.Value;

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
}
