# SQLTool — Design Specification
**Date:** 2026-03-17
**Stack:** ASP.NET Core + Blazor Server · Microsoft SQL Server · Microsoft Entra ID (Azure AD)

---

## 1. Purpose

A secure internal web app that lets non-technical team members run pre-defined SQL queries against multiple environments and databases. Users pick an environment, select a database, choose a query, fill in parameters, and view or export results — without ever writing SQL.

---

## 2. Users & Roles

| Role | Capabilities |
|------|-------------|
| **User** | Select environment/database, pick and run queries, view results, export CSV/Excel |
| **Admin** | Everything a User can do + manage queries (add/edit/delete), manage environments, reload config |

Roles are assigned via **Microsoft Entra ID (Azure AD) app roles** — no separate user management UI needed.

---

## 3. Environments & Databases

Three fixed tiers:

| Environment | Databases |
|-------------|-----------|
| DEV | 1 database |
| TEST | 1 database |
| PROD | 45–50 databases (searchable list) |

Each database is a named connection string stored in `environments.json`. The PROD database list is searchable by name since it can contain 45–50 entries.

---

## 4. Query Configuration

Queries are defined in `queries.json` and loaded at startup (with live reload supported). The Admin UI reads from and writes back to this file.

### queries.json structure

```json
{
  "queries": [
    {
      "id": "orders-by-date",
      "name": "Get orders by date range",
      "description": "Returns all orders placed between two dates",
      "allowedEnvironments": ["PROD", "TEST"],
      "sql": "SELECT OrderID, CustomerName, OrderDate, TotalAmount FROM Orders WHERE OrderDate BETWEEN @StartDate AND @EndDate",
      "enabled": true,
      "parameters": [
        {
          "name": "StartDate",
          "label": "Start date",
          "type": "date",
          "required": true
        },
        {
          "name": "EndDate",
          "label": "End date",
          "type": "date",
          "required": true
        }
      ]
    }
  ]
}
```

### Parameter input types & validation

| Type | UI control | Validation |
|------|-----------|-----------|
| `date` | Date picker | ISO 8601 format (yyyy-MM-dd); required check if `required: true` |
| `number` | Numeric input | Decimal allowed; optional `min` / `max` in config; no empty if required |
| `text` | Text input | Max 500 characters; no empty if required |
| `boolean` | Yes/No toggle | Always has a value (defaults to `false`) |
| `dropdown` | Select list | Value must match one of the configured `options`; validated server-side before execution |

**Dropdown parameter schema:**

```json
{
  "name": "Status",
  "label": "Status",
  "type": "dropdown",
  "required": true,
  "options": [
    { "value": "active", "label": "Active" },
    { "value": "inactive", "label": "Inactive" }
  ]
}
```

**Number parameter with constraints:**

```json
{
  "name": "MaxResults",
  "label": "Max results",
  "type": "number",
  "required": false,
  "min": 1,
  "max": 1000
}
```

### environments.json structure

```json
{
  "environments": [
    {
      "name": "PROD",
      "databases": [
        { "id": "customerdb-be", "label": "CustomerDB_BE", "connectionStringKey": "PROD_CustomerDB_BE" },
        { "id": "customerdb-nl", "label": "CustomerDB_NL", "connectionStringKey": "PROD_CustomerDB_NL" }
      ]
    },
    {
      "name": "TEST",
      "databases": [
        { "id": "test-db", "label": "TestDB", "connectionStringKey": "TEST_TestDB" }
      ]
    },
    {
      "name": "DEV",
      "databases": [
        { "id": "dev-db", "label": "DevDB", "connectionStringKey": "DEV_DevDB" }
      ]
    }
  ]
}
```

**Secrets management:** `environments.json` stores only a `connectionStringKey` name — not the actual connection string. Actual connection strings are resolved at runtime from one of:
1. ASP.NET Core `ConnectionStrings` section in `appsettings.json` (local dev, secrets.json)
2. Environment variables (e.g. `ConnectionStrings__PROD_CustomerDB_BE`) — recommended for server deployments
3. Azure Key Vault (referenced via `appsettings.json` Key Vault provider) — recommended for production

Connection strings are **never stored in `environments.json`** and never sent to the browser.

---

## 5. Architecture

