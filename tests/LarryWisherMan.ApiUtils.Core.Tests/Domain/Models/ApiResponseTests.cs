using System;
using System.Collections.Generic;
using LarryWisherMan.ApiUtils.Domain.Models;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Domain.Models
{
    public class ApiResponseTests
    {
        [Fact]
        public void Constructor_ShouldInitializeNonNullProperties()
        {
            // Arrange
            var response = new ApiResponse();

            // Assert
            Assert.NotNull(response.StatusDescription);
            Assert.NotNull(response.RawContent);
            Assert.NotNull(response.ParsedContent);
            Assert.NotNull(response.Headers);
            Assert.NotNull(response.ContentType);
            Assert.NotNull(response.SessionName);
            Assert.NotEqual(default, response.ResponseTime);
        }

        [Fact]
        public void Properties_ShouldSetAndGetValuesCorrectly()
        {
            // Arrange
            var response = new ApiResponse
            {
                StatusCode = 200,
                StatusDescription = "OK",
                RawContent = "{\"message\":\"success\"}",
                ParsedContent = new { message = "success" },
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                IsSuccessStatusCode = true,
                ContentType = "application/json",
                SessionName = "TestSession",
                ResponseTime = new DateTime(2024, 1, 1, 12, 0, 0)
            };

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("OK", response.StatusDescription);
            Assert.Equal("{\"message\":\"success\"}", response.RawContent);
            Assert.Equal("success", ((dynamic)response.ParsedContent).message);
            Assert.Contains("Content-Type", response.Headers.Keys);
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("application/json", response.ContentType);
            Assert.Equal("TestSession", response.SessionName);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), response.ResponseTime);
        }

        [Theory]
        [InlineData(200, true)]
        [InlineData(404, false)]
        [InlineData(500, false)]
        public void IsSuccessStatusCode_CanBeSetManually(int statusCode, bool expected)
        {
            var response = new ApiResponse
            {
                StatusCode = statusCode,
                IsSuccessStatusCode = expected
            };

            Assert.Equal(expected, response.IsSuccessStatusCode);
        }
    }
}
