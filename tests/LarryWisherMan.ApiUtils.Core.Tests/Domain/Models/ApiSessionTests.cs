using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using LarryWisherMan.ApiUtils.Domain.Models;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Domain.Models
{
    public class ApiSessionTests
    {
        [Fact]
        public void Constructor_ShouldInitializeDefaults()
        {
            var session = new ApiSession();

            session.Id.Should().NotBeEmpty();
            session.Name.Should().BeEmpty();
            session.BaseUri.Should().Be("http://localhost");
            session.DefaultHeaders.Should().NotBeNull().And.BeEmpty();
            session.UserAgent.Should().Be("PowerShell/ApiUtils");
            session.AuthenticationScheme.Should().Be("Bearer");
            session.ApiKeyHeaderName.Should().Be("X-Api-Key");
            session.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(30));
            session.MaxRedirections.Should().Be(5);
            session.SkipCertificateCheck.Should().BeFalse();
            session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            session.LastUsed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            session.CustomProperties.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var session = new ApiSession
            {
                Name = "TestSession",
                BaseUri = new Uri("https://api.example.com"),
                UserAgent = "CustomAgent/1.0",
                Credentials = new NetworkCredential("user", "pass"),
                AuthenticationToken = "abc123",
                AuthenticationScheme = "Token",
                ApiKeyHeaderName = "Api-Key",
                DefaultTimeout = TimeSpan.FromSeconds(60),
                MaxRedirections = 10,
                SkipCertificateCheck = true
            };

            session.Name.Should().Be("TestSession");
            session.BaseUri.Should().Be("https://api.example.com");
            session.UserAgent.Should().Be("CustomAgent/1.0");
            session.Credentials.Should().NotBeNull();
            session.AuthenticationToken.Should().Be("abc123");
            session.AuthenticationScheme.Should().Be("Token");
            session.ApiKeyHeaderName.Should().Be("Api-Key");
            session.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(60));
            session.MaxRedirections.Should().Be(10);
            session.SkipCertificateCheck.Should().BeTrue();
        }

        [Fact]
        public void CustomProperties_ShouldAllowStorage()
        {
            var session = new ApiSession();
            session.CustomProperties["Env"] = "Dev";
            session.CustomProperties["Retries"] = 3;

            session.CustomProperties.Should().ContainKey("Env");
            session.CustomProperties["Env"].Should().Be("Dev");
            session.CustomProperties["Retries"].Should().Be(3);
        }

        [Fact]
        public void Sessions_ShouldHaveUniqueIds()
        {
            var session1 = new ApiSession();
            var session2 = new ApiSession();

            session1.Id.Should().NotBe(session2.Id);
        }
    }
}
