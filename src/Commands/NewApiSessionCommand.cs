using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace LarryWisherMan.ApiUtils.Commands
{
    [Cmdlet(VerbsCommon.New, "ApiSession")]
    public class NewApiSessionCommand : SessionApiCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public Uri BaseUri { get; set; }

        [Parameter]
        public string AuthToken { get; set; }

        [Parameter]
        public string AuthScheme { get; set; } = "Bearer";

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var session = SessionService.CreateSessionAsync(Name, BaseUri, AuthToken, AuthScheme)
                    .GetAwaiter()
                    .GetResult();

                if (Headers != null)
                {
                    // Initialize DefaultHeaders if it's null
                    if (session.DefaultHeaders == null)
                    {
                        session.DefaultHeaders = new Dictionary<string, string>();
                    }

                    foreach (DictionaryEntry header in Headers)
                    {
                        var key = header.Key?.ToString();
                        var value = header.Value?.ToString();

                        if (!string.IsNullOrEmpty(key))
                        {
                            session.DefaultHeaders[key] = value ?? string.Empty;
                        }
                    }

                    SessionService.UpdateSessionAsync(session).GetAwaiter().GetResult();
                }

                if (Credential != null)
                {
                    session.Credentials = Credential.GetNetworkCredential();
                    SessionService.UpdateSessionAsync(session).GetAwaiter().GetResult();
                }

                WriteVerbose($"Created API session: {Name}");

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
