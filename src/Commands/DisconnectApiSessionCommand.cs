using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;


namespace LarryWisherMan.ApiUtils.Commands
{



    /// <summary>
    /// Disconnect-ApiSession - Remove authentication from a session
    /// </summary>
    [Cmdlet(VerbsCommunications.Disconnect, "ApiSession")]
    public class DisconnectApiSessionCommand : SessionInputCmdletBase
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

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

                Logger.LogInformation("Disconnecting session: {0}", session.Name);

                // Clear authentication
                session.AuthenticationToken = null;
                session.AuthenticationScheme = null;

                // Update session
                SessionService.UpdateSessionAsync(session).Wait();

                WriteVerbose($"Disconnected session: {session.Name}");

                if (PassThru)
                {
                    var result = new PSObject();
                    result.Properties.Add(new PSNoteProperty("SessionName", session.Name));
                    result.Properties.Add(new PSNoteProperty("BaseUri", session.BaseUri));
                    result.Properties.Add(new PSNoteProperty("Connected", false));
                    WriteObject(result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Disconnect failed: {0}", ex.Message);
                WriteError(new ErrorRecord(ex, "DisconnectError", ErrorCategory.NotSpecified, this));
            }
        }
    }

}
