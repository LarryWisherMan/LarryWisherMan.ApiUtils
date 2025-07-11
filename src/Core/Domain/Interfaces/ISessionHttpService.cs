using System;
using System.Net.Http;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for session-aware HTTP operations.
    /// </summary>
    public interface ISessionHttpService : IDisposable
    {
        /// <summary>
        /// Sends a resolved API request using an internal HTTP client.
        /// </summary>
        /// <param name="request">The resolved request containing headers, URL, etc.</param>
        /// <returns>A task representing the asynchronous HTTP operation. The task result contains the response message.</returns>
        Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request);
    }
}
