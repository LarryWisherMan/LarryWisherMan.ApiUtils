using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net.Http;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Runtime;          // if you keep WriteHttpError here

namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Invoke-ApiRequest – session-aware analogue to Invoke-WebRequest.
    /// Returns a PSObject that exposes status, headers, raw content, etc.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ApiRequest")]
    public sealed class InvokeApiRequestCommand : ApiCmdletBase<PSObject>
    {
        /// <remarks>
        /// No JSON/XML parsing → caller gets the raw body.  Simply keep
        /// <c>ParseContent = false</c> like the old ShouldParseContent() flag.
        /// </remarks>
        protected override PSObject InvokeCore(ApiRequest req)
        {
            var options = new ApiRequestOptions
            {
                ParseContent = false,   // replaces ShouldParseContent() => false
                ThrowOnError = false
            };


            var resp = SessionService.InvokeAsync(req, options).GetAwaiter().GetResult();

            /* map non-success codes to the PowerShell error stream */
            if (!resp.IsSuccessStatusCode)
            {
                WriteError(new ErrorRecord(
                    new HttpRequestException(
                        $"{resp.StatusCode} {resp.StatusDescription}"),
                    "HttpError",
                    ErrorCategory.InvalidResult,
                    resp));
                return null;            // nothing written when PassThru
            }

            // Build an Invoke-WebRequest-style wrapper
            /* build Invoke-WebRequest-style wrapper */
            var o = new PSObject();
            o.Properties.Add(new PSNoteProperty("StatusCode", resp.StatusCode));
            o.Properties.Add(new PSNoteProperty("StatusDescription", resp.StatusDescription));
            o.Properties.Add(new PSNoteProperty("Content", resp.RawContent));
            o.Properties.Add(new PSNoteProperty("Headers", resp.Headers));
            o.Properties.Add(new PSNoteProperty("SessionName", resp.SessionName));

            return o;                   // ApiCmdletBase writes when -PassThru
        }


    }
}
