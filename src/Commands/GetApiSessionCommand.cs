using System;
using System.Management.Automation;



namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// Get-ApiSession - Retrieves API sessions
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ApiSession")]
    public class GetApiSessionCommand : SessionApiCmdletBase
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrEmpty(Name))
                {
                    var sessions = SessionService.GetAllSessionsAsync().GetAwaiter().GetResult();
                    WriteObject(sessions, true);
                }
                else
                {
                    var session = SessionService.GetSessionAsync(Name).GetAwaiter().GetResult();
                    if (session != null)
                    {
                        WriteObject(session);
                    }
                    else
                    {
                        WriteError(new ErrorRecord(
                            new ItemNotFoundException($"Session '{Name}' not found"),
                            "SessionNotFound",
                            ErrorCategory.ObjectNotFound,
                            Name));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "GetSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
