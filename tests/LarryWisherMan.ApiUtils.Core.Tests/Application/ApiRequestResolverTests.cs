using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Application.Services;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;
using Moq;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Application
{
    public class ApiRequestResolverTests
    {
        private readonly Mock<ISessionRepository> _sessionRepoMock;
        private readonly ApiRequestResolver _resolver;

        public ApiRequestResolverTests()
        {
            _sessionRepoMock = new Mock<ISessionRepository>();
            _resolver = new ApiRequestResolver(_sessionRepoMock.Object);
        }

        [Fact]
        public async Task ResolveRequestAsync_WithAbsoluteUriAndNoSession_ShouldSucceed()
        {
            var request = new ApiRequest
            {
                Uri = new Uri("https://example.com/api/data"),
                Method = HttpMethod.Get.ToString(),
                ContentType = "application/json"
            };

            var result = await _resolver.ResolveRequestAsync(request);

            Assert.Equal("https://example.com/api/data", result.Uri.ToString());
            Assert.Equal(HttpMethod.Get.ToString(), result.Method);
            Assert.Equal("application/json", result.ContentType);
            Assert.Equal("PowerShell/ApiUtils", result.UserAgent);
        }

        [Fact]
        public async Task ResolveRequestAsync_WithSessionName_ShouldHydrateFromSession()
        {
            var request = new ApiRequest
            {
                SessionName = "test",
                Uri = new Uri("relative/path", UriKind.Relative),
                Method = HttpMethod.Post.ToString()
            };

            var session = new ApiSession
            {
                Name = "test",
                BaseUri = new Uri("https://example.com/"),
                AuthenticationToken = "abc123",
                AuthenticationScheme = "Bearer",
                UserAgent = "CustomAgent",
                DefaultHeaders = new Dictionary<string, string> { { "X-Test", "1" } },
                DefaultTimeout = TimeSpan.FromSeconds(60),
                MaxRedirections = 10,
                SkipCertificateCheck = true
            };

            _sessionRepoMock.Setup(x => x.GetSessionAsync("test"))
                .ReturnsAsync(session);

            var result = await _resolver.ResolveRequestAsync(request);

            Assert.Equal(new Uri("https://example.com/relative/path"), result.Uri);
            Assert.Equal("abc123", result.AuthenticationToken);
            Assert.Equal("Bearer", result.AuthenticationScheme);
            Assert.Equal("CustomAgent", result.UserAgent);
            Assert.Equal("1", result.Headers["X-Test"]);
            Assert.True(result.SkipCertificateValidation);
            Assert.Equal(TimeSpan.FromSeconds(60), result.Timeout);
        }

        [Fact]
        public async Task ResolveRequestAsync_WithMissingSession_Throws()
        {
            var request = new ApiRequest
            {
                SessionName = "missing",
                Uri = new Uri("/resource", UriKind.Relative),
                Method = HttpMethod.Get.ToString()
            };

            _sessionRepoMock.Setup(x => x.GetSessionAsync("missing"))
                .ReturnsAsync((ApiSession?)null);

            await Assert.ThrowsAsync<ArgumentException>(() => _resolver.ResolveRequestAsync(request));
        }

        [Fact]
        public async Task ResolveRequestAsync_MergesRequestHeadersOverSessionHeaders()
        {
            var request = new ApiRequest
            {
                SessionName = "override",
                Uri = new Uri("/data", UriKind.Relative),
                Method = HttpMethod.Get.ToString(),
                Headers = new Dictionary<string, string> { { "X-Test", "override" } }
            };

            var session = new ApiSession
            {
                Name = "override",
                BaseUri = new Uri("https://host/"),
                DefaultHeaders = new Dictionary<string, string> { { "X-Test", "original" }, { "X-Session", "yes" } }
            };

            _sessionRepoMock.Setup(x => x.GetSessionAsync("override"))
                .ReturnsAsync(session);

            var result = await _resolver.ResolveRequestAsync(request);

            Assert.Equal("override", result.Headers["X-Test"]);
            Assert.Equal("yes", result.Headers["X-Session"]);
        }

        [Fact]
        public async Task ResolveRequestAsync_UsesDefaults_WhenNotSpecified()
        {
            var request = new ApiRequest
            {
                Uri = new Uri("https://api/defaults"),
                Method = HttpMethod.Get.ToString()
            };

            var result = await _resolver.ResolveRequestAsync(request);

            Assert.Equal("application/json", result.ContentType);
            Assert.Equal("PowerShell/ApiUtils", result.UserAgent);
            Assert.Equal(TimeSpan.FromSeconds(30), result.Timeout);
            Assert.Equal(5, result.MaxRedirections);
        }
    }
}
