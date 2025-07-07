using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;



namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Set-ApiSession - Updates an existing API session
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ApiSession")]
    public class SetApiSessionCommand : SessionInputCmdletBase
    {
        [Parameter]
        public Uri BaseUri { get; set; }

        [Parameter]
        public string AuthToken { get; set; }

        [Parameter]
        public string AuthScheme { get; set; }

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        // SessionName and Session inherited from SessionInputCmdletBase

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

                // Update properties if provided
                if (BaseUri != null)
                    session.BaseUri = BaseUri;

                if (!string.IsNullOrEmpty(AuthToken))
                    session.AuthenticationToken = AuthToken;

                if (!string.IsNullOrEmpty(AuthScheme))
                    session.AuthenticationScheme = AuthScheme;

                if (Headers != null)
                {
                    foreach (DictionaryEntry header in Headers)
                    {
                        session.DefaultHeaders[header.Key.ToString()] = header.Value?.ToString();
                    }
                }

                if (Credential != null)
                    session.Credentials = Credential.GetNetworkCredential();

                SessionService.UpdateSessionAsync(session).GetAwaiter().GetResult();
                WriteVerbose($"Updated API session: {session.Name}");

                if (PassThru)
                {
                    WriteObject(session);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SetSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
