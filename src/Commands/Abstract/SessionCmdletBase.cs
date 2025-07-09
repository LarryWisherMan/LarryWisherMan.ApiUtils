using System;
using System.Management.Automation;

using LarryWisherMan.ApiUtils.Application.Services;
using LarryWisherMan.ApiUtils.Infrastructure.Logging;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using LarryWisherMan.ApiUtils.Infrastructure.Services;
using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
using LarryWisherMan.ApiUtils.Runtime;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Base for *session-management* cmdlets (Get-, New-, Set-, Remove-, Test-).
    /// No HTTP parameters – only Name / Session switches and common plumbing.
    /// </summary>
    public abstract class SessionCmdletBase<TResult> : PSCmdlet
    {
        /* ─── Common parameters ──────────────────────────────── */
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("SessionName")]
        public string Name { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }
        [Parameter] public SwitchParameter EnableDebugLogging { get; set; }

        /* ─── Shared services ────────────────────────────────── */
        protected IApiSessionService SessionService { get; private set; }
        protected CompositeSessionRepository Repo { get; private set; }
        protected IApiLogger Logger { get; private set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            var lvl = EnableDebugLogging.IsPresent ? LogLevel.Debug : LogLevel.Information;
            LoggerFactory.SetLogLevel(lvl);
            Logger = LoggerFactory.CreateLogger(GetType().Name, this);

            Repo = GlobalServices.Sessions;   // singleton
            var http = GlobalServices.Http;       // runspace-wide
            var resolver = new RequestResolver(Repo);
            var parser = new CompositeContentParser();
            var fileSvc = new FileService();

            SessionService = new ApiSessionService(Repo, http, resolver, parser, fileSvc);
        }

        protected override void ProcessRecord()
        {
            try
            {
                var result = InvokeCore();
                if (PassThru && result is not null)
                    WriteObject(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Session cmdlet failed");
                WriteError(new ErrorRecord(ex, GetType().Name,
                              ErrorCategory.NotSpecified, this));
            }
        }

        /// <summary>Concrete cmdlets implement their behaviour here.</summary>
        protected abstract TResult InvokeCore();
    }
}
