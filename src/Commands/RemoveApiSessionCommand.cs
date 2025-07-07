using System;
using System.Collections;
using System.Management.Automation;

namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Remove-ApiSession - Deletes an API session
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "ApiSession", SupportsShouldProcess = true)]
    public class RemoveApiSessionCommand : SessionApiCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (ShouldProcess(Name, "Remove API Session"))
                {
                    SessionService.DeleteSessionAsync(Name).GetAwaiter().GetResult();
                    WriteVerbose($"Removed API session: {Name}");
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "RemoveSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
