using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Application.Services;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using LarryWisherMan.ApiUtils.Infrastructure.Services;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Base class for session-aware API cmdlets
    /// </summary>
    public abstract class SessionApiCmdletBase : PSCmdlet, IDisposable
    {
        protected IApiSessionService SessionService { get; private set; }
        protected ISessionHttpService HttpService { get; private set; }
        protected CompositeSessionRepository CompositeRepository { get; private set; }
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Global preference for saving to file by default
        /// Can be overridden by individual cmdlet parameters
        /// </summary>
        protected virtual bool DefaultSaveToFile => false;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Use singleton composite repository to maintain memory cache across cmdlet calls
            CompositeRepository = SessionRepositoryManager.GetInstance(DefaultSaveToFile);


            // Use composite repository for all operations
            HttpService = new SessionHttpService();
            var requestResolver = new RequestResolver(CompositeRepository);
            var contentParser = new CompositeContentParser();
            var fileService = new FileService();

            SessionService = new ApiSessionService(CompositeRepository, HttpService, requestResolver, contentParser, fileService);
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
                // Return the actual value of the switch parameter
                return saveToFileParam.ToBool();
            }

            // If not provided, use the default
            return DefaultSaveToFile;
        }

        protected override void EndProcessing()
        {
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
                HttpService?.Dispose();
            }
        }

        ~SessionApiCmdletBase()
        {
            Dispose(false);
        }
    }
}
