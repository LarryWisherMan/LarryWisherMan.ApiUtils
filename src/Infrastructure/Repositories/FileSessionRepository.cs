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
    public class FileSessionRepository : ISessionRepository
    {
        private readonly string _sessionDirectory;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public FileSessionRepository(string sessionDirectory = null)
        {
            _sessionDirectory = sessionDirectory ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            ".apiutils", "sessions");

            Directory.CreateDirectory(_sessionDirectory);
        }

        public async Task<ApiSession> GetSessionAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            if (!File.Exists(filePath))
                return null;

            var json = await ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<ApiSession>(json, JsonSettings);
        }

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
                    sessions.Add(session);
                }
                catch
                {
                    // Skip corrupted session files
                }
            }

            return sessions;
        }

        public async Task SaveSessionAsync(ApiSession session)
        {
            var filePath = GetSessionFilePath(session.Name);
            var json = JsonConvert.SerializeObject(session, JsonSettings);
            await WriteAllTextAsync(filePath, json);
        }

        public async Task DeleteSessionAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            await Task.CompletedTask;
        }

        public async Task<bool> SessionExistsAsync(string name)
        {
            var filePath = GetSessionFilePath(name);
            return await Task.Run(() => File.Exists(filePath));
        }

        public async Task UpdateLastUsedAsync(string name)
        {
            var session = await GetSessionAsync(name);
            if (session != null)
            {
                session.LastUsed = DateTime.UtcNow;
                await SaveSessionAsync(session);
            }
        }

        private string GetSessionFilePath(string name)
        {
            var fileName = $"{name}.json";
            return Path.Combine(_sessionDirectory, fileName);
        }

        private async Task<string> ReadAllTextAsync(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fileStream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task WriteAllTextAsync(string filePath, string content)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(fileStream))
            {
                await writer.WriteAsync(content);
            }
        }
    }
}
