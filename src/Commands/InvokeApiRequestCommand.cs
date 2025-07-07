using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net.Http;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;



namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Invoke-ApiRequest - Session-aware web request (like Invoke-WebRequest)
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ApiRequest")]
    public class InvokeApiRequestCommand : HttpApiCmdletBase
    {
        protected override bool ShouldParseContent() => false;

        protected override string GetErrorId() => "ApiRequestError";

        protected override object GetResponseOutput(ApiResponse response)
        {
            // Create a response object similar to Invoke-WebRequest
            var responseObject = new PSObject();
            responseObject.Properties.Add(new PSNoteProperty("StatusCode", response.StatusCode));
            responseObject.Properties.Add(new PSNoteProperty("StatusDescription", response.StatusDescription));
            responseObject.Properties.Add(new PSNoteProperty("Content", response.RawContent));
            responseObject.Properties.Add(new PSNoteProperty("Headers", response.Headers));
            responseObject.Properties.Add(new PSNoteProperty("SessionName", response.SessionName));
            return responseObject;
        }
    }

}
