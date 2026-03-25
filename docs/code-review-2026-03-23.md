# Code Review — SQLTool sqltool branch
**Date:** 2026-03-23
**Branch:** sqltool
**Head:** 410afe9
**Reviewer:** Claude Sonnet 4.6
**Verdict:** ❌ Not ready for production

---

## Strengths

- SQL injection fully prevented via Dapper's `DynamicParameters`
- Server-side `AllowedEnvironments` check on query execution (not just UI)
- Atomic file writes in `ConfigService` (`.tmp` + `Move`)
- Thread-safe singleton with `lock` on all state mutations
- Cookie security hardened (`HttpOnly`, `SecurePolicy.Always`, `SameSite.Strict`)
- Anti-CSRF via `@Html.AntiForgeryToken()` and `app.UseAntiforgery()`
- 56 unit tests covering service layer, validation, and export

---

## Issues

### 🔴 Critical (Must Fix)

| # | File | Issue |
|---|------|-------|
| 1 | `Pages/Login.cshtml.cs:28` | **Plaintext password comparison** — passwords stored and compared as plain strings. No hashing. Use PBKDF2 or BCrypt. |
| 2 | `Pages/Login.cshtml.cs` | **No brute-force protection** — unlimited failed login attempts. Add rate limiting or lockout before any deployment with real DBs. |
| 3 | `appsettings.Development.json` | **Weak default credentials** — `admin/admin` and `user/user` must be changed before any non-local deployment. |

### 🟡 Important (Should Fix)

| # | File | Issue |
|---|------|-------|
| 4 | `Components/Pages/Home.razor:141` | `_selectedEnv!` null-forgiving reference crashes on Blazor circuit reconnect if `SelectDatabase` fires before `SelectEnvironment`. Add null guard. |
| 5 | `Components/Pages/Admin/ManageQueries.razor` | `SaveEdit` does not validate that `Name` and `SQL` are non-empty. Blank queries appear in the user-facing list. |
| 6 | `Components/Pages/Home.razor` | `RunQuery` missing `StateHasChanged()` after the `await`. Explicit call makes render intent unambiguous. |
| 7 | `Services/ExportService.cs` | `ToExcel` / `ToXLValue` has no `byte[]` case — renders as `System.Byte[]` literal in Excel instead of hex or empty. |
| 8 | `Services/ConfigService.cs` | `Reload` does not catch `JsonException`. Corrupt `queries.json` crashes the app on startup. Add try/catch with fallback. |
| 9 | `Services/QueryEngine.cs:101` | Raw SQL Server exception messages exposed to UI — leaks schema/server names. Log server-side, return generic message to user. |

### 🔵 Minor (Nice to Have)

| # | File | Issue |
|---|------|-------|
| 10 | `Tests/Services/ConfigServiceTests.cs` | Temp directory never cleaned up. Implement `IDisposable` with `Directory.Delete`. |
| 11 | `config/queries.json` | Test query targets PROD with `SELECT *`. Ship empty or with disabled example queries. |
| 12 | `Components/Shared/ParameterForm.razor:26` | Dropdown doesn't pre-select current value on re-render — always shows `-- Select --`. Add `value` attribute. |
| 13 | `Program.cs:52` | `GET /logout` allows CSRF logout. Use POST with anti-forgery token. |
| 14 | All services | No `ILogger<T>` — no audit trail for query execution (who ran what against which environment). |

---

## Summary

| Severity | Count |
|----------|-------|
| 🔴 Critical | 3 |
| 🟡 Important | 6 |
| 🔵 Minor | 5 |
| **Total** | **14** |

The core architecture is sound and SQL injection is correctly prevented. The blockers are authentication-related: plaintext passwords and no brute-force protection are unacceptable for a tool with direct access to production databases.

---

## Previously Fixed (prior review rounds)

- `@rendermode InteractiveServer` added to interactive pages
- `decimal.TryParse` uses `CultureInfo.InvariantCulture`
- Credentials moved out of `appsettings.json` (gitignored `appsettings.Development.json`)
- Cookie security hardened
- `RemoveParam` rebuilds dropdown index map after removal
- Confirmation dialog before permanent delete
- Excel typed cell values (no longer forces strings)
- `ValidationResult` moved to `Models/`
- `EnableQuery` method added
- Boolean parameter initializes to `"false"`
- Null guards in export methods
