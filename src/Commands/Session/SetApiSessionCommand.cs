using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Session
{
    /// <summary>
    /// Set-ApiSession – updates an existing session’s properties.
    /// Supports -Name, -Id, or a piped ApiSession object.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ApiSession", SupportsShouldProcess = true)]
    [OutputType(typeof(ApiSession))]
    public sealed class SetApiSessionCommand : SessionInputCmdletBase<ApiSession>
    {
        /*───────── updateable fields ─────────*/
        [Parameter] public Uri BaseUri { get; set; }
        [Parameter] public string AuthToken { get; set; }
        [Parameter] public string AuthScheme { get; set; }
        [Parameter] public Hashtable Headers { get; set; }
        [Parameter][Credential] public PSCredential Credential { get; set; }

        /*───────── actual logic ─────────*/
        protected override ApiSession InvokeCore()
        {
            var session = ResolveSession();
            if (session is null)
                throw new ItemNotFoundException("Session not found.");

            if (!ShouldProcess(session.Name, "Update API session"))
                return session;                 // no change when -WhatIf

            /* apply updates only when provided */
            if (BaseUri != null)
                session.BaseUri = BaseUri;

            if (!string.IsNullOrEmpty(AuthToken))
                session.AuthenticationToken = AuthToken;

            if (!string.IsNullOrEmpty(AuthScheme))
                session.AuthenticationScheme = AuthScheme;

            if (Credential != null)
                session.Credentials = Credential.GetNetworkCredential();

            if (Headers != null)
            {
                foreach (DictionaryEntry h in Headers)
                    session.DefaultHeaders[h.Key.ToString()] = h.Value?.ToString();
            }

            SessionService.UpdateSessionAsync(session)
                          .GetAwaiter().GetResult();

            WriteVerbose($"Updated API session: {session.Name}");
            return session;                     // written when -PassThru
        }
    }
}
