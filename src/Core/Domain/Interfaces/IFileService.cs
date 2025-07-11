using System.IO;
using System.Threading.Tasks;

namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for file operations.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Saves the contents of a stream to a file asynchronously.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="filePath">The path to the target file.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task SaveStreamToFileAsync(Stream stream, string filePath);

        /// <summary>
        /// Determines whether the specified directory exists.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns><c>true</c> if the directory exists; otherwise, <c>false</c>.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Gets the directory name portion of the specified file path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <returns>The directory name.</returns>
        string GetDirectoryName(string filePath);
    }
}
