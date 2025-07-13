using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// Provides an in-memory implementation of <see cref="ISessionRepository"/>.
    /// Used for testing or temporary, non-persistent session storage.
    /// </summary>
    public class InMemorySessionRepository : ISessionRepository
    {
        // Dictionary to store sessions keyed by name, using case-insensitive comparison.
        private readonly Dictionary<string, ApiSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

        // Synchronization object to ensure thread safety.
        private readonly object _lock = new();

        /// <summary>
        /// Retrieves a session by name.
        /// </summary>
        /// <param name="name">The name of the session.</param>
        /// <returns>The session if found; otherwise, null.</returns>
        public Task<ApiSession?> GetSessionAsync(string name)
        {
            lock (_lock)
            {
                _sessions.TryGetValue(name, out var session);
                return Task.FromResult<ApiSession?>(session);
            }
        }

        /// <summary>
        /// Retrieves all sessions currently stored in memory.
        /// </summary>
        /// <returns>A collection of <see cref="ApiSession"/> instances.</returns>
        public Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            lock (_lock)
            {
                return Task.FromResult<IEnumerable<ApiSession>>(_sessions.Values.ToList());
            }
        }

        /// <summary>
        /// Saves or updates the given session in memory.
        /// </summary>
        /// <param name="session">The session to save.</param>
        public Task SaveSessionAsync(ApiSession session)
        {
            lock (_lock)
            {
                _sessions[session.Name] = session;
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Deletes a session by name.
        /// </summary>
        /// <param name="name">The name of the session to delete.</param>
        public Task DeleteSessionAsync(string name)
        {
            lock (_lock)
            {
                _sessions.Remove(name);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Checks whether a session exists by name.
        /// </summary>
        /// <param name="name">The session name to check.</param>
        /// <returns>True if the session exists; otherwise, false.</returns>
        public Task<bool> SessionExistsAsync(string name)
        {
            lock (_lock)
            {
                return Task.FromResult(_sessions.ContainsKey(name));
            }
        }

        /// <summary>
        /// Updates the LastUsed timestamp of the specified session.
        /// </summary>
        /// <param name="name">The name of the session to update.</param>
        public async Task UpdateLastUsedAsync(string name)
        {
            var session = await GetSessionAsync(name);
            if (session != null)
            {
                session.LastUsed = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Clears all sessions from the in-memory store.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _sessions.Clear();
            }
        }

        /// <summary>
        /// Loads a collection of sessions into the in-memory store.
        /// </summary>
        /// <param name="sessions">Sessions to load.</param>
        public async Task LoadSessionsAsync(IEnumerable<ApiSession> sessions)
        {
            foreach (var session in sessions)
            {
                await SaveSessionAsync(session);
            }
        }
    }
}
