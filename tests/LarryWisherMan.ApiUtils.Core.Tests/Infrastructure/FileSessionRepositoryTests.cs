using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class FileSessionRepositoryTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FileSessionRepository _repo;

        public FileSessionRepositoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _repo = new FileSessionRepository(_tempDir);
        }

        [Fact]
        public async Task SaveSessionAsync_Should_Persist_And_Read_Back_Session()
        {
            var session = new ApiSession { Name = "TestSession", BaseUri = new Uri("https://api.example.com") };
            await _repo.SaveSessionAsync(session);

            var exists = await _repo.SessionExistsAsync("TestSession");
            var loaded = await _repo.GetSessionAsync("TestSession");

            Assert.True(exists);
            Assert.NotNull(loaded);
            Assert.Equal("TestSession", loaded!.Name);
            Assert.Equal(new Uri("https://api.example.com"), loaded!.BaseUri);

        }

        [Fact]
        public async Task GetSessionAsync_Should_Return_Null_When_Not_Exists()
        {
            var session = await _repo.GetSessionAsync("DoesNotExist");
            Assert.Null(session);
        }

        [Fact]
        public async Task GetAllSessionsAsync_Should_Return_All_Valid_Sessions()
        {
            await _repo.SaveSessionAsync(new ApiSession { Name = "One", BaseUri = new Uri("https://a.com") });
            await _repo.SaveSessionAsync(new ApiSession { Name = "Two", BaseUri = new Uri("https://b.com") });

            var all = await _repo.GetAllSessionsAsync();
            Assert.Equal(2, all.Count());
        }

        [Fact]
        public async Task DeleteSessionAsync_Should_Remove_Session_File()
        {
            var session = new ApiSession { Name = "DeleteMe", BaseUri = new Uri("https://delete.com") };
            await _repo.SaveSessionAsync(session);

            await _repo.DeleteSessionAsync("DeleteMe");
            var exists = await _repo.SessionExistsAsync("DeleteMe");

            Assert.False(exists);
        }

        [Fact]
        public async Task UpdateLastUsedAsync_Should_Change_Timestamp()
        {
            var session = new ApiSession { Name = "TouchMe", BaseUri = new Uri("https://touch.com") };
            await _repo.SaveSessionAsync(session);

            await Task.Delay(1000); // Ensure timestamp will change
            await _repo.UpdateLastUsedAsync("TouchMe");

            var updated = await _repo.GetSessionAsync("TouchMe");
            Assert.True((DateTime.UtcNow - updated!.LastUsed).TotalSeconds < 5);
        }

        [Fact]
        public async Task SaveSessionAsync_Should_Throw_If_Session_Is_Null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repo.SaveSessionAsync(null!));
        }

        public void Dispose()
        {
            // Clean up temporary test directory
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
    }
}
