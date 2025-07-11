using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Defines operations for managing and invoking API sessions.
    /// </summary>
    public interface IApiSessionService
    {
        /// <summary>
        /// Sends an API request using the specified session and returns the response.
        /// </summary>
        /// <param name="request">The request to send, including URI, headers, and body.</param>
        /// <param name="options">Optional settings for controlling behavior like pass-through, error handling, etc.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response.</returns>
        Task<ApiResponse> InvokeAsync(ApiRequest request, ApiRequestOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new named API session using the specified base URI and optional authentication token.
        /// </summary>
        /// <param name="name">The unique name to identify the session.</param>
        /// <param name="baseUri">The base URI for all requests in the session.</param>
        /// <param name="authToken">Optional bearer token or API key for authentication.</param>
        /// <param name="scheme">The authentication scheme (e.g., Bearer, Basic). Default is "Bearer".</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created session.</returns>
        Task<ApiSession> CreateSessionAsync(string name, Uri baseUri, string? authToken = null, string scheme = "Bearer", CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a previously created session by name.
        /// </summary>
        /// <param name="name">The name of the session to retrieve.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the session.</returns>
        Task<ApiSession> GetSessionAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all currently stored sessions.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all sessions.</returns>
        Task<IEnumerable<ApiSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the specified session by name.
        /// </summary>
        /// <param name="name">The name of the session to delete.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> DeleteSessionAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the state or configuration of an existing session.
        /// </summary>
        /// <param name="session">The session object with updated properties to persist.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> UpdateSessionAsync(ApiSession session, CancellationToken cancellationToken = default);
    }
}
