using System;
using System.Collections;
using System.Collections.Generic;
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

}
