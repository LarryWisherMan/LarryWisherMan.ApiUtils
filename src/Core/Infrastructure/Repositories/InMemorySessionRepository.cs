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
        private readonly Dictionary<string, ApiSession> _sessions = new Dictionary<string, ApiSession>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();

        public Task<ApiSession> GetSessionAsync(string name)
        {
            lock (_lock)
            {
                _sessions.TryGetValue(name, out var session);
                return Task.FromResult(session);
            }
        }

        public Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            lock (_lock)
            {
                return Task.FromResult<IEnumerable<ApiSession>>(_sessions.Values.ToList());
            }
        }

        public Task SaveSessionAsync(ApiSession session)
        {
            lock (_lock)
            {
                _sessions[session.Name] = session;
                return Task.CompletedTask;
            }
        }

        public Task DeleteSessionAsync(string name)
        {
            lock (_lock)
            {
                _sessions.Remove(name);
                return Task.CompletedTask;
            }
        }

        public Task<bool> SessionExistsAsync(string name)
        {
            lock (_lock)
            {
                return Task.FromResult(_sessions.ContainsKey(name));
            }
        }

        public async Task UpdateLastUsedAsync(string name)
        {
            var session = await GetSessionAsync(name);
            if (session != null)
            {
                session.LastUsed = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Clear all sessions from memory
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _sessions.Clear();
            }
        }

        /// <summary>
        /// Load sessions from another repository into memory
        /// </summary>
        public async Task LoadSessionsAsync(IEnumerable<ApiSession> sessions)
        {
            foreach (var session in sessions)
            {
                await SaveSessionAsync(session);
            }
        }
    }
}
