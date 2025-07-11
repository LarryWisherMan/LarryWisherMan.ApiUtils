using System.Collections.Generic;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;


namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for session persistence and management.
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Retrieves a session by name from the backing store.
        /// </summary>
        /// <param name="name">The name of the session.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the session.</returns>
        Task<ApiSession?> GetSessionAsync(string name);

        /// <summary>
        /// Retrieves all persisted sessions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of sessions.</returns>
        Task<IEnumerable<ApiSession>> GetAllSessionsAsync();

        /// <summary>
        /// Saves a session to the backing store.
        /// </summary>
        /// <param name="session">The session to save.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task SaveSessionAsync(ApiSession session);

        /// <summary>
        /// Deletes a session by name.
        /// </summary>
        /// <param name="name">The name of the session to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteSessionAsync(string name);

        /// <summary>
        /// Checks whether a session with the specified name exists.
        /// </summary>
        /// <param name="name">The session name to check.</param>
        /// <returns>A task that returns true if the session exists; otherwise, false.</returns>
        Task<bool> SessionExistsAsync(string name);

        /// <summary>
        /// Updates the last-used timestamp of the session.
        /// </summary>
        /// <param name="name">The name of the session to update.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task UpdateLastUsedAsync(string name);
    }
}
