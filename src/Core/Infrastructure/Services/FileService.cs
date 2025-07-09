using System.IO;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task SaveStreamToFileAsync(Stream stream, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
        }

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetDirectoryName(string filePath) => Path.GetDirectoryName(filePath);
    }
}
