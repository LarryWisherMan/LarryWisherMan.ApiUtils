using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Infrastructure.Services
{

    /// <summary>
    /// Session-aware HTTP client service
    /// </summary>
    public class SessionHttpService : ISessionHttpService, IDisposable
    {
        private bool _disposed = false;

        public async Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request)
        {
            using (var handler = CreateHandler(request))
            using (var client = new HttpClient(handler))
            using (var httpRequest = CreateHttpRequestMessage(request))
            {
                // Per-request client setup
                client.Timeout = (request.Timeout != TimeSpan.Zero) ? request.Timeout : TimeSpan.FromSeconds(100);
                client.DefaultRequestHeaders.ExpectContinue = false;
                // User-Agent set per message, not here!

                // For .NET Framework: Avoid Expect: 100-Continue globally
                System.Net.ServicePointManager.Expect100Continue = false;

                return await client.SendAsync(httpRequest);
            }
        }

        private HttpClientHandler CreateHandler(ResolvedApiRequest request)
        {
            var handler = new HttpClientHandler
            {
                MaxAutomaticRedirections = (request.MaxRedirections > 0) ? request.MaxRedirections : 50,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseProxy = true,
                Proxy = WebRequest.DefaultWebProxy
            };



            if (request.SkipCertificateValidation)
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            if (request.Credentials != null)
                handler.Credentials = request.Credentials;
            else if (request.UseDefaultCredentials)
                handler.UseDefaultCredentials = true;

            return handler;
        }

        private HttpRequestMessage CreateHttpRequestMessage(ResolvedApiRequest request)
        {
            var httpMethod = new HttpMethod(request.Method.ToUpperInvariant());
            var httpRequest = new HttpRequestMessage(httpMethod, request.Uri);

            // Add per-request headers
            AddHeaders(httpRequest, request);
            AddAuthentication(httpRequest, request);

            // Set User-Agent header here (not on the client)
            if (!string.IsNullOrWhiteSpace(request.UserAgent))
                httpRequest.Headers.UserAgent.ParseAdd(request.UserAgent);

            // Add content if present
            if (request.Body != null)
                httpRequest.Content = CreateHttpContent(request.Body, request.ContentType);

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
            string ct = contentType;
            if (string.IsNullOrEmpty(ct) && (body is string || body is IDictionary || body is not null))
            {
                ct = "application/json"; // Default for JSON
            }
            HttpContent content = body switch
            {
                string stringBody => new StringContent(stringBody, System.Text.Encoding.UTF8),
                byte[] byteBody => new ByteArrayContent(byteBody),
                IDictionary dictionaryBody => CreateFormContent(dictionaryBody),
                _ => new StringContent(body.ToString(), System.Text.Encoding.UTF8)
            };
            if (!string.IsNullOrEmpty(ct))
            {
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ct);
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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Dispose managed resources
            }
            // Free unmanaged resources if any
            _disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }

}
