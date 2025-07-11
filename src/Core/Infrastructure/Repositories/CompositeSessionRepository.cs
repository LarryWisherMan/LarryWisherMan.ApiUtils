using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// Composite session repository that combines an in-memory cache with persistent storage.
    /// </summary>
    public class CompositeSessionRepository : ISessionRepository, IDisposable
    {
        private readonly InMemorySessionRepository _memoryRepository;
        private readonly ISessionRepository _persistentRepository;
        private readonly bool _defaultSaveToFile;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeSessionRepository"/> class.
        /// </summary>
        /// <param name="persistentRepository">The persistent backing repository (e.g., file-based).</param>
        /// <param name="defaultSaveToFile">Whether to persist sessions to disk by default.</param>
        public CompositeSessionRepository(ISessionRepository persistentRepository, bool defaultSaveToFile = true)
        {
            _persistentRepository = persistentRepository ?? throw new ArgumentNullException(nameof(persistentRepository));
            _defaultSaveToFile = defaultSaveToFile;
            _memoryRepository = new InMemorySessionRepository();
        }

        /// <inheritdoc />
        public async Task<ApiSession?> GetSessionAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Session name cannot be null or empty.", nameof(name));

            // Try in-memory first
            var session = await _memoryRepository.GetSessionAsync(name).ConfigureAwait(false);
            if (session != null)
            {
                await _memoryRepository.UpdateLastUsedAsync(name).ConfigureAwait(false);
                return session;
            }

            // Fallback to persistent storage
            session = await _persistentRepository.GetSessionAsync(name).ConfigureAwait(false);
            if (session != null)
            {
                await _memoryRepository.SaveSessionAsync(session).ConfigureAwait(false);
                await _memoryRepository.UpdateLastUsedAsync(name).ConfigureAwait(false);
            }

            return session;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            var memorySessions = await _memoryRepository.GetAllSessionsAsync().ConfigureAwait(false);
            var persistentSessions = await _persistentRepository.GetAllSessionsAsync().ConfigureAwait(false);

            var memoryNames = new HashSet<string>(memorySessions.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
            var combined = memorySessions.ToList();

            foreach (var session in persistentSessions)
            {
                if (!memoryNames.Contains(session.Name))
                {
                    combined.Add(session);
                    await _memoryRepository.SaveSessionAsync(session).ConfigureAwait(false);
                }
            }

            return combined;
        }

        /// <inheritdoc />
        public Task SaveSessionAsync(ApiSession session) =>
            SaveSessionAsync(session, _defaultSaveToFile);

        /// <summary>
        /// Saves a session to memory and optionally to persistent storage.
        /// </summary>
        public async Task SaveSessionAsync(ApiSession session, bool saveToFile)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            await _memoryRepository.SaveSessionAsync(session).ConfigureAwait(false);

            if (saveToFile)
                await _persistentRepository.SaveSessionAsync(session).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteSessionAsync(string name)
        {
            await _memoryRepository.DeleteSessionAsync(name).ConfigureAwait(false);
            await _persistentRepository.DeleteSessionAsync(name).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> SessionExistsAsync(string name)
        {
            if (await _memoryRepository.SessionExistsAsync(name).ConfigureAwait(false))
                return true;

            return await _persistentRepository.SessionExistsAsync(name).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateLastUsedAsync(string name)
        {
            await _memoryRepository.UpdateLastUsedAsync(name).ConfigureAwait(false);

            if (await _persistentRepository.SessionExistsAsync(name).ConfigureAwait(false))
                await _persistentRepository.UpdateLastUsedAsync(name).ConfigureAwait(false);
        }

        /// <summary>
        /// Persists all sessions currently held in memory to the persistent repository.
        /// </summary>
        public async Task SyncToPersistentAsync()
        {
            var sessions = await _memoryRepository.GetAllSessionsAsync().ConfigureAwait(false);
            foreach (var session in sessions)
            {
                await _persistentRepository.SaveSessionAsync(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Loads all sessions from persistent store into memory cache.
        /// </summary>
        public async Task PreloadCacheAsync()
        {
            var sessions = await _persistentRepository.GetAllSessionsAsync().ConfigureAwait(false);
            await _memoryRepository.LoadSessionsAsync(sessions).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears all sessions from the memory cache.
        /// </summary>
        public void ClearCache() => _memoryRepository.Clear();

        /// <summary>
        /// Gets whether new sessions are saved to persistent storage by default.
        /// </summary>
        public bool DefaultSaveToFile => _defaultSaveToFile;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal disposal logic.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_defaultSaveToFile)
                {
                    try
                    {
                        SyncToPersistentAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // Fire-and-forget fallback; suppress exceptions
                    }
                }

                if (_persistentRepository is IDisposable disposable)
                    disposable.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~CompositeSessionRepository() => Dispose(false);
    }
}
