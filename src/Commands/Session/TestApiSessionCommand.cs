using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Session
{
    /// <summary>
    /// Test-ApiSession – performs a lightweight GET to verify connectivity.
    /// Supports -Name, -Id, or piping an ApiSession object.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "ApiSession")]
    [OutputType(typeof(PSObject))]
    public sealed class TestApiSessionCommand : SessionInputCmdletBase<PSObject>
    {
        /// <summary>
        /// Relative path or absolute URL to hit (defaults to “/”).
        /// </summary>
        [Parameter] public string TestEndpoint { get; set; } = "/";

        /// <summary>
        /// Required by SessionInputCmdletBase – contains the real work.
        /// </summary>
        protected override PSObject InvokeCore()
        {
            var session = ResolveSession()
                       ?? throw new ItemNotFoundException("Session not found.");

            /* Build target URI (absolute or relative) */
            var targetUri = Uri.TryCreate(TestEndpoint, UriKind.Absolute, out var abs)
                            ? abs
                            : new Uri(session.BaseUri, TestEndpoint);

            /* Prepare request */
            var req = new ApiRequest
            {
                SessionName = session.Name,
                Uri = targetUri,
                Method = "GET"
            };

            var opts = new ApiRequestOptions
            {
                ParseContent = false,
                ThrowOnError = false
            };

            /* Execute */
            var resp = SessionService.InvokeAsync(req, opts)
                                     .GetAwaiter().GetResult();

            /* Shape output */
            var result = new PSObject();
            result.Properties.Add(new PSNoteProperty("SessionName", session.Name));
            result.Properties.Add(new PSNoteProperty("Endpoint", targetUri));
            result.Properties.Add(new PSNoteProperty("StatusCode", resp.StatusCode));
            result.Properties.Add(new PSNoteProperty("IsSuccess", resp.IsSuccessStatusCode));
            result.Properties.Add(new PSNoteProperty("ResponseTime", resp.ResponseTime));

            return result;   // base class writes this (PassThru not needed here)
        }
    }
}
