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
    /// Invoke-ApiRestMethod - Session-aware REST method (like Invoke-RestMethod)
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ApiRestMethod")]
    public class InvokeApiRestMethodCommand : HttpApiCmdletBase
    {
        protected override bool ShouldParseContent() => true;

        protected override string GetErrorId() => "ApiRestMethodError";

        protected override object GetResponseOutput(ApiResponse response)
        {
            return response.ParsedContent;
        }
    }
}
