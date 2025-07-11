using LarryWisherMan.ApiUtils.Domain.Models;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Domain.Models
{
    public class ApiRequestOptionsTests
    {
        [Fact]
        public void Constructor_Sets_Default_Values()
        {
            // Act
            var options = new ApiRequestOptions();

            // Assert
            Assert.False(options.PassThrough);
            Assert.False(options.ThrowOnError);
            Assert.True(options.ParseContent);
            Assert.True(options.UpdateSessionLastUsed);
        }

        [Fact]
        public void Can_Set_All_Properties()
        {
            // Arrange
            var options = new ApiRequestOptions
            {
                PassThrough = true,
                ThrowOnError = true,
                ParseContent = false,
                UpdateSessionLastUsed = false
            };

            // Assert
            Assert.True(options.PassThrough);
            Assert.True(options.ThrowOnError);
            Assert.False(options.ParseContent);
            Assert.False(options.UpdateSessionLastUsed);
        }
    }
}
