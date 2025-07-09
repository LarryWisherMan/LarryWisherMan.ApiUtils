using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Session
{
    /// <summary>
    /// Remove-ApiSession – deletes an API session from the repository.
    /// Supports -Name, -Id, or piping in an ApiSession object.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "ApiSession",
            SupportsShouldProcess = true,
            ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(bool))]
    public sealed class RemoveApiSessionCommand : SessionInputCmdletBase<bool>
    {
        /// <summary>
        /// Actual execution logic; the generic base class wraps this in boilerplate
        /// (Begin/End/PassThru, error handling, etc.).
        /// </summary>
        protected override bool InvokeCore()
        {
            /* ── resolve target session ───────────────────────── */
            var session = ResolveSession();
            if (session is null)
                throw new ItemNotFoundException("Session not found.");

            /* ── honour -WhatIf / -Confirm ───────────────────── */
            if (!ShouldProcess(session.Name, "Remove API session"))
                return false;

            /* ── perform delete ──────────────────────────────── */
            SessionService.DeleteSessionAsync(session.Name)
                          .GetAwaiter().GetResult();

            WriteVerbose($"Removed API session: {session.Name}");
            return true;        // base class writes this if -PassThru was used
        }
    }
}
