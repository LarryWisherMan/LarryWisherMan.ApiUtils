using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class SessionRepositoryFactoryTests : IDisposable
    {
        private readonly string _tempPath;

        public SessionRepositoryFactoryTests()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempPath);
        }

        [Fact]
        public void Create_Should_Return_Composite_With_FileRepository_When_SaveToFile_True()
        {
            var factory = new SessionRepositoryFactory();
            var repo = factory.Create(saveToFile: true, preloadFromFile: false, storagePath: _tempPath);

            Assert.IsType<CompositeSessionRepository>(repo);
            var composite = (CompositeSessionRepository)repo;

            Assert.True(composite.DefaultSaveToFile);
        }

        [Fact]
        public void Create_Should_Return_Composite_With_NoOpRepository_When_SaveToFile_False()
        {
            var factory = new SessionRepositoryFactory();
            var repo = factory.Create(saveToFile: false);

            Assert.IsType<CompositeSessionRepository>(repo);
            var composite = (CompositeSessionRepository)repo;

            Assert.False(composite.DefaultSaveToFile);

            // Try saving something and ensure no error
            var session = new ApiSession { Name = "Transient", BaseUri = new Uri("https://noop.com") };
            var result = composite.SaveSessionAsync(session); // should not throw
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Create_Should_Respect_Custom_StoragePath()
        {
            var factory = new SessionRepositoryFactory();
            var repo = factory.Create(saveToFile: true, preloadFromFile: false, storagePath: _tempPath);

            var composite = (CompositeSessionRepository)repo;
            var session = new ApiSession { Name = "PathTest", BaseUri = new Uri("https://test.com") };

            await composite.SaveSessionAsync(session);
            var fileRepo = new FileSessionRepository(_tempPath);

            var fromDisk = await fileRepo.GetSessionAsync("PathTest");
            Assert.NotNull(fromDisk);
        }

        [Fact]
        public async Task Create_Should_Preload_When_Flag_True()
        {
            // Manually persist a session first
            var fileRepo = new FileSessionRepository(_tempPath);
            var session = new ApiSession { Name = "PreloadMe", BaseUri = new Uri("https://preload.com") };
            await fileRepo.SaveSessionAsync(session);

            var factory = new SessionRepositoryFactory();
            var repo = factory.Create(saveToFile: true, preloadFromFile: true, storagePath: _tempPath);

            // Give it a moment for preload to occur
            await Task.Delay(500);

            var composite = (CompositeSessionRepository)repo;
            var result = await composite.GetSessionAsync("PreloadMe");

            Assert.NotNull(result);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempPath))
                Directory.Delete(_tempPath, recursive: true);
        }
    }
}
