# Frontend Redesign Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the bare, unstyled SQLTool UI with a clean corporate design — compact filter bar replacing the 4-step wizard, Inter typography, blue/neutral color palette, and redesigned admin pages.

**Architecture:** CSS design tokens defined globally in `app.css`; each component gets its own `.razor.css` for scoped styles. Home.razor's `_currentStep` wizard is replaced with cascading enable/disable boolean flags. No new dependencies.

**Tech Stack:** Blazor Server (.NET 10), CSS custom properties, Inter (Google Fonts), Bootstrap (as reset only), xUnit + FluentAssertions for existing tests

**Spec:** `docs/superpowers/specs/2026-03-19-frontend-redesign-design.md`
**Mockup reference:** `docs/mockups/redesign-mockup.html`

---

## File Map

| Action | File | Responsibility |
|---|---|---|
| Modify | `SQLTool/wwwroot/app.css` | Global design tokens, Inter font, base resets |
| Modify | `SQLTool/Components/Layout/MainLayout.razor` | New nav: NavLink components, brand, user area |
| Modify | `SQLTool/Components/Layout/MainLayout.razor.css` | Nav styles (sticky bar, links, avatar) |
| Modify | `SQLTool/Components/Shared/ProdWarningBanner.razor` | Amber warning banner |
| Create | `SQLTool/Components/Shared/ProdWarningBanner.razor.css` | Banner styles |
| Modify | `SQLTool/Components/Shared/DatabaseSelector.razor` | Replace list-picker with native `<select>` |
| Modify | `SQLTool/Components/Shared/QuerySelector.razor` | Replace list-picker with native `<select>` |
| Modify | `SQLTool/Components/Shared/ParameterForm.razor` | Grid layout, red required asterisk |
| Create | `SQLTool/Components/Shared/ParameterForm.razor.css` | Param grid + input styles |
| Modify | `SQLTool/Components/Shared/ResultsTable.razor` | New results header, export buttons |
| Create | `SQLTool/Components/Shared/ResultsTable.razor.css` | Results card, table, header styles |
| Modify | `SQLTool/Components/Pages/Home.razor` | Single-page layout; replace `_currentStep` with boolean flags |
| Create | `SQLTool/Components/Pages/Home.razor.css` | Filter bar, params card styles |
| Delete | `SQLTool/Components/Shared/StepBar.razor` | No longer used |
| Modify | `SQLTool/Components/Pages/Admin/ManageQueries.razor` | Query card list + inline edit form |
| Create | `SQLTool/Components/Pages/Admin/ManageQueries.razor.css` | Admin query list styles |
| Modify | `SQLTool/Components/Pages/Admin/Environments.razor` | Env cards with tables |
| Create | `SQLTool/Components/Pages/Admin/Environments.razor.css` | Env card styles |

---

## Task 1: CSS Foundation — Design Tokens and Global Styles

**Files:**
- Modify: `SQLTool/wwwroot/app.css`

- [ ] **Step 1: Replace app.css**

Replace the entire contents of `SQLTool/wwwroot/app.css` with:

