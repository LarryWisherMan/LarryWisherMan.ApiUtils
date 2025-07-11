using LarryWisherMan.ApiUtils.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for session-aware API request processing
    /// </summary>
    public interface IApiSessionService
    {
        Task<ApiResponse> InvokeAsync(ApiRequest request, ApiRequestOptions options = null);
        Task<ApiSession> CreateSessionAsync(string name, Uri baseUri, string authToken = null, string scheme = "Bearer");
        Task<ApiSession> GetSessionAsync(string name);
        Task<IEnumerable<ApiSession>> GetAllSessionsAsync();
        Task DeleteSessionAsync(string name);
        Task UpdateSessionAsync(ApiSession session);
    }
}
