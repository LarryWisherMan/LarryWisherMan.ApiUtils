namespace LarryWisherMan.ApiUtils.Infrastructure.Repositories
{
    /// <summary>
    /// Singleton manager for the composite session repository
    /// Ensures memory cache persists across cmdlet invocations
    /// </summary>
    public static class SessionRepositoryManager
    {
        private static CompositeSessionRepository _instance;
        private static readonly object _lock = new object();
        private static bool _defaultSaveToFile = true;

        /// <summary>
        /// Get or create the singleton composite repository
        /// </summary>
        public static CompositeSessionRepository GetInstance(bool? defaultSaveToFile = null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    var fileRepository = new FileSessionRepository();
                    var saveToFile = defaultSaveToFile ?? _defaultSaveToFile;
                    _instance = new CompositeSessionRepository(fileRepository, saveToFile);
                    _defaultSaveToFile = saveToFile;
                }
                else if (defaultSaveToFile.HasValue && defaultSaveToFile.Value != _defaultSaveToFile)
                {
                    // If default save behavior changes, we don't recreate the instance
                    // but we can track the new preference for future operations
                    _defaultSaveToFile = defaultSaveToFile.Value;
                }

                return _instance;
            }
        }

        /// <summary>
        /// Reset the singleton (useful for testing or configuration changes)
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

        /// <summary>
        /// Get the current default save behavior
        /// </summary>
        public static bool DefaultSaveToFile => _defaultSaveToFile;

        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool HasInstance => _instance != null;
    }

}