```css
/* ─── Inter font ──────────────────────────────────────────── */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

/* ─── Design tokens ───────────────────────────────────────── */
:root {
  --bg:              #F8F9FA;
  --surface:         #FFFFFF;
  --border:          #E5E7EB;
  --border-md:       #D1D5DB;
  --primary:         #2563EB;
  --primary-hover:   #1D4ED8;
  --primary-light:   #EFF6FF;
  --text:            #111827;
  --text-secondary:  #6B7280;
  --text-tertiary:   #9CA3AF;
  --warning-bg:      #FFFBEB;
  --warning-border:  #F59E0B;
  --warning-text:    #92400E;
  --danger:          #EF4444;
  --success:         #10B981;
  --shadow-sm:       0 1px 2px rgba(0,0,0,.05);
  --shadow:          0 1px 3px rgba(0,0,0,.1), 0 1px 2px rgba(0,0,0,.06);
  --shadow-md:       0 4px 6px -1px rgba(0,0,0,.1), 0 2px 4px -1px rgba(0,0,0,.06);
  --radius:          8px;
  --radius-lg:       12px;
}

/* ─── Base ────────────────────────────────────────────────── */
*, *::before, *::after { box-sizing: border-box; }

html, body {
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
  font-size: 14px;
  line-height: 1.5;
  color: var(--text);
  background: var(--bg);
  margin: 0;
}

/* ─── Page layout ─────────────────────────────────────────── */
.sqltool-main {
  min-height: calc(100vh - 56px);
}

.page-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 32px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* ─── Shared button styles ────────────────────────────────── */
.btn-primary {
  height: 36px;
  padding: 0 16px;
  background: var(--primary);
  color: #fff;
  border: none;
  border-radius: var(--radius);
  font-family: inherit;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: background 0.15s;
}
.btn-primary:hover { background: var(--primary-hover); }
.btn-primary:disabled { background: var(--text-tertiary); cursor: not-allowed; }

.btn-secondary {
  height: 36px;
  padding: 0 14px;
  background: var(--surface);
  color: var(--text-secondary);
  border: 1px solid var(--border-md);
  border-radius: var(--radius);
  font-family: inherit;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: border-color 0.15s, color 0.15s;
}
.btn-secondary:hover { border-color: var(--primary); color: var(--primary); }

.btn-sm {
  height: 30px;
  padding: 0 12px;
  background: var(--surface);
  color: var(--text-secondary);
  border: 1px solid var(--border-md);
  border-radius: 6px;
  font-family: inherit;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s;
}
.btn-sm:hover { border-color: var(--text-secondary); }
.btn-sm.danger { color: var(--danger); border-color: #FECACA; }
.btn-sm.danger:hover { background: #FEF2F2; }

/* ─── Shared form inputs ──────────────────────────────────── */
input[type="text"],
input[type="date"],
input[type="number"],
select,
textarea {
  font-family: inherit;
  font-size: 13px;
  color: var(--text);
  border: 1px solid var(--border-md);
  border-radius: var(--radius);
  background: var(--surface);
  transition: border-color 0.15s, box-shadow 0.15s;
}

input[type="text"]:focus,
input[type="date"]:focus,
input[type="number"]:focus,
select:focus,
textarea:focus {
  outline: none;
  border-color: var(--primary);
  box-shadow: 0 0 0 3px rgba(37,99,235,0.1);
}

input[type="text"],
input[type="date"],
input[type="number"] {
  height: 36px;
  padding: 0 12px;
  width: 100%;
}

select {
  height: 36px;
  padding: 0 32px 0 12px;
  width: 100%;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%239CA3AF' stroke-width='2'%3E%3Cpath d='M6 9l6 6 6-6'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 10px center;
  cursor: pointer;
}
select:disabled {
  background-color: var(--bg);
  color: var(--text-tertiary);
  cursor: not-allowed;
}

textarea {
  padding: 8px 12px;
  resize: vertical;
  width: 100%;
}

/* ─── Shared badge ────────────────────────────────────────── */
.badge {
  display: inline-flex;
  align-items: center;
  font-size: 11px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 99px;
  white-space: nowrap;
}
.badge-active   { background: #D1FAE5; color: #065F46; }
.badge-inactive { background: #F3F4F6; color: var(--text-secondary); }
.badge-warning  { background: #FEF3C7; color: #92400E; }

/* ─── Page header ─────────────────────────────────────────── */
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}
.page-title    { font-size: 20px; font-weight: 700; color: var(--text); margin: 0; }
.page-subtitle { font-size: 13px; color: var(--text-secondary); margin: 4px 0 0; }
.page-actions  { display: flex; gap: 8px; flex-shrink: 0; }

/* ─── Validation summary ──────────────────────────────────── */
.validation-errors {
  margin: 8px 0 0;
  padding: 8px 12px 8px 28px;
  background: #FEF2F2;
  border: 1px solid #FECACA;
  border-radius: var(--radius);
  color: var(--danger);
  font-size: 12px;
  list-style: disc;
}
.validation-errors li { margin: 2px 0; }

/* ─── Blazor error boundary ───────────────────────────────── */
.blazor-error-boundary {
  background: url(data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNTYiIGhlaWdodD0iNDkiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgeG1sbnM6eGxpbmsiIG92ZXJmbG93PSJoaWRkZW4iPjxkZWZzPjxjbGlwUGF0aCBpZD0iY2xpcDAiPjxyZWN0IHg9IjIzNSIgeT0iNTEiIHdpZHRoPSI1NiIgaGVpZ2h0PSI0OSIvPjwvY2xpcFBhdGg+PC9kZWZzPjxnIGNsaXAtcGF0aD0idXJsKCNjbGlwMCkiIHRyYW5zZm9ybT0idHJhbnNsYXRlKC0yMzUgLTUxKSI+PHBhdGggZD0iTTI2My41MDYgNTFDMjY0LjcxNyA1MSAyNjUuODEzIDUxLjQ4MzcgMjY2LjYwNiA1Mi4yNjU4TDI2Ny4wNTIgNTIuNzk4NyAyNjcuNTM5IDUzLjYyODMgMjkwLjE4NSA5Mi4xODMxIDI5MC41NDUgOTIuNzk1IDI5MC42NTYgOTIuOTk2QzI5MC44NzcgOTMuNTEzIDI5MSA5NC4wODE1IDI5MSA5NC42NzgyIDI5MSA5Ny4wNjUxIDI4OS4wMzggOTkgMjg2LjYxNyA5OUwyNDAuMzgzIDk5QzIzNy45NjMgOTkgMjM2IDk3LjA2NTEgMjM2IDk0LjY3ODIgMjM2IDk0LjM3OTkgMjM2LjAzMSA5NC4wODg2IDIzNi4wODkgOTMuODA3MkwyMzYuMzM4IDkzLjAxNjIgMjM2Ljg1OCA5Mi4xMzE0IDI1OS40NzMgNTMuNjI5NCAyNTkuOTYxIDUyLjc5ODUgMjYwLjQwNyA1Mi4yNjU4QzI2MS4yIDUxLjQ4MzcgMjYyLjI5NiA1MSAyNjMuNTA2IDUxWk0yNjMuNTg2IDY2LjAxODNDMjYwLjczNyA2Ni4wMTgzIDI1OS4zMTMgNjcuMTI0NSAyNTkuMzEzIDY5LjMzNyAyNTkuMzEzIDY5LjYxMDIgMjU5LjMzMiA2OS44NjA4IDI1OS4zNzEgNzAuMDg4N0wyNjEuNzk1IDg0LjAxNjEgMjY1LjM4IDg0LjAxNjEgMjY3LjgyMSA2OS43NDc1QzI2Ny44NiA2OS43MzA5IDI2Ny44NzkgNjkuNTg3NyAyNjcuODc5IDY5LjMxNzkgMjY3Ljg3OSA2Ny4xMTgyIDI2Ni40NDggNjYuMDE4MyAyNjMuNTg2IDY2LjAxODNaTTI2My41NzYgODYuMDU0N0MyNjEuMDQ5IDg2LjA1NDcgMjU5Ljc4NiA4Ny4zMDA1IDI1OS43ODYgODkuNzkyMSAyNTkuNzg2IDkyLjI4MzcgMjYxLjA0OSA5My41Mjk1IDI2My41NzYgOTMuNTI5NSAyNjYuMTE2IDkzLjUyOTUgMjY3LjM4NyA5Mi4yODM3IDI2Ny4zODcgODkuNzkyMSAyNjcuMzg3IDg3LjMwMDUgMjY2LjExNiA4Ni4wNTQ3IDI2My41NzYgODYuMDU0N1oiIGZpbGw9IiNGRkU1MDAiIGZpbGwtcnVsZT0iZXZlbm9kZCIvPjwvZz48L3N2Zz4=) no-repeat 1rem/1.8rem, #b32121;
  padding: 1rem 1rem 1rem 3.7rem;
  color: white;
}
.blazor-error-boundary::after { content: "An error has occurred." }

#blazor-error-ui {
  color-scheme: light only;
  background: lightyellow;
  bottom: 0;
  box-shadow: 0 -1px 2px rgba(0,0,0,.2);
  display: none;
  left: 0;
  padding: 0.6rem 1.25rem 0.7rem;
  position: fixed;
  width: 100%;
  z-index: 1000;
}
#blazor-error-ui .dismiss { cursor: pointer; position: absolute; right: 0.75rem; top: 0.5rem; }
```

- [ ] **Step 2: Build to verify no errors**

```bash
cd /Users/kennethreijnders/Code/SQLTool
dotnet build SQLTool/SQLTool.csproj
```
Expected: Build succeeded, 0 Error(s)

- [ ] **Step 3: Run existing tests to confirm nothing broken**

