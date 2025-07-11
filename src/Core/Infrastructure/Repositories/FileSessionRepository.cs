using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// File-based implementation of <see cref="ISessionRepository"/>.
    /// Persists session data as JSON files in a local user profile directory.
    /// </summary>
    public sealed class FileSessionRepository : ISessionRepository
    {
        private readonly string _sessionDirectory;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        /// <summary>
        /// Creates a new <see cref="FileSessionRepository"/> instance with optional storage location.
        /// </summary>
        /// <param name="sessionDirectory">
        /// Optional override for the storage path. Defaults to: %USERPROFILE%\.apiutils\sessions
        /// </param>
        public FileSessionRepository(string? sessionDirectory = null)
        {
            _sessionDirectory = sessionDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".apiutils", "sessions");

            Directory.CreateDirectory(_sessionDirectory);
        }

        /// <inheritdoc />
        public async Task<ApiSession?> GetSessionAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            if (!File.Exists(filePath))
                return null;

            var json = await ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<ApiSession>(json, JsonSettings);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ApiSession>> GetAllSessionsAsync()
        {
            var sessions = new List<ApiSession>();
            var files = Directory.GetFiles(_sessionDirectory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await ReadAllTextAsync(file);
                    var session = JsonConvert.DeserializeObject<ApiSession>(json, JsonSettings);
                    if (session is not null)
                        sessions.Add(session);
                }
                catch
                {
                    // Skip corrupted files
                }
            }

            return sessions;
        }

        /// <inheritdoc />
        public async Task SaveSessionAsync(ApiSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var filePath = GetSessionFilePath(session.Name);
            var json = JsonConvert.SerializeObject(session, JsonSettings);
            await WriteAllTextAsync(filePath, json);
        }

        /// <inheritdoc />
        public Task DeleteSessionAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> SessionExistsAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            return Task.FromResult(File.Exists(filePath));
        }

        /// <inheritdoc />
        public async Task UpdateLastUsedAsync(string name)
        {
            var session = await GetSessionAsync(name);
            if (session is not null)
            {
                session.LastUsed = DateTime.UtcNow;
                await SaveSessionAsync(session);
            }
        }

        /* ─────────────── HELPERS ─────────────── */

        private string GetSessionFilePath(string name) =>
            Path.Combine(_sessionDirectory, $"{name}.json");

        private static async Task<string> ReadAllTextAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private static async Task WriteAllTextAsync(string filePath, string content)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
        }
    }
}
