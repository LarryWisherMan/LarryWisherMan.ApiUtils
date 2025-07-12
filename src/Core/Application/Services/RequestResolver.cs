using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Application.Services
{
    /// <summary>
    /// Resolves and hydrates an <see cref="ApiRequest"/> into a fully qualified <see cref="ResolvedApiRequest"/>,
    /// combining request input with session context (base URI, headers, auth, etc.).
    /// </summary>
    public class ApiRequestResolver : IApiRequestResolver
    {
        private readonly ISessionRepository _sessionRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiRequestResolver"/> class.
        /// </summary>
        /// <param name="sessionRepository">Repository used to retrieve session data when specified by request.</param>
        public ApiRequestResolver(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        /// <inheritdoc />
        public async Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Uri == null)
                throw new ArgumentException("Request URI is required.", nameof(request));

            ApiSession? session = null;

            if (!string.IsNullOrWhiteSpace(request.SessionName))
            {
                var sessionName = request.SessionName!;
                session = await _sessionRepository.GetSessionAsync(sessionName);
                if (session == null)
                {
                    throw new ArgumentException($"Session '{sessionName}' not found.");
                }
            }

            return MergeRequestWithSession(request, session);
        }

        /// <summary>
        /// Combines the API request with session context and fallback values to produce a complete request object.
        /// </summary>
        private ResolvedApiRequest MergeRequestWithSession(ApiRequest request, ApiSession? session)
        {
            var resolved = new ResolvedApiRequest
            {
                SessionName = request.SessionName ?? string.Empty,
                Method = request.Method,
                OutputFilePath = request.OutputFilePath,
                Timeout = request.Timeout ?? session?.DefaultTimeout ?? TimeSpan.FromSeconds(30),
                MaxRedirections = request.MaxRedirections ?? session?.MaxRedirections ?? 5,
                SkipCertificateValidation = request.SkipCertificateValidation ?? session?.SkipCertificateCheck ?? false,
                ContentType = request.ContentType ?? "application/json",
                Body = request.Body,
                UserAgent = request.UserAgent ?? session?.UserAgent ?? "PowerShell/ApiUtils"
            };

            resolved.Uri = ResolveUri(request.Uri!, session?.BaseUri);
            resolved.Headers = MergeHeaders(session?.DefaultHeaders, request.Headers);
            ResolveAuthentication(request, session, resolved);

            return resolved;
        }

        /// <summary>
        /// Combines a relative or absolute URI with a session base URI if needed.
        /// </summary>
        private static Uri ResolveUri(Uri requestUri, Uri? sessionBaseUri)
        {
            if (requestUri.IsAbsoluteUri)
                return requestUri;

            if (sessionBaseUri != null)
                return new Uri(sessionBaseUri, requestUri);

            throw new ArgumentException("Relative URI requires a session with a BaseUri");
        }

        /// <summary>
        /// Merges request and session headers with request headers taking precedence.
        /// </summary>
        private static Dictionary<string, string> MergeHeaders(
            IDictionary<string, string>? sessionHeaders,
            IDictionary<string, string>? requestHeaders)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (sessionHeaders != null)
                foreach (var kvp in sessionHeaders)
                    merged[kvp.Key] = kvp.Value;

            if (requestHeaders != null)
                foreach (var kvp in requestHeaders)
                    merged[kvp.Key] = kvp.Value;

            return merged;
        }

        /// <summary>
        /// Resolves authentication settings by preferring request settings and falling back to session values.
        /// </summary>
        private static void ResolveAuthentication(ApiRequest request, ApiSession? session, ResolvedApiRequest resolved)
        {
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
