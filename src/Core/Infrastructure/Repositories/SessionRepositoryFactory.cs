using System;
using LarryWisherMan.ApiUtils.Domain.Factories;
using LarryWisherMan.ApiUtils.Domain.Interfaces;


namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// Default implementation of <see cref="ISessionRepositoryFactory"/>.
    /// Produces composite session repositories that combine memory and optional file persistence.
    /// </summary>
    public class SessionRepositoryFactory : ISessionRepositoryFactory
    {
        /// <summary>
        /// Creates a new <see cref="ISessionRepository"/> using memory and optional file persistence.
        /// </summary>
        /// <param name="saveToFile">
        /// If true, sessions will be saved to disk; otherwise, they remain memory-only.
        /// </param>
        /// <param name="preloadFromFile">
        /// If true, all file-based sessions will be loaded into memory cache on creation.
        /// Ignored if <paramref name="saveToFile"/> is false.
        /// </param>
        /// <param name="storagePath">
        /// Optional path to the folder where session files are stored. If null, a default is used.
        /// </param>
        /// <returns>
        /// A configured <see cref="ISessionRepository"/> that wraps both memory and file-based persistence.
        /// </returns>
        public ISessionRepository Create(bool saveToFile = true, bool preloadFromFile = true, string? storagePath = null)
        {
            ISessionRepository fileRepository;

            if (saveToFile)
            {
                fileRepository = storagePath is not null
                    ? new FileSessionRepository(storagePath)
                    : new FileSessionRepository(); // uses default AppData location
            }
            else
            {
                // Use a null-object that satisfies the interface but does nothing
                fileRepository = new NoOpSessionRepository();
            }

            var composite = new CompositeSessionRepository(fileRepository, saveToFile);

            if (saveToFile && preloadFromFile)
            {
                // Fire and forget preload; caller can await explicitly if needed
                _ = composite.PreloadCacheAsync();
            }

            return composite;
        }
    }
}
