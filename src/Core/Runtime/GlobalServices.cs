using System.Threading;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using LarryWisherMan.ApiUtils.Infrastructure.Services;

namespace LarryWisherMan.ApiUtils.Runtime
{
    /// <summary>
    /// Cross-runspace singletons and helpers.
    ///
    /// ─ Lifetime table ───────────────────────────────────────────────
    /// Sessions : single global repository shared by all runspaces
    /// Http     : one SessionHttpService per runspace/thread (AsyncLocal)
    /// </summary>
    public sealed class GlobalServices
    {
        /*-----------------------------------------------------------------
         *  PUBLIC STATIC FACADE  (keeps legacy code working)
         *----------------------------------------------------------------*/
        public static CompositeSessionRepository Sessions => Instance.SessionRepository;
        public static SessionHttpService Http => Instance.GetHttpForCurrentRunspace();

        /*-----------------------------------------------------------------
         *  SINGLETON INSTANCE  (preferred for new code)
         *----------------------------------------------------------------*/
        public static GlobalServices Instance { get; } = new GlobalServices();

        /*-----------------------------------------------------------------
         *  PUBLIC INSTANCE PROPERTIES
         *----------------------------------------------------------------*/
        public CompositeSessionRepository SessionRepository { get; }

        /*-----------------------------------------------------------------
         *  INTERNAL IMPLEMENTATION
         *----------------------------------------------------------------*/
        private readonly AsyncLocal<SessionHttpService> _http = new();

        private GlobalServices()
        {
            // Disk + memory repository (false = do not save by default)
            SessionRepository = SessionRepositoryManager.GetInstance(defaultSaveToFile: false);
        }

        private SessionHttpService GetHttpForCurrentRunspace() =>
            _http.Value ??= new SessionHttpService();

        /*-----------------------------------------------------------------
         *  TEST-HELPER
         *----------------------------------------------------------------*/
        internal static void ResetHttp() => Instance._http.Value = null;
    }
}