```bash
dotnet test SQLTool.Tests/SQLTool.Tests.csproj
```
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add SQLTool/wwwroot/app.css
git commit -m "style: add design tokens and global base styles to app.css"
```

---

## Task 2: Navigation — MainLayout Rewrite

**Files:**
- Modify: `SQLTool/Components/Layout/MainLayout.razor`
- Modify: `SQLTool/Components/Layout/MainLayout.razor.css`

- [ ] **Step 1: Rewrite MainLayout.razor**

Replace the entire file:

```razor
@* SQLTool/Components/Layout/MainLayout.razor *@
@inherits LayoutComponentBase
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
    <Authorized>
        <nav class="sqltool-nav">
            <a class="nav-brand" href="/">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <ellipse cx="12" cy="5" rx="9" ry="3"/>
                    <path d="M21 12c0 1.66-4.03 3-9 3S3 13.66 3 12"/>
                    <path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5"/>
                </svg>
                SQLTool
            </a>
            <div class="nav-links">
                <NavLink class="nav-link" href="/" Match="NavLinkMatch.All" ActiveClass="active">Run Query</NavLink>
                <AuthorizeView Roles="SQLTool.Admin" Context="adminCtx">
                    <NavLink class="nav-link" href="/admin/queries" ActiveClass="active">Manage Queries</NavLink>
                    <NavLink class="nav-link" href="/admin/environments" ActiveClass="active">Environments</NavLink>
                </AuthorizeView>
            </div>
            <div class="nav-user">
                <div class="nav-avatar">@GetInitials(context.User.Identity?.Name)</div>
                <span class="nav-username">@context.User.Identity?.Name</span>
                <a class="btn-signout" href="/logout">Sign out</a>
            </div>
        </nav>
        <main class="sqltool-main">
            @Body
        </main>
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>

@code {
    private static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : name[..Math.Min(2, name.Length)].ToUpper();
    }
}
```

- [ ] **Step 2: Rewrite MainLayout.razor.css**

Replace the entire file:

```css
/* SQLTool/Components/Layout/MainLayout.razor.css */
.sqltool-nav {
    background: var(--surface);
    border-bottom: 1px solid var(--border);
    height: 56px;
    display: flex;
    align-items: center;
    padding: 0 24px;
    gap: 32px;
    position: sticky;
    top: 0;
    z-index: 100;
    box-shadow: var(--shadow-sm);
}

.nav-brand {
    font-size: 16px;
    font-weight: 700;
    color: var(--primary);
    letter-spacing: -0.3px;
    display: flex;
    align-items: center;
    gap: 8px;
    text-decoration: none;
    white-space: nowrap;
}

.nav-links {
    display: flex;
    gap: 4px;
    flex: 1;
}

.nav-link {
    font-size: 13px;
    font-weight: 500;
    color: var(--text-secondary);
    text-decoration: none;
    padding: 6px 12px;
    border-radius: 6px;
    transition: background 0.15s, color 0.15s;
    white-space: nowrap;
}
.nav-link:hover { background: var(--bg); color: var(--text); }
.nav-link.active { background: var(--primary-light); color: var(--primary); }

.nav-user {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-left: auto;
}

.nav-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: var(--primary);
    color: #fff;
    font-size: 12px;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.nav-username {
    font-size: 13px;
    font-weight: 500;
    color: var(--text);
    white-space: nowrap;
}

.btn-signout {
    font-size: 12px;
    color: var(--text-tertiary);
    text-decoration: none;
    padding: 4px 8px;
    border-radius: 4px;
    transition: color 0.15s, background 0.15s;
    white-space: nowrap;
}
.btn-signout:hover { color: var(--text-secondary); background: var(--bg); }
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```
Expected: Build succeeded, 0 Error(s)

- [ ] **Step 4: Commit**

```bash
git add SQLTool/Components/Layout/MainLayout.razor SQLTool/Components/Layout/MainLayout.razor.css
git commit -m "style: rewrite nav — sticky bar, NavLink active states, avatar initials"
```

---

## Task 3: ProdWarningBanner

**Files:**
- Modify: `SQLTool/Components/Shared/ProdWarningBanner.razor`
- Create: `SQLTool/Components/Shared/ProdWarningBanner.razor.css`

- [ ] **Step 1: Rewrite ProdWarningBanner.razor**

```razor
@* SQLTool/Components/Shared/ProdWarningBanner.razor *@
@if (Show)
{
    <div class="prod-warning">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
        <span><strong>Production environment</strong> — this query runs against live data. Results are read-only.</span>
    </div>
}
@code { [Parameter] public bool Show { get; set; } }
```

- [ ] **Step 2: Create ProdWarningBanner.razor.css**

```css
/* SQLTool/Components/Shared/ProdWarningBanner.razor.css */
.prod-warning {
    background: var(--warning-bg);
    border: 1px solid var(--warning-border);
    border-radius: var(--radius);
    padding: 10px 16px;
    font-size: 13px;
    color: var(--warning-text);
    display: flex;
    align-items: center;
    gap: 10px;
}
.prod-warning svg { flex-shrink: 0; color: var(--warning-border); }
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 4: Commit**

```bash
git add SQLTool/Components/Shared/ProdWarningBanner.razor SQLTool/Components/Shared/ProdWarningBanner.razor.css
git commit -m "style: restyle ProdWarningBanner as amber alert"
```

---

## Task 4: DatabaseSelector — Replace List-Picker with Native Select

**Files:**
- Modify: `SQLTool/Components/Shared/DatabaseSelector.razor`

The current component has a search input + clickable list. Replace it with a native `<select>`. The `OnSelect` callback signature (`EventCallback<DatabaseEntry>`) stays the same; only the rendering changes.

- [ ] **Step 1: Rewrite DatabaseSelector.razor**

```razor
@* SQLTool/Components/Shared/DatabaseSelector.razor *@
@using SQLTool.Models

<select value="@SelectedId" @onchange="OnChanged" disabled="@(!Databases.Any())">
    <option value="">— Select database —</option>
    @foreach (var db in Databases)
    {
        <option value="@db.Id">@db.Label</option>
    }
</select>

@code {
    [Parameter] public List<DatabaseEntry> Databases { get; set; } = new();
    [Parameter] public string? SelectedId { get; set; }
    [Parameter] public EventCallback<DatabaseEntry> OnSelect { get; set; }

    private async Task OnChanged(ChangeEventArgs e)
    {
        var id = e.Value?.ToString();
        var db = Databases.FirstOrDefault(d => d.Id == id);
        if (db is not null)
            await OnSelect.InvokeAsync(db);
    }
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 3: Commit**

```bash
git add SQLTool/Components/Shared/DatabaseSelector.razor
git commit -m "refactor: replace DatabaseSelector list-picker with native select"
```

---

## Task 5: QuerySelector — Replace List-Picker with Native Select

**Files:**
- Modify: `SQLTool/Components/Shared/QuerySelector.razor`

Same pattern as Task 4. The `OnSelect` callback (`EventCallback<QueryDefinition>`) stays unchanged.

- [ ] **Step 1: Rewrite QuerySelector.razor**

```razor
@* SQLTool/Components/Shared/QuerySelector.razor *@
@using SQLTool.Models

