using System.Threading;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Resolves and expands a user-provided API request into a fully composed request
    /// by merging session defaults and validating the final result.
    /// </summary>
    public interface IApiRequestResolver
    {
        /// <summary>
        /// Builds a ResolvedApiRequest using session data and request overrides.
        /// </summary>
        /// <param name="request">The incoming API request with optional session binding.</param>
        /// <param name="cancellationToken">A cancellation token for async control.</param>
        /// <returns>A fully composed and validated API request ready for dispatch.</returns>
        Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request, CancellationToken cancellationToken = default);
    }
}
