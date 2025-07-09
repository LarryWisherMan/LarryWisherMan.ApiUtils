using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Application.Services;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Infrastructure.Logging;
using LarryWisherMan.ApiUtils.Infrastructure.Parsers;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using LarryWisherMan.ApiUtils.Infrastructure.Services;
using LarryWisherMan.ApiUtils.Runtime;
namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Single, synchronous (netstandard2.0) base class for *all* API cmdlets.
    /// It wires logging, session‑service plumbing, common parameters and
    /// a reusable request/response workflow into one place.
    ///
    /// · Add async / multi‑TFM support later by #if‑guarding the ProcessRecord
    ///   section and inheriting from <see cref="System.Management.Automation.AsyncCmdlet"/>.
    /// </summary>
    public abstract class ApiCmdletBase<TResult> : PSCmdlet, IDisposable
    {
        #region ─── Services / logger ──────────────────────────────────────
        protected IApiSessionService SessionService { get; private set; }
        protected ISessionHttpService HttpService { get; private set; }
        protected CompositeSessionRepository CompositeRepository { get; private set; }
        protected IApiLogger Logger { get; private set; }

        /// <summary>Override for cmdlets such as Connect‑ApiSession that default to file persistence.</summary>
        protected virtual bool DefaultSaveToFile => false;

        /// <summary>Override to raise or lower the default verbosity of this cmdlet.</summary>
        protected virtual LogLevel DefaultLogLevel => LogLevel.Information;
        #endregion

        #region ─── Common parameter set ───────────────────────────────────
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public Uri Uri { get; set; }

        [Parameter]
        [ValidateSet("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", IgnoreCase = true)]
        public string Method { get; set; } = "GET";

        [Parameter] public Hashtable Headers { get; set; }
        [Parameter] public string UserAgent { get; set; }
        [Parameter(ValueFromPipeline = true)] public object Body { get; set; }

        [Parameter]
        [ValidateSet("application/json", "application/xml", "application/x-www-form-urlencoded", "text/plain", "multipart/form-data")]
        public string ContentType { get; set; }

        [Parameter][Credential] public PSCredential Credential { get; set; }
        [Parameter] public string AuthToken { get; set; }
        [Parameter]
        [ValidateSet("Bearer", "Basic", "ApiKey", IgnoreCase = true)]
        public string AuthScheme { get; set; } = "Bearer";

        [Parameter] public SwitchParameter UseDefaultCredentials { get; set; }

        [Parameter][ValidateRange(1, 3600)] public int TimeoutSec { get; set; }
        [Parameter][ValidateRange(1, 100)] public int MaximumRedirection { get; set; }

        [Parameter] public SwitchParameter SkipCertificateCheck { get; set; }

        [Parameter]
        [ValidatePattern(@"^.*\.(json|xml|txt|csv|log)$")]
        public string OutFile { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }
        [Parameter] public SwitchParameter EnableDebugLogging { get; set; }
        #endregion

        #region ─── Lifecycle overrides ────────────────────────────────────
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            InitializeLogging();
            InitializeServices();
            Logger.LogInformation("Starting {0}", GetType().Name);
        }

        protected override void ProcessRecord()
        {
            try
            {
                Logger.LogDebug("Building API request …");
                var request = BuildApiRequest();
                LogRequestDetails(request);

                TResult result = InvokeCore(request);  // implemented by concrete cmdlet

                if (!string.IsNullOrEmpty(OutFile))
                    SaveResultToFile(result);

                if (PassThru)
                    WriteObject(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Cmdlet failed");
                WriteError(new ErrorRecord(ex, GetType().Name, ErrorCategory.NotSpecified, this));
            }
        }

        protected override void EndProcessing()
        {
            Logger.LogInformation("Completed {0}", GetType().Name);
            base.EndProcessing();
            Dispose();
        }
        #endregion

        #region ─── Abstract hook ───────────────────────────────────────────
        /// <summary>
        /// Concrete cmdlets implement their unique logic here using the fully
        /// built <see cref="ApiRequest"/> plus the <see cref="SessionService"/>,
        /// and return whatever object they wish to write.
        /// </summary>
        protected abstract TResult InvokeCore(ApiRequest request);
        #endregion

        #region ─── Service / logger bootstrap ─────────────────────────────
        private void InitializeLogging()
        {
            var level = EnableDebugLogging.IsPresent ? LogLevel.Debug : DefaultLogLevel;
            LoggerFactory.SetLogLevel(level);
            Logger = LoggerFactory.CreateLogger(GetType().Name, this);
        }



        private void InitializeServices()
        {
            CompositeRepository = GlobalServices.Sessions;  // shared
            HttpService = GlobalServices.Http;      // runspace-wide

            var resolver = new RequestResolver(CompositeRepository);
            var parser = new CompositeContentParser();
            var fileService = new FileService();

            SessionService = new ApiSessionService(
                CompositeRepository, HttpService, resolver, parser, fileService);
        }

        #endregion

        #region ─── Helpers ────────────────────────────────────────────────
        protected ApiRequest BuildApiRequest()
        {
            var hdrs = Headers?.Cast<DictionaryEntry>()
                               .ToDictionary(e => e.Key.ToString(), e => e.Value?.ToString())
                       ?? new Dictionary<string, string>();

            string ct = ContentType;
            if (string.IsNullOrWhiteSpace(ct) &&
                (Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                 Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)) &&
                Body != null && !(Body is byte[]))
            {
                ct = "application/json";
            }

            return new ApiRequest
            {
                Uri = Uri,
                Method = Method,
                Headers = hdrs,
                UserAgent = UserAgent,
                ContentType = ct,
                Body = Body,
                Credentials = Credential?.GetNetworkCredential(),
                AuthenticationToken = AuthToken,
                AuthenticationScheme = AuthScheme,
                UseDefaultCredentials = UseDefaultCredentials,
                Timeout = TimeoutSec > 0 ? TimeSpan.FromSeconds(TimeoutSec) : null,
                MaxRedirections = MaximumRedirection > 0 ? MaximumRedirection : null,
                SkipCertificateValidation = SkipCertificateCheck.IsPresent ? (bool?)true : null,
                OutputFilePath = OutFile
            };
        }

        protected void LogRequestDetails(ApiRequest req)
        {
            var url = req.Uri?.ToString() ?? "(null)";
            Logger.LogHttpRequest(req.Method, url);
            if (req.Headers?.Count > 0)
                Logger.LogDebug("Headers: {0}", string.Join("; ", req.Headers.Keys));
        }

        private void SaveResultToFile(TResult result)
        {
            if (result == null) return;
            System.IO.File.WriteAllText(OutFile, result.ToString());
            Logger.LogInformation("Response saved to {0}", OutFile);
        }

        /// <summary>Honours the cmdlet‑level -SaveToFile switch or falls back to the module default.</summary>
        protected bool ShouldSaveToFile(SwitchParameter saveParam)
        {
            return MyInvocation.BoundParameters.ContainsKey("SaveToFile") ? saveParam.ToBool() : DefaultSaveToFile;
        }
        #endregion

        #region ─── IDisposable ────────────────────────────────────────────
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            HttpService?.Dispose();     // keep singleton repo alive
        }

        ~ApiCmdletBase() { Dispose(false); }
        #endregion
    }
}
