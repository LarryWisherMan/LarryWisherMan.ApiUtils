using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// In-memory session repository for testing or temporary sessions
    /// </summary>
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly Dictionary<string, ApiSession> _sessions = new Dictionary<string, ApiSession>();

        public Task<ApiSession> GetSessionAsync(string name)
        {
            _sessions.TryGetValue(name, out var session);
            return Task.FromResult(session);
        }

        public Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            return Task.FromResult<IEnumerable<ApiSession>>(_sessions.Values.ToList());
        }

        public Task SaveSessionAsync(ApiSession session)
        {
            _sessions[session.Name] = session;
            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(string name)
        {
            _sessions.Remove(name);
            return Task.CompletedTask;
        }

        public Task<bool> SessionExistsAsync(string name)
        {
            return Task.FromResult(_sessions.ContainsKey(name));
        }

        public async Task UpdateLastUsedAsync(string name)
        {
            var session = await GetSessionAsync(name);
            if (session != null)
            {
                session.LastUsed = DateTime.UtcNow;
            }
        }
    }
}
