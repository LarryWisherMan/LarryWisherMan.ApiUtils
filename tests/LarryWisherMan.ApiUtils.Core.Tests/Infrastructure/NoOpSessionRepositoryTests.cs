using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class NoOpSessionRepositoryTests
    {
        private readonly NoOpSessionRepository _repo;

        public NoOpSessionRepositoryTests()
        {
            _repo = new NoOpSessionRepository();
        }

        [Fact]
        public async Task GetSessionAsync_Should_Always_Return_Null()
        {
            var result = await _repo.GetSessionAsync("AnyName");
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllSessionsAsync_Should_Return_Empty_List()
        {
            var result = await _repo.GetAllSessionsAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task SaveSessionAsync_Should_Succeed_Without_Exception()
        {
            var session = new ApiSession { Name = "Dummy", BaseUri = new Uri("https://none.com") };
            var ex = await Record.ExceptionAsync(() => _repo.SaveSessionAsync(session));

            Assert.Null(ex);
        }

        [Fact]
        public async Task DeleteSessionAsync_Should_Succeed_Without_Exception()
        {
            var ex = await Record.ExceptionAsync(() => _repo.DeleteSessionAsync("NonExistent"));
            Assert.Null(ex);
        }

        [Fact]
        public async Task SessionExistsAsync_Should_Always_Return_False()
        {
            var result = await _repo.SessionExistsAsync("Whatever");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateLastUsedAsync_Should_Succeed_Without_Exception()
        {
            var ex = await Record.ExceptionAsync(() => _repo.UpdateLastUsedAsync("Anything"));
            Assert.Null(ex);
        }
    }
}
