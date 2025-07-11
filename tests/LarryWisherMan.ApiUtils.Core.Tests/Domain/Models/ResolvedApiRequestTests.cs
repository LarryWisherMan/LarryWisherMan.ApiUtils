using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using LarryWisherMan.ApiUtils.Domain.Models;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Domain.Models
{
    public class ResolvedApiRequestTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var request = new ResolvedApiRequest();

            // Assert
            request.Uri.Should().NotBeNull();
            request.Method.Should().Be("GET");
            request.Headers.Should().NotBeNull().And.BeEmpty();
            request.UserAgent.Should().Be("PowerShell/ApiUtils");
            request.ContentType.Should().Be("application/json");
            request.AuthenticationScheme.Should().Be("Bearer");
            request.ApiKeyHeaderName.Should().Be("X-Api-Key");
            request.Timeout.Should().Be(TimeSpan.FromSeconds(30));
            request.MaxRedirections.Should().Be(5);
            request.SessionName.Should().BeEmpty();

            // Optional/defaults
            request.Body.Should().BeNull();
            request.Credentials.Should().BeNull();
            request.AuthenticationToken.Should().BeNull();
            request.OutputFilePath.Should().BeNull();
            request.SkipCertificateValidation.Should().BeFalse();
        }

        [Fact]
        public void Can_Set_And_Get_All_Properties()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            };
            var creds = new NetworkCredential("user", "pass");

            var request = new ResolvedApiRequest
            {
                Uri = new Uri("https://api.example.com/data"),
                Method = "POST",
                Headers = headers,
                UserAgent = "CustomAgent/1.0",
                ContentType = "application/xml",
                Body = "<data></data>",
                Credentials = creds,
                AuthenticationToken = "abc123",
                AuthenticationScheme = "Token",
                ApiKeyHeaderName = "X-My-Key",
                UseDefaultCredentials = true,
                Timeout = TimeSpan.FromSeconds(60),
                MaxRedirections = 10,
                SkipCertificateValidation = true,
                OutputFilePath = @"C:\temp\output.txt",
                SessionName = "TestSession"
            };

            // Assert
            request.Uri.AbsoluteUri.Should().Be("https://api.example.com/data");
            request.Method.Should().Be("POST");
            request.Headers.Should().BeEquivalentTo(headers);
            request.UserAgent.Should().Be("CustomAgent/1.0");
            request.ContentType.Should().Be("application/xml");
            request.Body.Should().Be("<data></data>");
            request.Credentials.Should().Be(creds);
            request.AuthenticationToken.Should().Be("abc123");
            request.AuthenticationScheme.Should().Be("Token");
            request.ApiKeyHeaderName.Should().Be("X-My-Key");
            request.UseDefaultCredentials.Should().BeTrue();
            request.Timeout.Should().Be(TimeSpan.FromSeconds(60));
            request.MaxRedirections.Should().Be(10);
            request.SkipCertificateValidation.Should().BeTrue();
            request.OutputFilePath.Should().Be(@"C:\temp\output.txt");
            request.SessionName.Should().Be("TestSession");
        }
    }
}
