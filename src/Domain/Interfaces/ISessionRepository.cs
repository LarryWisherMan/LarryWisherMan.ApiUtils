using System.Collections.Generic;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;


namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for session persistence and management
    /// </summary>
    public interface ISessionRepository
    {
        Task<ApiSession> GetSessionAsync(string name);
        Task<IEnumerable<ApiSession>> GetAllSessionsAsync();
        Task SaveSessionAsync(ApiSession session);
        Task DeleteSessionAsync(string name);
        Task<bool> SessionExistsAsync(string name);
        Task UpdateLastUsedAsync(string name);
    }
}
