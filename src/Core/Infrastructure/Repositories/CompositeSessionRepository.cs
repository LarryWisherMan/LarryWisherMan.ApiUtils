using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;



namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{

    /// <summary>
    /// Enhanced composite repository with configurable save behavior
    /// </summary>
    public class CompositeSessionRepository : ISessionRepository, IDisposable
    {
        private readonly InMemorySessionRepository _memoryRepository;
        private readonly ISessionRepository _persistentRepository;
        private readonly bool _defaultSaveToFile;
        private bool _disposed = false;

        public CompositeSessionRepository(ISessionRepository persistentRepository, bool defaultSaveToFile = true)
        {
            _memoryRepository = new InMemorySessionRepository();
            _persistentRepository = persistentRepository ?? throw new ArgumentNullException(nameof(persistentRepository));
            _defaultSaveToFile = defaultSaveToFile;
        }

        public async Task<ApiSession> GetSessionAsync(string name)
        {
            // 1. Try memory first (fast)
            var session = await _memoryRepository.GetSessionAsync(name);
            if (session != null)
            {
                await _memoryRepository.UpdateLastUsedAsync(name);
                return session;
            }

            // 2. Fall back to persistent storage (always check file as fallback)
            session = await _persistentRepository.GetSessionAsync(name);
            if (session != null)
            {
                // Cache in memory for next time
                await _memoryRepository.SaveSessionAsync(session);
                await _memoryRepository.UpdateLastUsedAsync(name);
            }

            return session;
        }

        public async Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            // Get from both sources and merge (memory takes precedence for duplicates)
            var memorySessions = await _memoryRepository.GetAllSessionsAsync();
            var persistentSessions = await _persistentRepository.GetAllSessionsAsync();

            var memorySessionNames = new HashSet<string>(
                memorySessions.Select(s => s.Name),
                StringComparer.OrdinalIgnoreCase);

            var combinedSessions = memorySessions.ToList();

            // Add persistent sessions that aren't already in memory
            foreach (var persistentSession in persistentSessions)
            {
                if (!memorySessionNames.Contains(persistentSession.Name))
                {
                    combinedSessions.Add(persistentSession);
                    // Cache in memory
                    await _memoryRepository.SaveSessionAsync(persistentSession);
                }
            }

            return combinedSessions;
        }

        public async Task SaveSessionAsync(ApiSession session)
        {
            await SaveSessionAsync(session, _defaultSaveToFile);
        }

        public async Task SaveSessionAsync(ApiSession session, bool saveToFile)
        {
            // Always save to memory
            await _memoryRepository.SaveSessionAsync(session);

            // Conditionally save to file
            if (saveToFile)
            {
                await _persistentRepository.SaveSessionAsync(session);
            }
        }

        public async Task DeleteSessionAsync(string name)
        {
            // Delete from both repositories
            await _memoryRepository.DeleteSessionAsync(name);
            await _persistentRepository.DeleteSessionAsync(name);
        }

        public async Task<bool> SessionExistsAsync(string name)
        {
            // Check memory first, then persistent (always check file as fallback)
            var existsInMemory = await _memoryRepository.SessionExistsAsync(name);
            if (existsInMemory)
            {
                return true;
            }

            return await _persistentRepository.SessionExistsAsync(name);
        }

        public async Task UpdateLastUsedAsync(string name)
        {
            await _memoryRepository.UpdateLastUsedAsync(name);

            // Update file if it exists there (preserve existing file sessions)
            var existsInFile = await _persistentRepository.SessionExistsAsync(name);
            if (existsInFile)
            {
                await _persistentRepository.UpdateLastUsedAsync(name);
            }
        }

        /// <summary>
        /// Manually sync memory to persistent storage
        /// </summary>
        public async Task SyncToPersistentAsync()
        {
            var memorySessions = await _memoryRepository.GetAllSessionsAsync();
            foreach (var session in memorySessions)
            {
                await _persistentRepository.SaveSessionAsync(session);
            }
        }

        /// <summary>
        /// Load all persistent sessions into memory cache
        /// </summary>
        public async Task PreloadCacheAsync()
        {
            var persistentSessions = await _persistentRepository.GetAllSessionsAsync();
            await _memoryRepository.LoadSessionsAsync(persistentSessions);
        }

        /// <summary>
        /// Clear memory cache
        /// </summary>
        public void ClearCache()
        {
            _memoryRepository.Clear();
        }

        /// <summary>
        /// Get current default save behavior
        /// </summary>
        public bool DefaultSaveToFile => _defaultSaveToFile;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_defaultSaveToFile)
                {
                    // Try to sync before disposing (fire and forget)
                    try
                    {
                        SyncToPersistentAsync().Wait(TimeSpan.FromSeconds(5));
                    }
                    catch
                    {
                        // Best effort - don't throw during disposal
                    }
                }

                if (_persistentRepository is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _disposed = true;
            }
        }

        ~CompositeSessionRepository()
        {
            Dispose(false);
        }
    }
}
