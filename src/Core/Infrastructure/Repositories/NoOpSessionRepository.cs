using System.Collections.Generic;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// A non-persistent implementation of <see cref="ISessionRepository"/>.
    /// This repository performs no storage or retrieval operations and always returns empty or default values.
    /// Useful in scenarios where persistence is optional or disabled (e.g., memory-only mode).
    /// </summary>
    public class NoOpSessionRepository : ISessionRepository
    {
        /// <inheritdoc />
        public Task<ApiSession?> GetSessionAsync(string name) =>
            Task.FromResult<ApiSession?>(null);

        /// <inheritdoc />
        public Task<IEnumerable<ApiSession>> GetAllSessionsAsync() =>
            Task.FromResult<IEnumerable<ApiSession>>(new List<ApiSession>());

        /// <inheritdoc />
        public Task SaveSessionAsync(ApiSession session) =>
            Task.CompletedTask;

        /// <inheritdoc />
        public Task DeleteSessionAsync(string name) =>
            Task.CompletedTask;

        /// <inheritdoc />
        public Task<bool> SessionExistsAsync(string name) =>
            Task.FromResult(false);

        /// <inheritdoc />
        public Task UpdateLastUsedAsync(string name) =>
            Task.CompletedTask;
    }
}
