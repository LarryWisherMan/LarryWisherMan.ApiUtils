using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for resolving requests with session data.
    /// </summary>
    public interface IRequestResolver
    {
        /// <summary>
        /// Resolves an API request using session metadata (e.g., headers, auth).
        /// </summary>
        /// <param name="request">The logical API request.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the resolved request.</returns>
        Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request);
    }
}
