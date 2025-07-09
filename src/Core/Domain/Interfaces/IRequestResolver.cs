using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for resolving requests with session data
    /// </summary>
    public interface IRequestResolver
    {
        Task<ResolvedApiRequest> ResolveRequestAsync(ApiRequest request);
    }
}