<select value="@SelectedId" @onchange="OnChanged" disabled="@(!Queries.Any())">
    <option value="">— Select query —</option>
    @foreach (var q in Queries)
    {
        <option value="@q.Id">@q.Name</option>
    }
</select>

@code {
    [Parameter] public List<QueryDefinition> Queries { get; set; } = new();
    [Parameter] public string? SelectedId { get; set; }
    [Parameter] public EventCallback<QueryDefinition> OnSelect { get; set; }

    private async Task OnChanged(ChangeEventArgs e)
    {
        var id = e.Value?.ToString();
        var q = Queries.FirstOrDefault(q => q.Id == id);
        if (q is not null)
            await OnSelect.InvokeAsync(q);
    }
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 3: Commit**

```bash
git add SQLTool/Components/Shared/QuerySelector.razor
git commit -m "refactor: replace QuerySelector list-picker with native select"
```

---

## Task 6: ParameterForm — Grid Layout

**Files:**
- Modify: `SQLTool/Components/Shared/ParameterForm.razor`
- Create: `SQLTool/Components/Shared/ParameterForm.razor.css`

- [ ] **Step 1: Rewrite ParameterForm.razor**

```razor
@* SQLTool/Components/Shared/ParameterForm.razor *@
@using SQLTool.Models

<div class="param-grid">
    @foreach (var param in Parameters)
    {
        <div class="param-field">
            <label class="param-label">
                @param.Label
                @if (param.Required) { <span class="required">*</span> }
            </label>
            @switch (param.Type)
            {
                case "date":
                    <input type="date" value="@GetValue(param.Name)"
                           @onchange="e => SetValue(param.Name, e.Value?.ToString())" />
                    break;
                case "number":
                    <input type="number" value="@GetValue(param.Name)"
                           min="@param.Min" max="@param.Max"
                           @onchange="e => SetValue(param.Name, e.Value?.ToString())" />
                    break;
                case "boolean":
                    <select @onchange="e => SetValue(param.Name, e.Value?.ToString())">
                        <option value="false">No</option>
                        <option value="true">Yes</option>
                    </select>
                    break;
                case "dropdown":
                    <select @onchange="e => SetValue(param.Name, e.Value?.ToString())">
                        <option value="">— Select —</option>
                        @foreach (var opt in param.Options)
                        {
                            <option value="@opt.Value">@opt.Label</option>
                        }
                    </select>
                    break;
                default:
                    <input type="text" maxlength="500" value="@GetValue(param.Name)"
                           @onchange="e => SetValue(param.Name, e.Value?.ToString())" />
                    break;
            }
        </div>
    }
</div>

@code {
    [Parameter] public List<QueryParameter> Parameters { get; set; } = new();
    [Parameter] public List<SQLTool.Models.ParameterValue> Values { get; set; } = new();
    [Parameter] public EventCallback<List<SQLTool.Models.ParameterValue>> ValuesChanged { get; set; }

    private string? GetValue(string name) => Values.FirstOrDefault(v => v.Name == name)?.Value;

    private void SetValue(string name, string? value)
    {
        var existing = Values.FirstOrDefault(v => v.Name == name);
        if (existing is not null) existing.Value = value;
        else Values.Add(new SQLTool.Models.ParameterValue { Name = name, Value = value });
        ValuesChanged.InvokeAsync(Values);
    }
}
```

- [ ] **Step 2: Create ParameterForm.razor.css**

```css
/* SQLTool/Components/Shared/ParameterForm.razor.css */
.param-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 16px;
    align-items: end;
}

.param-field {
    display: flex;
    flex-direction: column;
    gap: 5px;
}

.param-label {
    font-size: 12px;
    font-weight: 500;
    color: var(--text-secondary);
    display: flex;
    align-items: center;
    gap: 2px;
}

.required {
    color: var(--danger);
    font-size: 11px;
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 4: Commit**

```bash
git add SQLTool/Components/Shared/ParameterForm.razor SQLTool/Components/Shared/ParameterForm.razor.css
git commit -m "style: restyle ParameterForm with grid layout and required asterisk"
```

---

## Task 7: ResultsTable — New Header and Table Styles

**Files:**
- Modify: `SQLTool/Components/Shared/ResultsTable.razor`
- Create: `SQLTool/Components/Shared/ResultsTable.razor.css`

- [ ] **Step 1: Rewrite ResultsTable.razor**

```razor
@* SQLTool/Components/Shared/ResultsTable.razor *@
@using SQLTool.Models
@inject SQLTool.Services.IExportService ExportService
@inject IJSRuntime JS

@if (Result is null) { return; }

<div class="results-card">
    <div class="results-header">
        <div class="results-meta">
            <span class="row-count">@(Result.Truncated ? "10,000+" : Result.TotalRowCount.ToString()) rows</span>
            <span class="elapsed">· @Result.ElapsedMilliseconds ms</span>
            @if (Result.Truncated)
            {
                <span class="badge badge-warning">Showing first 10,000 rows only</span>
            }
        </div>
        <div class="export-btns">
            <button class="btn-export" @onclick="ExportCsv">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/>
                </svg>
                Export CSV
            </button>
            <button class="btn-export" @onclick="ExportExcel">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/>
                </svg>
                Export Excel
            </button>
        </div>
    </div>
    <div class="table-wrap">
        <table class="results-table">
            <thead>
                <tr>@foreach (var col in Result.Columns) { <th>@col</th> }</tr>
            </thead>
            <tbody>
                @foreach (var row in Result.Rows)
                {
                    <tr>@foreach (var col in Result.Columns) { <td>@row.GetValueOrDefault(col)?.ToString()</td> }</tr>
                }
            </tbody>
        </table>
    </div>
</div>

@code {
    [Parameter] public QueryResult? Result { get; set; }
    [Parameter] public string QueryName { get; set; } = "results";

    private async Task ExportCsv()
    {
        var csv = ExportService.ToCsv(Result!);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        await JS.InvokeVoidAsync("downloadFile", $"{QueryName}.csv", "text/csv", bytes);
    }

    private async Task ExportExcel()
    {
        var bytes = ExportService.ToExcel(Result!, QueryName);
        await JS.InvokeVoidAsync("downloadFile", $"{QueryName}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }
}
```

- [ ] **Step 2: Create ResultsTable.razor.css**

```css
/* SQLTool/Components/Shared/ResultsTable.razor.css */
.results-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow-sm);
}

.results-header {
    padding: 14px 20px;
    border-bottom: 1px solid var(--border);
    display: flex;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
}

.results-meta {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 1;
    flex-wrap: wrap;
}

.row-count { font-size: 13px; font-weight: 600; color: var(--text); }
.elapsed   { font-size: 12px; color: var(--text-tertiary); }

.export-btns { display: flex; gap: 8px; }

.btn-export {
    height: 32px;
    padding: 0 14px;
    border: 1px solid var(--border-md);
    border-radius: var(--radius);
    background: var(--surface);
    font-family: inherit;
    font-size: 12px;
    font-weight: 500;
    color: var(--text-secondary);
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
    transition: border-color 0.15s, color 0.15s, background 0.15s;
}
.btn-export:hover {
    border-color: var(--primary);
    color: var(--primary);
    background: var(--primary-light);
}

.table-wrap {
    overflow-x: auto;
    max-height: 480px;
    overflow-y: auto;
}

.results-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
}

