using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    /// <summary>
    /// Adds -Name, -Id, and -Session pipeline support to a SessionCmdletBase.
    /// </summary>
    public abstract class SessionInputCmdletBase<TResult> : SessionCmdletBase<TResult>
    {
        /* ── three mutually exclusive parameter sets ─────────── */

        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = "ByName")]
        [Alias("SessionName")]
        public string Name { get; set; }

        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "ById")]
        public Guid Id { get; set; }    // can be Guid.Empty when not used

        [Parameter(Mandatory = true,
                   ValueFromPipeline = true,
                   ParameterSetName = "ByObject")]
        public ApiSession Session { get; set; }

        /* ── helper available to concrete cmdlets ────────────── */
        protected ApiSession ResolveSession()
        {
            if (Session != null) return Session;

            if (ParameterSetName == "ById")
                //return SessionService.GetSessionByIdAsync(Id).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(Name))
                    return SessionService.GetSessionAsync(Name)
                                         .GetAwaiter().GetResult();

            return null;
        }

        protected string ResolveSessionName() =>
            Session?.Name ?? Name;
    }
}
