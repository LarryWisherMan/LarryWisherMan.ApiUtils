Below is an **updated master-plan** that folds in everything we just added
(short IDs, vault storage, typed-object conversion, helper cmdlets) and shows
how to organise namespaces and PowerShell verb-noun naming so the module scales
cleanly.

---

## 🚧 Updated Road-map (v 1.2 → v 1.4)

| Phase                                  | Deliverables                                                                                                 | ETA        |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------ | ---------- |
| **1 — Flatten bases** (done)           | `ApiCmdletBase`, `SessionCmdletBase`, `SessionInputCmdletBase`; refactored core cmdlets                      | ✓          |
| **2 — Short incremental IDs** (done)   | `SessionIdGenerator`, `Id` on `ApiSession`, `-Id` pipeline support                                           | ✓          |
| **3 — SecretStore persistence** (done) | `SecretVaultOptions`, `SecretStoreProxy`, `SessionSecret` + `SessionMetadata`, `SecretJsonSessionRepository` | ✓          |
| **4 — TTL & auto-purge**               | `ExpiresUtc`, `-TtlHours`, env `LWM_APIUTILS_SESSION_TTL_HOURS`, startup cleanup                             | **Week 1** |
| **5 — Request/Response helpers**       | `New-ApiRequestBody`, `New-ApiQueryString`, `Show-ApiCurl`, `ConvertFrom-ApiError`                           | **Week 2** |
| **6 — Typed-object conversion** (done) | `ConvertTo-ApiTypedObject`, `DynamicTypeFactory`, property-map support                                       | ✓          |
| **7 — Docs & CI polish**               | README, about\_ help, Pester suite, multi-OS GitHub Actions                                                  | **Week 3** |
| **8 — Public release v1.3**            | Gallery publish; blog / internal announcement                                                                | **Week 4** |
| **9 — Stretch v1.4**                   | AsyncCmdlet + net6.0 TFM, SQLite repository option, parallel-safe ID generator                               | Q4         |

---

## 📖 README (abridged skeleton)

````markdown
# LarryWisherMan.ApiUtils

*A secure, session-aware toolkit for calling REST APIs from PowerShell 5.1 & 7+.*

## Features
| ✅ | Description |
|----|-------------|
| Session cmdlets | `New- / Get- / Set- / Remove- / Test-ApiSession` (short IDs, TTL, vault-encrypted secrets) |
| Request cmdlets | `Invoke-ApiRestMethod` (JSON/XML parsing) & `Invoke-ApiRequest` (raw) |
| Helpers | `New-ApiRequestBody`, `New-ApiQueryString`, `Show-ApiCurl`, `ConvertTo-ApiTypedObject` |
| Security | Secrets stored in **SecretStore** vault, metadata in JSON; default TTL 24 h |

## Quick start
```powershell
Install-Module LarryWisherMan.ApiUtils -Scope CurrentUser

# 1. create & persist a session
Connect-ApiSession -Name prod -BaseUri https://api -LoginEndpoint /auth `
                   -Credential $cred -SaveToFile

# 2. reuse it anywhere
Invoke-ApiRestMethod -Session prod -Uri '/users'
````

## Configuration

| Setting                  | Env var                          | Default     |
| ------------------------ | -------------------------------- | ----------- |
| Default session TTL (h)  | `LWM_APIUTILS_SESSION_TTL_HOURS` | 24          |
| Secret vault name        | `LWM_VAULT_NAME`                 | LWMApiUtils |
| Vault unlock timeout (s) | `LWM_VAULT_TIMEOUT`              | 900         |

## Road-map

See [CHANGELOG.md](CHANGELOG.md) for planned features (typed-object mapping, SQLite backend, …).

````

*(Full README—including badges, install instructions, advanced examples—can be generated whenever you’re ready.)*

---

## 🚀 Future Feature Back-log

| Priority | Idea | Notes |
|----------|------|-------|
| 🔜 | **`Measure-ApiLatency`** & **`Test-ApiEndpoint`** | perf + availability checks |
| 🔜 | **`Renew-ApiSessionToken`** | explicit refresh flow |
| 🔜 | **Multipart upload helper** | `New-ApiMultipartFormData` |
| 🌓 | **SQLite persistence option** | single-file DB, indexed look-ups |
| 🌓 | **AsyncCmdlet + net6.0 target** | real async for PS 7+ |
| 🌓 | **Secret vault migration cmdlet** | move secrets to another vault / password |
| 🌒 | **Integrated telemetry (opt-in)** | cmdlet timing, expiry stats |

---

## 🗂 Folder & Namespace Layout

```text
src
├─ Commands
│  ├─ Core           (Invoke-*, Connect-*)
│  ├─ Session        (New-*, Get-*, Remove-*, …)
│  └─ Utilities      (New-ApiRequestBody, Show-ApiCurl, ConvertTo-ApiTypedObject)
├─ Domain
│  └─ Models         (ApiSession, SessionMetadata, SessionSecret)
├─ Infrastructure
│  ├─ Parsers        (JsonContentParser, XmlContentParser, PlainTextParser)
│  └─ Repositories   (SecretJsonSessionRepository, …)
└─ Runtime
   ├─ GlobalServices.cs
   ├─ SecretStoreProxy.cs
   ├─ SessionIdGenerator.cs
   └─ DynamicTypeFactory.cs
````

| Namespace                                               | Contains                                   |                               |                             |
| ------------------------------------------------------- | ------------------------------------------ | ----------------------------- | --------------------------- |
| \`LarryWisherMan.ApiUtils.Commands.\[Core               | Session                                    | Utilities]\`                  | Cmdlet classes (`*Command`) |
| `LarryWisherMan.ApiUtils.Domain.Models`                 | POCOs (`ApiSession`, `SessionSecret`, …)   |                               |                             |
| \`LarryWisherMan.ApiUtils.Infrastructure.\[Repositories | Parsers]\`                                 | disk/db code, content parsing |                             |
| `LarryWisherMan.ApiUtils.Runtime`                       | app-lifetime helpers (vault proxy, ID gen) |                               |                             |

---

## 📜 Verb-Noun Naming Convention

* **Verb** — follow Microsoft-approved verbs (`Invoke`, `New`, `Get`, `Set`, `Remove`, `Test`, `ConvertTo`, `ConvertFrom`, `Show`, `Measure`, `Enable`, `Disable`).
* **Noun** — start with **`Api`** to avoid collisions and keep IntelliSense grouped (`ApiSession`, `ApiRequestBody`, `ApiTypedObject`).

Examples:

| Category         | Cmdlet                                                           |
| ---------------- | ---------------------------------------------------------------- |
| **Session Mgmt** | `New-ApiSession`, `Remove-ApiSession`, `Test-ApiSession`         |
| **HTTP**         | `Invoke-ApiRestMethod`, `Invoke-ApiRequest`                      |
| **Helpers**      | `New-ApiQueryString`, `ConvertTo-ApiTypedObject`, `Show-ApiCurl` |

---

## ✅ Next Actions

1. **Merge** refactored bases & short-ID code → `feature/refactor-base` branch.
2. **Implement** TTL / auto-purge logic (Phase 4).
3. **Add** helper cmdlets (Phase 5).
4. **Write** README / about\_ help and bump version to `1.2.0-preview`.

Ping me whenever you start the next phase or need a detailed scaffold for the helper cmdlets.
