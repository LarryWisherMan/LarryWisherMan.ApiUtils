using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Application.Services;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using LarryWisherMan.ApiUtils.Infrastructure.Services;
using LarryWisherMan.ApiUtils.Infrastructure.Logging;


namespace LarryWisherMan.ApiUtils.Commands.Abstract
{

    /// <summary>
    /// Base class for session-aware API cmdlets with lightweight logging support
    /// </summary>
    public abstract class SessionApiCmdletBase : PSCmdlet, IDisposable
    {
        protected IApiSessionService SessionService { get; private set; }
        protected ISessionHttpService HttpService { get; private set; }
        protected CompositeSessionRepository CompositeRepository { get; private set; }
        protected IApiLogger Logger { get; private set; }

        /// <summary>
        /// Global preference for saving to file by default
        /// Can be overridden by individual cmdlet parameters
        /// </summary>
        protected virtual bool DefaultSaveToFile => false;

        /// <summary>
        /// Log level for this cmdlet (can be overridden)
        /// </summary>
        protected virtual LogLevel LogLevel => LogLevel.Information;

        [Parameter]
        public SwitchParameter EnableDebugLogging { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            InitializeLogging();
            InitializeServices();

            Logger.LogInformation("Starting {0}", this.GetType().Name);
        }

        private void InitializeLogging()
        {
            // Set log level based on parameters
            var logLevel = EnableDebugLogging.IsPresent ? LogLevel.Debug : LogLevel;
            LoggerFactory.SetLogLevel(logLevel);

            // Create logger for this cmdlet
            Logger = LoggerFactory.CreateLogger(this.GetType().Name, this);

            Logger.LogDebug("Logging initialized with level: {0}", logLevel);
        }

        private void InitializeServices()
        {
            Logger.LogDebug("Initializing services...");

            // Use singleton composite repository to maintain memory cache across cmdlet calls
            CompositeRepository = SessionRepositoryManager.GetInstance(DefaultSaveToFile);
            Logger.LogDebug("Repository initialized (Singleton: {0})", SessionRepositoryManager.HasInstance);

            // Create other services (these can be per-cmdlet since they don't hold state)
            HttpService = new SessionHttpService();
            var requestResolver = new RequestResolver(CompositeRepository);
            var contentParser = new CompositeContentParser();
            var fileService = new FileService();

            SessionService = new ApiSessionService(CompositeRepository, HttpService, requestResolver, contentParser, fileService);

            Logger.LogDebug("All services initialized");
        }

        /// <summary>
        /// Helper method to get composite repository features
        /// </summary>
        protected CompositeSessionRepository GetCompositeRepository()
        {
            return CompositeRepository;
        }

        /// <summary>
        /// Helper method to check if session should be saved to file
        /// </summary>
        protected bool ShouldSaveToFile(SwitchParameter saveToFileParam)
        {
            // Check if the SaveToFile parameter was explicitly provided
            if (MyInvocation.BoundParameters.ContainsKey("SaveToFile"))
            {
                var result = saveToFileParam.ToBool();
                Logger.LogDebug("SaveToFile parameter specified: {0}", result);
                return result;
            }

            // If not provided, use the default
            Logger.LogDebug("Using default SaveToFile: {0}", DefaultSaveToFile);
            return DefaultSaveToFile;
        }

        protected override void ProcessRecord()
        {
            Logger.LogDebug("Processing record...");
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            Logger.LogInformation("Completed {0}", this.GetType().Name);
            base.EndProcessing();
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger?.LogDebug("Disposing services...");

                // DON'T dispose the singleton repository - it needs to persist
                // Only dispose per-cmdlet services
                HttpService?.Dispose();

                Logger?.LogDebug("Services disposed");
            }
        }

        ~SessionApiCmdletBase()
        {
            Dispose(false);
        }
    }
}
