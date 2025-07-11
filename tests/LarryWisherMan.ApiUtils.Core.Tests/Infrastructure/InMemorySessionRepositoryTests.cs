using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class InMemorySessionRepositoryTests
    {
        private readonly InMemorySessionRepository _repo;

        public InMemorySessionRepositoryTests()
        {
            _repo = new InMemorySessionRepository();
        }

        [Fact]
        public async Task SaveSessionAsync_Should_Add_And_Retrieve_Session()
        {
            var session = new ApiSession
            {
                Name = "Test1",
                BaseUri = new Uri("https://api.test.com")
            };

            await _repo.SaveSessionAsync(session);

            var retrieved = await _repo.GetSessionAsync("Test1");

            Assert.NotNull(retrieved);
            Assert.Equal("Test1", retrieved!.Name);
            Assert.Equal(new Uri("https://api.test.com"), retrieved.BaseUri);
        }

        [Fact]
        public async Task GetSessionAsync_Should_Return_Null_If_Not_Exists()
        {
            var result = await _repo.GetSessionAsync("NotFound");
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllSessionsAsync_Should_Return_All_Sessions()
        {
            await _repo.SaveSessionAsync(new ApiSession { Name = "One", BaseUri = new Uri("https://one.com") });
            await _repo.SaveSessionAsync(new ApiSession { Name = "Two", BaseUri = new Uri("https://two.com") });

            var all = await _repo.GetAllSessionsAsync();

            Assert.Equal(2, all.Count());
        }

        [Fact]
        public async Task SessionExistsAsync_Should_Indicate_Existence()
        {
            await _repo.SaveSessionAsync(new ApiSession { Name = "Exists", BaseUri = new Uri("https://exists.com") });

            Assert.True(await _repo.SessionExistsAsync("Exists"));
            Assert.False(await _repo.SessionExistsAsync("DoesNotExist"));
        }

        [Fact]
        public async Task DeleteSessionAsync_Should_Remove_Session()
        {
            await _repo.SaveSessionAsync(new ApiSession { Name = "DeleteMe", BaseUri = new Uri("https://delete.com") });

            await _repo.DeleteSessionAsync("DeleteMe");

            var result = await _repo.GetSessionAsync("DeleteMe");
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateLastUsedAsync_Should_Modify_Timestamp()
        {
            var session = new ApiSession { Name = "TouchMe", BaseUri = new Uri("https://touch.com") };
            await _repo.SaveSessionAsync(session);

            await Task.Delay(1000); // ensure measurable time difference
            await _repo.UpdateLastUsedAsync("TouchMe");

            var updated = await _repo.GetSessionAsync("TouchMe");
            Assert.True((DateTime.UtcNow - updated!.LastUsed).TotalSeconds < 5);
        }

        [Fact]
        public async Task Clear_Should_Remove_All_Sessions()
        {
            await _repo.SaveSessionAsync(new ApiSession { Name = "A", BaseUri = new Uri("https://a.com") });
            await _repo.SaveSessionAsync(new ApiSession { Name = "B", BaseUri = new Uri("https://b.com") });

            _repo.Clear();

            var all = await _repo.GetAllSessionsAsync();
            Assert.Empty(all);
        }

        [Fact]
        public async Task LoadSessionsAsync_Should_Populate_From_External_Source()
        {
            var input = new List<ApiSession>
            {
                new ApiSession { Name = "X", BaseUri = new Uri("https://x.com") },
                new ApiSession { Name = "Y", BaseUri = new Uri("https://y.com") }
            };

            await _repo.LoadSessionsAsync(input);

            var all = await _repo.GetAllSessionsAsync();
            Assert.Equal(2, all.Count());
        }
    }
}
