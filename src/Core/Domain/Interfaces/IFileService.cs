using System.IO;
using System.Threading.Tasks;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for file operations
    /// </summary>
    public interface IFileService
    {
        Task SaveStreamToFileAsync(Stream stream, string filePath);
        bool DirectoryExists(string path);
        string GetDirectoryName(string filePath);
    }
}
