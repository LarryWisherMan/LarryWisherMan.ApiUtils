using System;
using System.Collections.Generic;
using System.Net;
using LarryWisherMan.ApiUtils.Domain.Models;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Domain.Models
{
    public class ApiRequestTests
    {
        [Fact]
        public void Constructor_SetsDefaults_Correctly()
        {
            // Act
            var request = new ApiRequest();

            // Assert
            Assert.Equal("GET", request.Method);
            Assert.NotNull(request.Headers);
            Assert.Empty(request.Headers);
        }

        [Fact]
        public void Can_Set_All_Properties()
        {
            // Arrange
            var uri = new Uri("https://example.com/api");
            var headers = new Dictionary<string, string> { { "Accept", "application/json" } };
            var creds = new NetworkCredential("user", "pass");

            // Act
            var request = new ApiRequest
            {
                SessionName = "TestSession",
                Uri = uri,
                Method = "POST",
                Headers = headers,
                UserAgent = "TestAgent/1.0",
                ContentType = "application/json",
                Body = new { Name = "John" },
                Credentials = creds,
                AuthenticationToken = "abc123",
                AuthenticationScheme = "Bearer",
                UseDefaultCredentials = true,
                Timeout = TimeSpan.FromSeconds(10),
                MaxRedirections = 5,
                SkipCertificateValidation = true,
                OutputFilePath = @"C:\temp\output.json"
            };

            // Assert
            Assert.Equal("TestSession", request.SessionName);
            Assert.Equal(uri, request.Uri);
            Assert.Equal("POST", request.Method);
            Assert.Equal(headers, request.Headers);
            Assert.Equal("TestAgent/1.0", request.UserAgent);
            Assert.Equal("application/json", request.ContentType);
            Assert.NotNull(request.Body);
            Assert.Equal(creds, request.Credentials);
            Assert.Equal("abc123", request.AuthenticationToken);
            Assert.Equal("Bearer", request.AuthenticationScheme);
            Assert.True(request.UseDefaultCredentials);
            Assert.Equal(TimeSpan.FromSeconds(10), request.Timeout);
            Assert.Equal(5, request.MaxRedirections);
            Assert.True(request.SkipCertificateValidation.Value);
            Assert.Equal(@"C:\temp\output.json", request.OutputFilePath);
        }
    }
}
