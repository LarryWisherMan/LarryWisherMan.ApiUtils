using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Runtime;   // GlobalServices.Instance

namespace LarryWisherMan.ApiUtils.Commands.Session
{
    /// <summary>
    /// New-ApiSession – creates a new session object and, optionally, persists it.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ApiSession")]
    [OutputType(typeof(ApiSession))]
    public sealed class NewApiSessionCommand : SessionCmdletBase<ApiSession>
    {
        /*───── mandatory args ─────*/
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty] public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNull] public Uri BaseUri { get; set; }

        /*───── optional args ─────*/
        [Parameter] public Hashtable Headers { get; set; }
        [Parameter][Credential] public PSCredential Credential { get; set; }
        [Parameter] public string AuthToken { get; set; }
        [Parameter] public string AuthScheme { get; set; } = "Bearer";
        [Parameter] public SwitchParameter SaveToFile { get; set; }

        /*───── InvokeCore: actual logic ─────*/
        protected override ApiSession InvokeCore()
        {
            // 1) create session in memory
            var session = SessionService.CreateSessionAsync(Name, BaseUri,
                                                            AuthToken, AuthScheme)
                                        .GetAwaiter().GetResult();

            // 2) headers
            if (Headers != null)
            {
                foreach (DictionaryEntry h in Headers)
                    session.DefaultHeaders[h.Key.ToString()] = h.Value?.ToString();
            }

            // 3) credentials
            if (Credential != null)
                session.Credentials = Credential.GetNetworkCredential();

            // 4) persist (file vs memory only)
            var persist = SaveToFile.IsPresent;
            GlobalServices.Instance.SessionRepository
                           .SaveSessionAsync(session, persist)
                           .GetAwaiter().GetResult();

            WriteVerbose($"Created API session '{Name}' " +
                         (persist ? "(saved to file)" : "(memory only)"));

            return session;   // base class writes this if -PassThru was used
        }
    }
}
