using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;





namespace LarryWisherMan.ApiUtils.Commands
{
    [Cmdlet(VerbsCommon.Get, "ApiSession")]
    public sealed class GetApiSessionCommand : SessionCmdletBase<object>
    {
        [Parameter] public SwitchParameter Detailed { get; set; }  // example of extra flag

        protected override object InvokeCore()
        {
            if (string.IsNullOrEmpty(Name))
                return SessionService.GetAllSessionsAsync().GetAwaiter().GetResult();

            var s = SessionService.GetSessionAsync(Name).GetAwaiter().GetResult();
            if (s == null)
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Session '{Name}' not found"),
                    "SessionNotFound", ErrorCategory.ObjectNotFound, Name));
            }
            return s;
        }
    }
}