.results-table thead th {
    position: sticky;
    top: 0;
    background: var(--bg);
    border-bottom: 1px solid var(--border);
    padding: 10px 16px;
    text-align: left;
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.4px;
    color: var(--text-secondary);
    white-space: nowrap;
}

.results-table tbody tr {
    border-bottom: 1px solid var(--border);
    transition: background 0.1s;
}
.results-table tbody tr:last-child { border-bottom: none; }
.results-table tbody tr:hover { background: var(--bg); }

.results-table tbody td {
    padding: 10px 16px;
    color: var(--text);
    white-space: nowrap;
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 4: Commit**

```bash
git add SQLTool/Components/Shared/ResultsTable.razor SQLTool/Components/Shared/ResultsTable.razor.css
git commit -m "style: restyle ResultsTable — new header, sticky column headers, export buttons"
```

---

## Task 8: Home.razor — Single-Page Layout with Cascade Logic

**Files:**
- Modify: `SQLTool/Components/Pages/Home.razor`
- Create: `SQLTool/Components/Pages/Home.razor.css`

This is the largest change. The `_currentStep` wizard is replaced by boolean enable/disable flags. All four UI sections are always rendered; visibility/disabled state is controlled reactively.

- [ ] **Step 1: Rewrite Home.razor**

```razor
@* SQLTool/Components/Pages/Home.razor *@
@page "/"
@attribute [Authorize(Policy = "UserPolicy")]
@using SQLTool.Models
@using SQLTool.Services
@inject IConfigService ConfigService
@inject IQueryEngine QueryEngine

<PageTitle>SQLTool</PageTitle>

<div class="page-content">

    @* Filter Bar *@
    <div class="filter-bar">

        <div class="filter-group">
            <span class="filter-label">
                <span class="step-badge @(_selectedEnv is not null ? "done" : "")">
                    @(_selectedEnv is not null ? "✓" : "1")
                </span>
                Environment
            </span>
            <select value="@(_selectedEnv?.Name ?? "")" @onchange="OnEnvChanged">
                <option value="">— Select environment —</option>
                @foreach (var env in _environments)
                {
                    <option value="@env.Name">@env.Name</option>
                }
            </select>
        </div>

        <div class="filter-divider"></div>

        <div class="filter-group">
            <span class="filter-label">
                <span class="step-badge @(_selectedDb is not null ? "done" : "")">
                    @(_selectedDb is not null ? "✓" : "2")
                </span>
                Database
            </span>
            <select value="@(_selectedDb?.Id ?? "")" @onchange="OnDbChanged" disabled="@(_selectedEnv is null)">
                <option value="">— Select database —</option>
                @foreach (var db in _selectedEnv?.Databases ?? new())
                {
                    <option value="@db.Id">@db.Label</option>
                }
            </select>
        </div>

        <div class="filter-divider"></div>

        <div class="filter-group query-group">
            <span class="filter-label">
                <span class="step-badge @(_selectedQuery is not null ? "done" : "")">
                    @(_selectedQuery is not null ? "✓" : "3")
                </span>
                Query
            </span>
            <QuerySelector Queries="@_queries" SelectedId="@_selectedQuery?.Id"
                           OnSelect="SelectQuery" />
        </div>

        <button class="btn-run" @onclick="RunQuery"
                disabled="@(_selectedEnv is null || _selectedDb is null || _selectedQuery is null || _isRunning)">
            @if (_isRunning)
            {
                <span>Running…</span>
            }
            else
            {
                <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M5 3l14 9-14 9V3z"/></svg>
                <span>Run Query</span>
            }
        </button>

    </div>

    @* PROD Warning *@
    <ProdWarningBanner Show="@(_selectedEnv?.Name == "PROD")" />

    @* Parameters *@
    @if (_selectedQuery is not null && _selectedQuery.Parameters.Any())
    {
        <div class="params-card">
            <div class="card-label">Parameters</div>
            <ParameterForm Parameters="@_selectedQuery.Parameters" @bind-Values="_paramValues" />
            @if (_validationErrors.Any())
            {
                <ul class="validation-errors">
                    @foreach (var e in _validationErrors) { <li>@e</li> }
                </ul>
            }
        </div>
    }

    @* Results *@
    @if (_queryResult is not null)
    {
        <ResultsTable Result="_queryResult" QueryName="@(_selectedQuery?.Name ?? "results")" />
    }

</div>

@code {
    private List<EnvironmentConfig> _environments = new();
    private EnvironmentConfig? _selectedEnv;
    private DatabaseEntry? _selectedDb;
    private List<QueryDefinition> _queries = new();
    private QueryDefinition? _selectedQuery;
    private List<SQLTool.Models.ParameterValue> _paramValues = new();
    private List<string> _validationErrors = new();
    private bool _isRunning = false;
    private SQLTool.Models.QueryResult? _queryResult;

    protected override void OnInitialized()
    {
        _environments = ConfigService.GetEnvironments().ToList();
    }

    private void OnEnvChanged(ChangeEventArgs e)
    {
        var name = e.Value?.ToString();
        _selectedEnv = _environments.FirstOrDefault(env => env.Name == name);
        _selectedDb = null;
        _queries = new();
        _selectedQuery = null;
        _paramValues = new();
        _validationErrors = new();
        _queryResult = null;
    }

    private void OnDbChanged(ChangeEventArgs e)
    {
        var id = e.Value?.ToString();
        _selectedDb = _selectedEnv?.Databases.FirstOrDefault(db => db.Id == id);
        _queries = _selectedDb is not null
            ? ConfigService.GetQueries()
                .Where(q => q.AllowedEnvironments.Contains(_selectedEnv!.Name, StringComparer.OrdinalIgnoreCase))
                .ToList()
            : new();
        _selectedQuery = null;
        _paramValues = new();
        _validationErrors = new();
        _queryResult = null;
    }

    private void SelectQuery(QueryDefinition q)
    {
        _selectedQuery = q;
        _paramValues = new();
        _validationErrors = new();
        _queryResult = null;
    }

    private async Task RunQuery()
    {
        _validationErrors = new();
        var validation = QueryEngine.ValidateParameters(_selectedQuery!, _paramValues);
        if (!validation.IsValid)
        {
            _validationErrors = validation.Errors;
            return;
        }
        if (!_selectedQuery!.AllowedEnvironments.Contains(_selectedEnv!.Name, StringComparer.OrdinalIgnoreCase))
        {
            _validationErrors = new() { "This query is not allowed in the selected environment." };
            return;
        }
        _isRunning = true;
        StateHasChanged();
        _queryResult = await QueryEngine.ExecuteAsync(_selectedQuery!, _selectedDb!, _paramValues);
        _isRunning = false;
    }
}
```


- [ ] **Step 2: Create Home.razor.css**

```css
/* SQLTool/Components/Pages/Home.razor.css */
.filter-bar {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    padding: 16px 20px;
    display: flex;
    align-items: flex-end;
    gap: 12px;
    box-shadow: var(--shadow-sm);
    flex-wrap: wrap;
}

.filter-group {
    display: flex;
    flex-direction: column;
    gap: 5px;
    flex: 1;
    min-width: 140px;
}

.query-group { flex: 2; }

.filter-label {
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: var(--text-tertiary);
    display: flex;
    align-items: center;
    gap: 6px;
}

.step-badge {
    width: 16px;
    height: 16px;
    background: var(--primary);
    color: #fff;
    border-radius: 50%;
    font-size: 9px;
    font-weight: 700;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    transition: background 0.15s;
}
.step-badge.done { background: var(--success); }

.filter-divider {
    width: 1px;
    height: 38px;
    background: var(--border);
    align-self: flex-end;
    margin-bottom: 0;
    flex-shrink: 0;
}

.btn-run {
    height: 38px;
    padding: 0 20px;
    background: var(--primary);
    color: #fff;
    border: none;
    border-radius: var(--radius);
    font-family: inherit;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    white-space: nowrap;
    flex-shrink: 0;
    transition: background 0.15s, transform 0.1s;
    box-shadow: var(--shadow-sm);
    align-self: flex-end;
}
.btn-run:hover:not(:disabled) { background: var(--primary-hover); }
.btn-run:active:not(:disabled) { transform: scale(0.98); }
.btn-run:disabled { background: var(--text-tertiary); cursor: not-allowed; }

.params-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    padding: 20px;
    box-shadow: var(--shadow-sm);
}

.card-label {
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: var(--text-tertiary);
    margin-bottom: 16px;
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```
Expected: Build succeeded, 0 Error(s)

- [ ] **Step 4: Run all tests**

```bash
dotnet test SQLTool.Tests/SQLTool.Tests.csproj
```
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Components/Pages/Home.razor SQLTool/Components/Pages/Home.razor.css
git commit -m "feat: rewrite Home as single-page layout with cascading filter bar"
```

---

## Task 9: Delete StepBar

> **Depends on Task 8** — Home.razor must be rewritten first (Task 8) before deleting StepBar, otherwise the build will fail on a missing component reference.

**Files:**
- Delete: `SQLTool/Components/Shared/StepBar.razor`

- [ ] **Step 1: Delete the file**

```bash
git rm SQLTool/Components/Shared/StepBar.razor
```

- [ ] **Step 2: Build to confirm no references remain**

```bash
dotnet build SQLTool/SQLTool.csproj
```
Expected: Build succeeded. If you see a "StepBar" compile error, search for any remaining usages with `grep -r "StepBar" SQLTool/` and remove them.

- [ ] **Step 3: Commit**

```bash
git commit -m "chore: remove StepBar component (replaced by filter bar badges)"
```

---

## Task 10: ManageQueries — Query Cards and Inline Edit Form

**Files:**
- Modify: `SQLTool/Components/Pages/Admin/ManageQueries.razor`
- Create: `SQLTool/Components/Pages/Admin/ManageQueries.razor.css`

- [ ] **Step 1: Rewrite ManageQueries.razor**

```razor
@* SQLTool/Components/Pages/Admin/ManageQueries.razor *@
@page "/admin/queries"
@attribute [Authorize(Policy = "AdminPolicy")]
@using SQLTool.Models
@using SQLTool.Services
@inject IConfigService ConfigService

<div class="page-content">
    <div class="page-header">
        <div>
            <h1 class="page-title">Manage Queries</h1>
            <p class="page-subtitle">queries.json · @_allQueries.Count queries</p>
        </div>
        <div class="page-actions">
            <button class="btn-secondary" @onclick="ReloadConfig">↺ Reload Config</button>
            <button class="btn-primary" @onclick="AddNew">+ Add Query</button>
        </div>
    </div>

    <div class="query-list">
        @foreach (var q in _allQueries)
        {
            <div class="query-item @(!q.Enabled ? "disabled" : "")">
                <div class="query-info">
                    <div class="query-name">@q.Name</div>
                    <div class="query-meta">@q.Parameters.Count param(s) · @string.Join(", ", q.AllowedEnvironments)</div>
                </div>
                <span class="badge @(q.Enabled ? "badge-active" : "badge-inactive")">
                    @(q.Enabled ? "Active" : "Disabled")
                </span>
                <div class="query-actions">
                    <button class="btn-sm" @onclick="() => StartEdit(q)">✏ Edit</button>
                    @if (q.Enabled)
                    {
                        <button class="btn-sm danger" @onclick="() => SoftDelete(q.Id)">Disable</button>
                    }
                    else
                    {
                        <button class="btn-sm" @onclick="() => Enable(q.Id)">Enable</button>
                    }
                    <button class="btn-sm danger" @onclick="() => HardDelete(q.Id)">Remove</button>
                </div>
            </div>

            @if (_editingId == q.Id)
            {
                <div class="edit-form">
                    <div class="form-row">
                        <label>Display Name</label>
                        <input type="text" @bind="_draft!.Name" />
                    </div>
                    <div class="form-row">
                        <label>Description</label>
                        <input type="text" @bind="_draft!.Description" />
                    </div>
                    <div class="form-row">
                        <label>Allowed Environments</label>
                        <div class="env-checks">
                            @foreach (var env in _allEnvNames)
                            {
                                <label class="check-label">
                                    <input type="checkbox"
                                           checked="@_draft!.AllowedEnvironments.Contains(env)"
                                           @onchange="e => ToggleEnv(env, (bool)(e.Value ?? false))" />
                                    @env
                                </label>
                            }
                        </div>
                    </div>
                    <div class="form-row">
                        <label>SQL</label>
                        <textarea @bind="_draft!.Sql" rows="5" class="sql-editor"></textarea>
                        <small class="form-hint">Use @@ParameterName for parameters</small>
                    </div>
                    <div class="params-editor">
                        <div class="params-editor-header">
                            <span>Parameters</span>
                            <button class="btn-sm" @onclick="AddParam">+ Add</button>
                        </div>
                        @for (var i = 0; i < _draft!.Parameters.Count; i++)
                        {
                            var idx = i;
                            var p = _draft.Parameters[idx];
                            <div class="param-row">
                                <input type="text" placeholder="SQL Name (e.g. StartDate)" @bind="p.Name" />
                                <input type="text" placeholder="Label (shown to user)" @bind="p.Label" />
                                <select @bind="p.Type">
                                    <option value="text">Text</option>
                                    <option value="date">Date</option>
                                    <option value="number">Number</option>
                                    <option value="boolean">Yes/No</option>
                                    <option value="dropdown">Dropdown</option>
                                </select>
                                <label class="check-label">
                                    <input type="checkbox" @bind="p.Required" /> Required
                                </label>
                                <button class="btn-sm danger" @onclick="() => RemoveParam(idx)">🗑</button>
                                @if (p.Type == "dropdown")
                                {
                                    <div class="dropdown-opts">
                                        <small>Options (value:label, one per line)</small>
                                        <textarea rows="3"
                                                  value="@_dropdownOptionsText.GetValueOrDefault(idx, "")"
                                                  @onchange="e => { _dropdownOptionsText[idx] = e.Value?.ToString() ?? string.Empty; SyncDropdownOptions(idx); }"></textarea>
                                    </div>
                                }
                            </div>
                        }
                    </div>
                    <div class="form-actions">
                        <button class="btn-secondary" @onclick="CancelEdit">Cancel</button>
                        <button class="btn-primary" @onclick="SaveEdit">💾 Save to config</button>
                    </div>
                </div>
            }
        }
    </div>
</div>

@code {
    private List<QueryDefinition> _allQueries = new();
    private List<string> _allEnvNames = new();
    private string? _editingId;
    private QueryDefinition? _draft;
    private Dictionary<int, string> _dropdownOptionsText = new();

    protected override void OnInitialized() => LoadData();

    private void LoadData()
    {
        _allQueries = ConfigService.GetAllQueries().ToList();
        _allEnvNames = ConfigService.GetEnvironments().Select(e => e.Name).ToList();
    }

    private void ReloadConfig() { ConfigService.Reload(); LoadData(); }
    private void AddNew() => StartEdit(new QueryDefinition { Id = Guid.NewGuid().ToString("N")[..8] });

    private void StartEdit(QueryDefinition q)
    {
        _draft = new QueryDefinition
        {
            Id = q.Id, Name = q.Name, Description = q.Description,
            AllowedEnvironments = new(q.AllowedEnvironments),
            Sql = q.Sql, Enabled = q.Enabled,
            Parameters = q.Parameters.Select(p => new QueryParameter
            {
                Name = p.Name, Label = p.Label, Type = p.Type,
                Required = p.Required, Min = p.Min, Max = p.Max,
                Options = new(p.Options)
            }).ToList()
        };
        _dropdownOptionsText = _draft.Parameters
            .Select((p, i) => (i, string.Join("\n", p.Options.Select(o => $"{o.Value}:{o.Label}"))))
            .ToDictionary(x => x.i, x => x.Item2);
        _editingId = q.Id;
    }

    private void CancelEdit() { _editingId = null; _draft = null; }

    private void SaveEdit()
    {
        for (var i = 0; i < _draft!.Parameters.Count; i++)
            if (_draft.Parameters[i].Type == "dropdown") SyncDropdownOptions(i);
        ConfigService.SaveQuery(_draft!);
        CancelEdit();
        LoadData();
    }

    private void SoftDelete(string id) { ConfigService.DeleteQuery(id, permanent: false); LoadData(); }
    private void Enable(string id)
    {
        var q = _allQueries.FirstOrDefault(q => q.Id == id);
        if (q is null) return;
        q.Enabled = true;
        ConfigService.SaveQuery(q);
        LoadData();
    }
    private void HardDelete(string id) { ConfigService.DeleteQuery(id, permanent: true); LoadData(); }
    private void AddParam() { _draft!.Parameters.Add(new()); _dropdownOptionsText[_draft.Parameters.Count - 1] = ""; }
    private void RemoveParam(int idx) { _draft!.Parameters.RemoveAt(idx); }

    private void ToggleEnv(string env, bool add)
    {
        if (add && !_draft!.AllowedEnvironments.Contains(env)) _draft.AllowedEnvironments.Add(env);
        else if (!add) _draft!.AllowedEnvironments.Remove(env);
    }

    private void SyncDropdownOptions(int idx)
    {
        var lines = _dropdownOptionsText.GetValueOrDefault(idx, "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        _draft!.Parameters[idx].Options = lines
            .Select(l => l.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new DropdownOption { Value = parts[0].Trim(), Label = parts[1].Trim() })
            .ToList();
    }
}
```

> **Note about Enable():** The current `ConfigService` has a `SaveQuery` method that upserts by ID. Enabling a disabled query by setting `q.Enabled = true` and calling `SaveQuery` is the correct pattern, consistent with how the edit form saves. Verify `ConfigService.SaveQuery` handles this by checking `SQLTool/Services/ConfigService.cs` — if `SaveQuery` only updates existing entries, it will work. If it requires a draft copy, adapt `Enable()` to clone the query first.

- [ ] **Step 2: Create ManageQueries.razor.css**

```css
/* SQLTool/Components/Pages/Admin/ManageQueries.razor.css */
.query-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.query-item {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    padding: 14px 18px;
    display: flex;
    align-items: center;
    gap: 16px;
    box-shadow: var(--shadow-sm);
    flex-wrap: wrap;
}
.query-item.disabled { opacity: 0.55; }

.query-info { flex: 1; min-width: 0; }
.query-name { font-size: 14px; font-weight: 600; color: var(--text); }
.query-meta { font-size: 12px; color: var(--text-tertiary); margin-top: 2px; }

.query-actions { display: flex; gap: 8px; flex-shrink: 0; }

/* Inline edit form */
.edit-form {
    background: var(--surface);
    border: 1px solid var(--border);
    border-top: none;
    border-radius: 0 0 var(--radius-lg) var(--radius-lg);
    padding: 20px 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    margin-top: -8px;
}

.form-row {
    display: flex;
    flex-direction: column;
    gap: 5px;
}
.form-row > label {
    font-size: 12px;
    font-weight: 500;
    color: var(--text-secondary);
}
.form-hint {
    font-size: 11px;
    color: var(--text-tertiary);
    margin-top: 2px;
}

.sql-editor { font-family: monospace; font-size: 13px; }

.env-checks {
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
}

.check-label {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    color: var(--text);
    cursor: pointer;
}

.params-editor {
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.params-editor-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    font-size: 12px;
    font-weight: 600;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.4px;
}

.param-row {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
}
.param-row input[type="text"] { flex: 1; min-width: 120px; }
.param-row select { width: 120px; flex-shrink: 0; }

.dropdown-opts {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding-left: 4px;
}
.dropdown-opts small { font-size: 11px; color: var(--text-tertiary); }

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    padding-top: 4px;
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```
Expected: Build succeeded. If `Enable()` causes a compile issue, check `IConfigService` and adapt.

- [ ] **Step 4: Run tests**

```bash
dotnet test SQLTool.Tests/SQLTool.Tests.csproj
```

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Components/Pages/Admin/ManageQueries.razor SQLTool/Components/Pages/Admin/ManageQueries.razor.css
git commit -m "style: restyle ManageQueries — query cards, inline edit form, Enable button"
```

---

## Task 11: Environments — Env Cards

**Files:**
- Modify: `SQLTool/Components/Pages/Admin/Environments.razor`
- Create: `SQLTool/Components/Pages/Admin/Environments.razor.css`

- [ ] **Step 1: Rewrite Environments.razor**

```razor
@* SQLTool/Components/Pages/Admin/Environments.razor *@
@page "/admin/environments"
@attribute [Authorize(Policy = "AdminPolicy")]
@using SQLTool.Services
@inject IConfigService ConfigService

<div class="page-content">
    <div class="page-header">
        <div>
            <h1 class="page-title">Environments</h1>
            <p class="page-subtitle">Connection strings are resolved from environment variables or Azure Key Vault — not stored here.</p>
        </div>
    </div>

    <div class="env-list">
        @foreach (var env in ConfigService.GetEnvironments())
        {
            <div class="env-card">
                <div class="env-card-header">
                    <span class="env-name">@env.Name</span>
                    <span class="env-db-count">@env.Databases.Count database(s)</span>
                </div>
                <div class="env-table-wrap">
                    <table class="env-table">
                        <thead>
                            <tr>
                                <th>Label</th>
                                <th>Connection String Key</th>
                                <th>Timeout</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var db in env.Databases)
                            {
                                <tr>
                                    <td>@db.Label</td>
                                    <td><code class="conn-key">@db.ConnectionStringKey</code></td>
                                    <td>@db.QueryTimeoutSeconds s</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        }
    </div>
</div>
```

- [ ] **Step 2: Create Environments.razor.css**

```css
/* SQLTool/Components/Pages/Admin/Environments.razor.css */
.env-list {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.env-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow-sm);
}

.env-card-header {
    padding: 14px 20px;
    border-bottom: 1px solid var(--border);
    display: flex;
    align-items: center;
    gap: 10px;
    background: var(--bg);
}

.env-name {
    font-size: 14px;
    font-weight: 700;
    color: var(--text);
}

.env-db-count {
    font-size: 12px;
    color: var(--text-tertiary);
}

.env-table-wrap { overflow-x: auto; }

.env-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
}
.env-table thead th {
    padding: 10px 20px;
    text-align: left;
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.4px;
    color: var(--text-secondary);
    border-bottom: 1px solid var(--border);
    white-space: nowrap;
}
.env-table tbody tr {
    border-bottom: 1px solid var(--border);
    transition: background 0.1s;
}
.env-table tbody tr:last-child { border-bottom: none; }
.env-table tbody tr:hover { background: var(--bg); }
.env-table tbody td { padding: 10px 20px; color: var(--text); }

.conn-key {
    font-size: 12px;
    background: var(--bg);
    padding: 2px 6px;
    border-radius: 4px;
    font-family: monospace;
    color: var(--text-secondary);
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 4: Run all tests**

```bash
dotnet test SQLTool.Tests/SQLTool.Tests.csproj
```
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Components/Pages/Admin/Environments.razor SQLTool/Components/Pages/Admin/Environments.razor.css
git commit -m "style: restyle Environments page with env cards and table"
```

---

## Final Verification

- [ ] **Run the app and visually verify against the mockup**

```bash
cd /Users/kennethreijnders/Code/SQLTool
dotnet run --project SQLTool/SQLTool.csproj
```

Open `https://localhost:5001` (or whatever port is shown). Compare against `docs/mockups/redesign-mockup.html`.

Check each page:
1. Nav: brand, links, avatar initials, active state highlight
2. Run Query: filter bar cascade (env → db → query), PROD warning appears on env select, params card, results table
3. Manage Queries: query cards, badges, Enable button neutral, Disable/Remove red, inline edit form with monospace SQL
4. Environments: env cards with tables, monospace connection key

- [ ] **Run all tests one final time**

```bash
dotnet test SQLTool.Tests/SQLTool.Tests.csproj
```

- [ ] **Final commit if any minor fixes were made during verification**

```bash
git add -p
git commit -m "style: fix visual polish after final verification"
```
