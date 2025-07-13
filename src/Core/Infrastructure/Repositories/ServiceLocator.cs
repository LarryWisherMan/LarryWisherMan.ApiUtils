using LarryWisherMan.ApiUtils.Domain.Factories;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using System;

namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// Provides a static service locator for resolving shared services.
    /// Intended as a lightweight composition root for PowerShell binary modules.
    /// </summary>
    public static class ServiceLocator
    {
        private static ISessionRepository? _sessionRepository;
        private static ISessionRepositoryFactory? _repositoryFactory;

        /// <summary>
        /// Lazily creates and returns the shared session repository instance.
        /// </summary>
        public static ISessionRepository SessionRepository =>
            _sessionRepository ??= CreateRepository();

        /// <summary>
        /// Returns the configured repository factory (used for explicit instantiation).
        /// </summary>
        public static ISessionRepositoryFactory RepositoryFactory =>
            _repositoryFactory ??= new SessionRepositoryFactory();

        /// <summary>
        /// Allows replacing the default repository factory (e.g., for testing).
        /// </summary>
        public static void SetFactory(ISessionRepositoryFactory factory)
        {
            _repositoryFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            _sessionRepository = null; // reset for lazy reinit
        }

        /// <summary>
        /// Allows replacing the session repository manually (e.g., for testing).
        /// </summary>
        public static void SetRepository(ISessionRepository repository)
        {
            _sessionRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Creates a new session repository using the factory and returns it.
        /// This does NOT cache the result in the static instance.
        /// </summary>
        public static ISessionRepository CreateRepository(bool saveToFile = false, bool preloadFromFile = false)
        {
            return RepositoryFactory.Create(saveToFile, preloadFromFile, storagePath: null);
        }

        public static void Reset()
        {
            _sessionRepository = null;
            _repositoryFactory = null;
        }

        /// <summary>
        /// Disposes the session repository if it implements IDisposable.
        /// </summary>
        public static void Dispose()
        {
            if (_sessionRepository is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _sessionRepository = null;
        }
    }
}
