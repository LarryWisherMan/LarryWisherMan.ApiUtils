using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for session-aware HTTP operations.
    /// Wraps HTTP client execution with session context applied.
    /// </summary>
    public interface ISessionHttpService : IDisposable
    {
        /// <summary>
        /// Sends a resolved API request using an internal HTTP client.
        /// </summary>
        /// <param name="request">The resolved request containing URI, headers, method, and body.</param>
        /// <param name="cancellationToken">Token to cancel the request.</param>
        /// <returns>A task representing the asynchronous HTTP operation. The task result contains the HTTP response.</returns>
        Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request, CancellationToken cancellationToken = default);
    }
}
