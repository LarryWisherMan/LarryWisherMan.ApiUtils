using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Base class for session management cmdlets with common session parameters
    /// </summary>
    public abstract class SessionManagementCmdletBase : SessionApiCmdletBase
    {
        [Parameter]
        public string AuthToken { get; set; }

        [Parameter]
        public string AuthScheme { get; set; } = "Bearer";

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }
    }

}
