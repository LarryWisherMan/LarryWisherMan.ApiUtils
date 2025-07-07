using System;
using System.Management.Automation;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Application.Services;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using LarryWisherMan.ApiUtils.Infrastructure.Services;

namespace LarryWisherMan.ApiUtils.Commands
{

    /// <summary>
    /// Base class for session-aware API cmdlets
    /// </summary>
    public abstract class SessionApiCmdletBase : PSCmdlet, IDisposable
    {
        protected IApiSessionService SessionService { get; private set; }
        protected ISessionHttpService HttpService { get; private set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            InitializeServices();
        }

        private void InitializeServices()
        {
            var sessionRepository = new FileSessionRepository();
            HttpService = new SessionHttpService();
            var requestResolver = new RequestResolver(sessionRepository);
            var contentParser = new CompositeContentParser();
            var fileService = new FileService();

            SessionService = new ApiSessionService(sessionRepository, HttpService, requestResolver, contentParser, fileService);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            Dispose();
        }

        public void Dispose()
        {
            HttpService?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
