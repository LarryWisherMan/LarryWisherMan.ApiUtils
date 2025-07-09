using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Commands.Abstract;
using LarryWisherMan.ApiUtils.Domain.Models;
using LarryWisherMan.ApiUtils.Runtime;        // GlobalServices
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LarryWisherMan.ApiUtils.Commands.Session
{
    [Cmdlet(VerbsCommunications.Connect, "ApiSession")]
    [OutputType(typeof(PSObject))]
    public sealed class ConnectApiSessionCommand
        : SessionInputCmdletBase<PSObject>
    {
        /*───────── login parameters ─────────*/
        [Parameter(Mandatory = true)] public Uri BaseUri { get; set; }
        [Parameter(Mandatory = true)] public string LoginEndpoint { get; set; }

        [Parameter(Mandatory = true)]
        public string TokenPropertyName { get; set; } = "access_token";

        [Parameter] public Hashtable LoginBody { get; set; }
        [Parameter] public string UsernameProperty { get; set; } = "username";
        [Parameter] public string PasswordProperty { get; set; } = "password";
        [Parameter] public string MFACode { get; set; }
        [Parameter] public string MFAPropertyName { get; set; } = "passcode";
        [Parameter] public SwitchParameter SaveToFile { get; set; }

        /*───────── generic HTTP knobs ───────*/
        [Parameter] public Hashtable Headers { get; set; }

        [Parameter, Credential]
        public PSCredential Credential { get; set; }

        [Parameter] public string ContentType { get; set; } = "application/json";
        [Parameter] public int TimeoutSec { get; set; } = 30;
        [Parameter] public SwitchParameter SkipCertificateCheck { get; set; }
        [Parameter] public string AuthScheme { get; set; } = "Bearer";

        /*───────── actual logic – required by base class ─────────*/
        protected override PSObject InvokeCore()
        {
            /* Resolve or create session */
            var session = ResolveSession()
                       ?? SessionService.CreateSessionAsync(
                              Name ?? Guid.NewGuid().ToString(), BaseUri)
                          .GetAwaiter().GetResult();

            /*Build login body */
            if (Credential == null)
                throw new PSArgumentNullException(nameof(Credential),
                    "Credential is required for login.");

            var body = new Dictionary<string, object?>
            {
                [UsernameProperty] = Credential.UserName,
                [PasswordProperty] = Credential.GetNetworkCredential().Password
            };

            if (!string.IsNullOrEmpty(MFACode))
                body[MFAPropertyName] = MFACode;

            if (LoginBody != null)
                foreach (DictionaryEntry kv in LoginBody)
                    body[kv.Key.ToString()] = kv.Value;

            var bodyJson = JsonConvert.SerializeObject(body);

            /* Build & execute request */
            var req = new ApiRequest
            {
                SessionName = session.Name,
                Uri = new Uri(LoginEndpoint, UriKind.RelativeOrAbsolute),
                Method = "POST",
                Body = bodyJson,
                ContentType = ContentType,
                Headers = HashtableToDict(Headers),
                Timeout = TimeSpan.FromSeconds(TimeoutSec),
                SkipCertificateValidation = SkipCertificateCheck.IsPresent
            };

            var resp = SessionService.InvokeAsync(
                           req,
                           new ApiRequestOptions { ParseContent = true, ThrowOnError = false })
                         .GetAwaiter().GetResult();

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Login failed: {resp.StatusCode} {resp.StatusDescription}\n{resp.RawContent}");

            /* Extract token */
            var token = ExtractToken(resp.ParsedContent);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException(
                    $"Login succeeded but '{TokenPropertyName}' not found in response.");

            /* Persist token on session */
            session.AuthenticationToken = token;
            session.AuthenticationScheme = AuthScheme;
            SessionService.UpdateSessionAsync(session).GetAwaiter().GetResult();

            /* optional file save */
            if (SaveToFile.IsPresent)
                GlobalServices.Instance.SessionRepository
                              .SaveSessionAsync(session, true)
                              .GetAwaiter().GetResult();

            WriteVerbose($"Connected as {Credential.UserName}; token stored.");

            /* Return summary object (base writes when -PassThru) */
            var o = new PSObject();
            o.Properties.Add(new PSNoteProperty("SessionName", session.Name));
            o.Properties.Add(new PSNoteProperty("BaseUri", session.BaseUri));
            o.Properties.Add(new PSNoteProperty("Username", Credential.UserName));
            o.Properties.Add(new PSNoteProperty("AuthScheme", AuthScheme));
            o.Properties.Add(new PSNoteProperty("SavedToFile", SaveToFile.IsPresent));
            return o;
        }

        /*───────── helpers ─────────*/
        private static Dictionary<string, string> HashtableToDict(Hashtable ht)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (ht == null) return d;
            foreach (DictionaryEntry kv in ht)
                d[kv.Key.ToString()] = kv.Value?.ToString();
            return d;
        }

        private string ExtractToken(object parsed) => parsed switch
        {
            JObject j => j[TokenPropertyName]?.ToString(),
            IDictionary<string, object> d when d.ContainsKey(TokenPropertyName)
                                                      => d[TokenPropertyName]?.ToString(),
            _ => null
        };
    }
}
