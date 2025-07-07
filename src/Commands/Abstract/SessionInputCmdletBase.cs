

using System;
using System.Management.Automation;
using LarryWisherMan.ApiUtils.Domain.Models;

namespace LarryWisherMan.ApiUtils.Commands.Abstract
{
    public abstract class SessionInputCmdletBase : SessionApiCmdletBase
    {
        #region Session Input Parameters

        [Parameter(Position = 0, ParameterSetName = "ByName")]
        [ValidateNotNullOrEmpty]
        public string SessionName { get; set; }

        [Parameter(ValueFromPipeline = true, ParameterSetName = "BySession")]
        [ValidateNotNull]
        public ApiSession Session { get; set; }

        #endregion

        protected string ResolveSessionName()
        {
            return Session?.Name ?? SessionName;
        }

        protected ApiSession ResolveSession()
        {
            if (Session != null)
            {
                return Session;
            }

            if (!string.IsNullOrEmpty(SessionName))
            {
                return SessionService.GetSessionAsync(SessionName).GetAwaiter().GetResult();
            }

            return null;
        }
    }
}
