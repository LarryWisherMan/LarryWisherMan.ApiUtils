using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Domain.Factories
{
    /// <summary>
    /// Factory interface for creating session repository instances.
    /// </summary>
    public interface ISessionRepositoryFactory
    {
        /// <summary>
        /// Creates a new <see cref="ISessionRepository"/> instance backed by both memory and optional file storage.
        /// </summary>
        /// <param name="saveToFile">Whether sessions should be persisted to disk.</param>
        /// <param name="preloadFromFile">Whether to preload sessions from persistent storage into memory.</param>
        /// <param name="storagePath">Optional storage path; if null, a default is used.</param>
        ISessionRepository Create(bool saveToFile, bool preloadFromFile, string? storagePath);
    }
}
