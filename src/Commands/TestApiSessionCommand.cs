using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;



namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Test-ApiSession - Tests connectivity of an API session
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "ApiSession")]
    public class TestApiSessionCommand : SessionInputCmdletBase
    {
        [Parameter]
        public string TestEndpoint { get; set; } = "/";

        // SessionName and Session inherited from SessionInputCmdletBase
        // NO duplicate InitializeServices, EndProcessing, or Dispose methods!

        protected override void ProcessRecord()
        {
            try
            {
                var session = ResolveSession();
                if (session == null)
                {
                    var sessionName = ResolveSessionName();
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException($"Session '{sessionName}' not found"),
                        "SessionNotFound",
                        ErrorCategory.ObjectNotFound,
                        sessionName));
                    return;
                }

                var testUri = new Uri(session.BaseUri, TestEndpoint);
                var request = new ApiRequest
                {
                    SessionName = session.Name,
                    Uri = testUri,
                    Method = "GET"
                };

                var options = new ApiRequestOptions
                {
                    ThrowOnError = false,
                    ParseContent = false
                };

                var response = SessionService.InvokeAsync(request, options)
                    .GetAwaiter()
                    .GetResult();

                var result = new PSObject();
                result.Properties.Add(new PSNoteProperty("SessionName", session.Name));
                result.Properties.Add(new PSNoteProperty("BaseUri", session.BaseUri));
                result.Properties.Add(new PSNoteProperty("TestEndpoint", TestEndpoint));
                result.Properties.Add(new PSNoteProperty("StatusCode", response.StatusCode));
                result.Properties.Add(new PSNoteProperty("IsSuccess", response.IsSuccessStatusCode));
                result.Properties.Add(new PSNoteProperty("ResponseTime", response.ResponseTime));

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "TestSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
