using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Domain.Factories
{
    /// <summary>
    /// Factory interface for creating session repository instances.
    /// </summary>
    /// <remarks>
    /// Provides a way to construct a <see cref="CompositeSessionRepository"/> using configurable
    /// options for file persistence, memory caching, and storage location.
    /// </remarks>
    public interface ISessionRepositoryFactory
    {
        /// <summary>
        /// Creates a new <see cref="ISessionRepository"/> instance backed by both memory and optional file storage.
        /// </summary>
        /// <param name="saveToFile">
        /// Whether sessions should be persisted to disk. If <c>false</c>, sessions exist only in memory.
        /// </param>
        /// <param name="preloadFromFile">
        /// Whether existing sessions from persistent storage should be loaded into memory upon creation.
        /// </param>
        /// <param name="storagePath">
        /// Optional path to the session storage directory. If <c>null</c>, a default system location is used (e.g., AppData).
        /// </param>
        /// <returns>
        /// A new <see cref="ISessionRepository"/> instance configured with the specified behavior.
        /// </returns>
        ISessionRepository Create(bool saveToFile = true, bool preloadFromFile = true, string? storagePath = null);
    }
}