```
Browser (HTML rendered server-side, kept in sync via SignalR)
        ↕
ASP.NET Core / Blazor Server
  ├── Auth Middleware        → Azure AD / Microsoft Entra ID (MSAL)
  ├── Query Engine           → Executes parameterized SQL (read-only, no dynamic SQL)
  ├── Config Service         → Loads & hot-reloads queries.json / environments.json
  └── Export Service         → Generates CSV / Excel (ClosedXML) on demand
        ↕
Microsoft SQL Server (DEV / TEST / PROD × 45-50 DBs)
        ↕
Microsoft Entra ID           → Login · Role assignment (Admin/User) · Token validation
```

**Key security principles:**
- All queries use **parameterized SQL only** — no string concatenation, no dynamic SQL injection risk
- Queries are **read-only by design** — connection strings use a read-only DB user
- Users can only run queries allowed for the selected environment
- Connection strings never exposed to the browser

---

## 6. UI Flow

### User flow (4 steps)

```
① Select Environment  →  ② Select Database  →  ③ Select Query + Fill Parameters  →  ④ View Results
```

- A **step progress bar** at the top shows current position; completed steps are clickable to go back
- At step ①: three environment buttons (PROD / TEST / DEV)
- At step ②: searchable list of databases for the selected environment
- At step ③: searchable query list on the left, parameter form on the right; a **PROD warning banner** is shown when running against production
- At step ④: results table with row count and execution time; **Export CSV** and **Export Excel** buttons (explicit click required, no auto-export)

### Admin flow

Accessible via the top nav (Admin role only):

- **Manage Queries** — list of all queries with status (Active/Disabled); inline edit form with:
  - Display name, description, allowed environments
  - SQL editor with `@ParameterName` syntax
  - Parameter editor: add/remove parameters, set SQL name, user label, input type, required flag, and dropdown options
  - "Save to config" writes back to `queries.json` (atomic write)
  - **Delete behavior:** deleting a query sets `enabled: false` (soft delete) rather than removing it from the file, preserving history. A separate "Remove permanently" action does a hard delete.
  - "Reload Config" hot-reloads `queries.json` **and** `environments.json` from disk (admin-triggered only; file-system watching via `IOptionsMonitor` is **disabled** to prevent unintended PROD config changes). In-flight query executions at the time of reload complete against the previous config.
- **Environments** — view configured environments and databases (connection strings masked, showing only the key name)

---

## 7. Authentication

- **Microsoft Entra ID** via OpenID Connect (MSAL / Microsoft.Identity.Web)
- Users log in with their company Microsoft account — no separate passwords
- App roles defined in Entra ID:
  - `SQLTool.User` → regular access
  - `SQLTool.Admin` → admin access
- Unauthenticated requests redirect to Microsoft login

---

## 8. Safety & Security

| Concern | Mitigation |
|---------|-----------|
| SQL injection | Parameterized queries only, no dynamic SQL |
| Accidental PROD writes | Read-only DB user per environment |
| Unauthorized access | Microsoft Entra ID auth required |
| Sensitive config exposure | Connection strings resolved server-side from env vars / Key Vault; never in `environments.json`; never sent to browser |
| Running wrong environment | PROD warning banner; environment clearly shown in step bar throughout |
| Runaway queries | Query timeout: 30s (configurable per environment in `environments.json`) |
| `allowedEnvironments` bypass | Enforced **server-side** before execution — the backend checks the query's `allowedEnvironments` against the requested environment; UI filtering alone is not sufficient |
| Dropdown value tampering | Server-side validation: submitted dropdown value must exist in the query's configured `options` list |
| Large result sets | Hard cap of **10,000 rows** per query execution. If the result exceeds 10,000 rows, execution is truncated and a warning banner is shown. Export generates from the already-fetched in-memory result (no re-execution) |
| User with no assigned role | Authenticated users with neither `SQLTool.User` nor `SQLTool.Admin` role are redirected to a dedicated "Access Denied" page with instructions to contact their administrator |

---

## 9. Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor Server (C#) |
| Backend | ASP.NET Core 8 |
| Auth | Microsoft.Identity.Web + MSAL |
| Database | Microsoft SQL Server (via Dapper or EF Core) |
| Export | ClosedXML (Excel) + CsvHelper (CSV) |
| Config | JSON files (queries.json, environments.json) with IOptionsMonitor hot-reload |
| Hosting | Any IIS / Azure App Service / Docker |

---

## 10. Out of Scope

- Free-text SQL editor for users
- Query scheduling / automation
- User management UI (handled via Entra ID)
- Audit logging (can be added later)
- Query result pagination beyond a reasonable row limit
