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

### Parameter input types

| Type | UI control |
|------|-----------|
| `date` | Date picker |
| `number` | Numeric input |
| `text` | Text input |
| `boolean` | Yes/No toggle |
| `dropdown` | Select list (options defined in config) |

### environments.json structure

```json
{
  "environments": [
    {
      "name": "PROD",
      "databases": [
        { "id": "customerdb-be", "label": "CustomerDB_BE", "connectionString": "Server=...;Database=CustomerDB_BE;..." },
        { "id": "customerdb-nl", "label": "CustomerDB_NL", "connectionString": "..." }
      ]
    },
    {
      "name": "TEST",
      "databases": [
        { "id": "test-db", "label": "TestDB", "connectionString": "..." }
      ]
    },
    {
      "name": "DEV",
      "databases": [
        { "id": "dev-db", "label": "DevDB", "connectionString": "..." }
      ]
    }
  ]
}
```

---

## 5. Architecture

```
Browser (Blazor WASM via SignalR)
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
  - Parameter editor: add/remove parameters, set SQL name, user label, input type, required flag
  - "Save to config" writes back to `queries.json`
  - "Reload Config" hot-reloads without restart
- **Environments** — view configured environments and databases (connection strings masked)

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
| Sensitive config exposure | Connection strings in server-side config only, never sent to browser |
| Running wrong environment | PROD warning banner; environment clearly shown in step bar throughout |
| Runaway queries | Query timeout configured per environment (default: 30s) |

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
