using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class CompositeSessionRepositoryTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly CompositeSessionRepository _composite;

        public CompositeSessionRepositoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            var fileRepo = new FileSessionRepository(_tempDir);
            _composite = new CompositeSessionRepository(fileRepo);
        }

        [Fact]
        public async Task SaveSessionAsync_Should_Persist_In_Memory_And_File()
        {
            var session = new ApiSession { Name = "TestSession", BaseUri = new Uri("https://api.example.com") };
            await _composite.SaveSessionAsync(session);

            var loaded = await _composite.GetSessionAsync("TestSession");
            Assert.NotNull(loaded);
            Assert.Equal("TestSession", loaded!.Name);
            Assert.Equal(new Uri("https://api.example.com"), loaded.BaseUri);
        }

        [Fact]
        public async Task GetAllSessionsAsync_Should_Merge_And_Cache()
        {
            await _composite.SaveSessionAsync(new ApiSession { Name = "A", BaseUri = new Uri("https://a.com") });
            await _composite.SaveSessionAsync(new ApiSession { Name = "B", BaseUri = new Uri("https://b.com") });

            var all = await _composite.GetAllSessionsAsync();
            Assert.Equal(2, all.Count());
        }

        [Fact]
        public async Task SessionExistsAsync_Should_Check_Memory_Then_File()
        {
            var session = new ApiSession { Name = "Existy", BaseUri = new Uri("https://exists.com") };
            await _composite.SaveSessionAsync(session);

            var exists = await _composite.SessionExistsAsync("Existy");
            Assert.True(exists);
        }

        [Fact]
        public async Task DeleteSessionAsync_Should_Remove_From_Both()
        {
            var session = new ApiSession { Name = "DeleteMe", BaseUri = new Uri("https://delete.com") };
            await _composite.SaveSessionAsync(session);

            await _composite.DeleteSessionAsync("DeleteMe");
            var exists = await _composite.SessionExistsAsync("DeleteMe");

            Assert.False(exists);
        }

        [Fact]
        public async Task UpdateLastUsedAsync_Should_Update_Timestamp()
        {
            var session = new ApiSession { Name = "Touchy", BaseUri = new Uri("https://touch.com") };
            await _composite.SaveSessionAsync(session);

            await Task.Delay(1000);
            await _composite.UpdateLastUsedAsync("Touchy");

            var updated = await _composite.GetSessionAsync("Touchy");
            Assert.True((DateTime.UtcNow - updated!.LastUsed).TotalSeconds < 5);
        }

        [Fact]
        public async Task SyncToPersistentAsync_Should_Write_All_To_File()
        {
            var inMemory = new ApiSession { Name = "MemoryOnly", BaseUri = new Uri("https://temp.com") };
            await _composite.SaveSessionAsync(inMemory, saveToFile: false);

            var fileRepo = new FileSessionRepository(_tempDir);
            Assert.Null(await fileRepo.GetSessionAsync("MemoryOnly"));

            await _composite.SyncToPersistentAsync();

            var synced = await fileRepo.GetSessionAsync("MemoryOnly");
            Assert.NotNull(synced);
        }

        [Fact]
        public async Task PreloadCacheAsync_Should_Load_From_File()
        {
            var directFileRepo = new FileSessionRepository(_tempDir);
            await directFileRepo.SaveSessionAsync(new ApiSession { Name = "FileOnly", BaseUri = new Uri("https://file.com") });

            var preloadComposite = new CompositeSessionRepository(directFileRepo);
            await preloadComposite.PreloadCacheAsync();

            var session = await preloadComposite.GetSessionAsync("FileOnly");
            Assert.NotNull(session);
        }

        public void Dispose()
        {
            _composite.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
    }
}
