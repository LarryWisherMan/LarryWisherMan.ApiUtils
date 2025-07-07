using System;
using System.Collections;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;


namespace LarryWisherMan.ApiUtils.Commands
{

    /// <summary>
    /// Connect-ApiSession - Generic API authentication that handles login and token storage
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "ApiSession")]
    public class ConnectApiSessionCommand : SessionInputCmdletBase
    {
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public Uri BaseUri { get; set; }

        [Parameter(Position = 2, Mandatory = true)]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter(Position = 3, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string LoginEndpoint { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string TokenPropertyName { get; set; } = "access_token";

        [Parameter]
        public int TimeoutSec { get; set; } = 30;

        [Parameter]
        public SwitchParameter SkipCertificateCheck { get; set; }

        [Parameter]
        public Hashtable LoginBody { get; set; }

        [Parameter]
        public Hashtable Headers { get; set; }

        [Parameter]
        public string ContentType { get; set; } = "application/json";

        [Parameter]
        public string UsernameProperty { get; set; } = "username";

        [Parameter]
        public string PasswordProperty { get; set; } = "password";

        [Parameter]
        public string AuthScheme { get; set; } = "Bearer";

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter SaveToFile { get; set; }

        [Parameter]
        public string MFACode { get; set; }

        [Parameter]
        public string MFAPropertyName { get; set; } = "passcode";

        protected override void ProcessRecord()
        {
            try
            {
                var sessionName = ResolveSessionName();
                Logger.LogInformation("Connecting to API: {0}", BaseUri);

                // Create initial session (without auth token) or get existing one
                Logger.LogDebug("Creating/updating session: {0}", sessionName);
                var session = SessionService.CreateSessionAsync(sessionName, BaseUri)
                    .GetAwaiter()
                    .GetResult();

                // Add any provided headers
                if (Headers != null)
                {
                    foreach (DictionaryEntry header in Headers)
                    {
                        session.DefaultHeaders[header.Key.ToString()] = header.Value?.ToString();
                    }
                }

                // Prepare login body
                var loginBodyDict = new System.Collections.Generic.Dictionary<string, object>();

                // Add credential fields
                loginBodyDict[UsernameProperty] = Credential.UserName;
                loginBodyDict[PasswordProperty] = Credential.GetNetworkCredential().Password;

                // Add MFA code if provided
                if (!string.IsNullOrEmpty(MFACode))
                {
                    loginBodyDict[MFAPropertyName] = MFACode;
                    Logger.LogDebug("Added MFA code to login request");
                }

                // Add any additional login body fields
                if (LoginBody != null)
                {
                    foreach (DictionaryEntry item in LoginBody)
                    {
                        loginBodyDict[item.Key.ToString()] = item.Value;
                    }
                }

                var loginBodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(loginBodyDict);
                Logger.LogDebug("Performing authentication to endpoint: {0}", LoginEndpoint);

                Domain.Models.ApiResponse loginResponse;
                try
                {
                    // Perform login request with timeout and error handling
                    loginResponse = SessionService.InvokeAsync(new Domain.Models.ApiRequest
                    {
                        SessionName = sessionName,
                        Uri = new Uri(LoginEndpoint, UriKind.RelativeOrAbsolute),
                        Method = "POST",
                        Body = loginBodyJson,
                        Headers = new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["Content-Type"] = ContentType
                        },
                        Timeout = TimeoutSec > 0 ? TimeSpan.FromSeconds(TimeoutSec) : null,
                        SkipCertificateValidation = SkipCertificateCheck.IsPresent ? (bool?)true : null
                    }, new Domain.Models.ApiRequestOptions
                    {
                        ParseContent = true,
                        ThrowOnError = false
                    }).GetAwaiter().GetResult();

                    Logger.LogDebug("Login request completed with status: {0}", loginResponse.StatusCode);
                }
                catch (System.Threading.Tasks.TaskCanceledException ex)
                {
                    var timeoutMsg = $"Authentication request timed out after {TimeoutSec} seconds. Check network connectivity to {BaseUri}";
                    Logger.LogError("Authentication timeout: {0}", timeoutMsg);
                    WriteError(new ErrorRecord(
                        new TimeoutException(timeoutMsg, ex),
                        "AuthenticationTimeout",
                        ErrorCategory.OperationTimeout,
                        this));
                    return;
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    var networkMsg = $"Network error connecting to {BaseUri}: {ex.Message}";
                    Logger.LogError("Network error: {0}", networkMsg);
                    WriteError(new ErrorRecord(
                        ex,
                        "NetworkError",
                        ErrorCategory.ConnectionError,
                        this));
                    return;
                }

                if (!loginResponse.IsSuccessStatusCode)
                {
                    var errorMsg = $"Authentication failed: {loginResponse.StatusCode} {loginResponse.StatusDescription}";
                    if (!string.IsNullOrEmpty(loginResponse.RawContent))
                    {
                        errorMsg += $"\nResponse: {loginResponse.RawContent}";
                    }

                    Logger.LogError("Authentication failed: {0}", errorMsg);
                    WriteError(new ErrorRecord(
                        new System.Net.Http.HttpRequestException(errorMsg),
                        "AuthenticationFailed",
                        ErrorCategory.AuthenticationError,
                        this));
                    return;
                }

                // Extract token from response
                string token = null;
                if (loginResponse.ParsedContent != null)
                {
                    if (loginResponse.ParsedContent is PSObject psObj)
                    {
                        var tokenProperty = psObj.Properties[TokenPropertyName];
                        if (tokenProperty?.Value != null)
                        {
                            token = tokenProperty.Value.ToString();
                        }
                    }
                    else if (loginResponse.ParsedContent is Newtonsoft.Json.Linq.JObject jObject)
                    {
                        if (jObject.TryGetValue(TokenPropertyName, out var tokenValue))
                        {
                            token = tokenValue.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(token))
                {
                    Logger.LogError("Could not extract token from response using property: {0}", TokenPropertyName);
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Authentication succeeded but could not find token property '{TokenPropertyName}' in response"),
                        "TokenNotFound",
                        ErrorCategory.InvalidResult,
                        loginResponse.ParsedContent));
                    return;
                }

                Logger.LogDebug("Authentication successful, updating session with token");

                // Update session with auth token
                session.AuthenticationToken = token;
                session.AuthenticationScheme = AuthScheme;

                // Save session with the specified save option
                var saveToFile = ShouldSaveToFile(SaveToFile);
                GetCompositeRepository().SaveSessionAsync(session, saveToFile).Wait();

                Logger.LogInformation("Successfully connected and authenticated session: {0}", sessionName);
                WriteVerbose($"Successfully connected to {BaseUri} as {Credential.UserName}");

                if (PassThru)
                {
                    var result = new PSObject();
                    result.Properties.Add(new PSNoteProperty("SessionName", sessionName));
                    result.Properties.Add(new PSNoteProperty("BaseUri", BaseUri));
                    result.Properties.Add(new PSNoteProperty("Username", Credential.UserName));
                    result.Properties.Add(new PSNoteProperty("AuthScheme", AuthScheme));
                    result.Properties.Add(new PSNoteProperty("Connected", true));
                    result.Properties.Add(new PSNoteProperty("SavedToFile", saveToFile));
                    WriteObject(result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Connection failed: {0}", ex.Message);
                WriteError(new ErrorRecord(ex, "ConnectionError", ErrorCategory.NotSpecified, this));
            }
        }
    }


}
