using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;

namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Remove-ApiSession - Deletes an API session
    /// </summary>    /// <summary>
    /// Remove-ApiSession - Deletes an API session
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "ApiSession", SupportsShouldProcess = true)]
    public class RemoveApiSessionCommand : SessionInputCmdletBase
    {
        protected override void ProcessRecord()
        {
            try
            {
                var sessionName = ResolveSessionName();

                if (string.IsNullOrEmpty(sessionName))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("No session specified"),
                        "NoSessionSpecified",
                        ErrorCategory.InvalidArgument,
                        this));
                    return;
                }

                if (ShouldProcess(sessionName, "Remove API Session"))
                {
                    SessionService.DeleteSessionAsync(sessionName).GetAwaiter().GetResult();
                    WriteVerbose($"Removed API session: {sessionName}");
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "RemoveSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
