using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;

namespace LarryWisherMan.ApiUtils.Commands
{
    /// <summary>
    /// New-ApiSession - Creates and saves an API session
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ApiSession")]
    public class NewApiSessionCommand : SessionManagementCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public Uri BaseUri { get; set; }

        [Parameter]
        public SwitchParameter SaveToFile { get; set; }

        // AuthToken, AuthScheme, Headers, Credential, PassThru inherited from SessionManagementCmdletBase

        protected override void ProcessRecord()
        {
            try
            {
                var session = SessionService.CreateSessionAsync(Name, BaseUri, AuthToken, AuthScheme)
                    .GetAwaiter()
                    .GetResult();

                if (Headers != null)
                {
                    foreach (DictionaryEntry header in Headers)
                    {
                        session.DefaultHeaders[header.Key.ToString()] = header.Value?.ToString();
                    }

                }

                if (Credential != null)
                {
                    session.Credentials = Credential.GetNetworkCredential();
                }

                // Save with explicit file control using the helper method
                var saveToFile = ShouldSaveToFile(SaveToFile);
                GetCompositeRepository().SaveSessionAsync(session, saveToFile).Wait();

                WriteVerbose($"Created API session: {Name}" + (saveToFile ? " (saved to file)" : " (memory only)"));

                if (PassThru)
                {
                    WriteObject(session);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "CreateSessionError", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
