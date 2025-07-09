using System.Management.Automation;
using System.Net.Http;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Runtime;      // for WriteHttpError extension

namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Invoke-ApiRestMethod – session-aware analogue to Invoke-RestMethod.
    /// Automatically parses JSON/XML into PowerShell objects.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ApiRestMethod")]
    public sealed class InvokeApiRestMethodCommand : ApiCmdletBase<object>
    {
        /// <remarks>
        /// · ParseContent = true  → JSON/XML converted by CompositeContentParser
        /// · ThrowOnError = false → non-2xx codes mapped to WriteHttpError()
        /// </remarks>
        protected override object InvokeCore(ApiRequest request)
        {
            var opts = new ApiRequestOptions
            {
                ParseContent = true,
                ThrowOnError = false
            };

            var response = SessionService.InvokeAsync(request, opts).GetAwaiter().GetResult();


            /* map non-success codes to the PowerShell error stream */
            if (!response.IsSuccessStatusCode)
            {
                WriteError(new ErrorRecord(
                    new HttpRequestException(
                        $"{response.StatusCode} {response.StatusDescription}"),
                    "HttpError",
                    ErrorCategory.InvalidResult,
                    response));
                return null;            // nothing written when PassThru
            }
            return response.ParsedContent;       // caller gets PSCustomObject / XmlDocument / string
        }
    }
}
