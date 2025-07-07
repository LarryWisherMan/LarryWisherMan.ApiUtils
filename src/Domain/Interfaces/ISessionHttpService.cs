using System.Net.Http;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for session-aware HTTP operations
    /// </summary>
    public interface ISessionHttpService
    {
        Task<HttpResponseMessage> SendAsync(ResolvedApiRequest request);
        void Dispose();
    }
}
