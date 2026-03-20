# SQLTool Frontend Redesign — Design Spec

**Date:** 2026-03-19
**Status:** Approved

---

## Context

SQLTool is a Blazor Server app (.NET 10) used primarily by **business users** (analysts, ops, finance) to run pre-built SQL queries against multiple environments. The current UI is functional but unstyled — no design system, no visual hierarchy, bare custom CSS. This spec defines a full frontend redesign.

---

## Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Primary audience | Business users | Non-technical users running pre-built queries |
| Visual style | Clean & corporate | Approachable, professional, not a dev-tool aesthetic |
| Layout model | Single page (not wizard) | Faster for repeat users; all controls visible at once |
| Color palette | Blue/neutral (custom) | No existing brand guidelines; chosen for clarity |
| Typography | Inter | Standard for modern enterprise internal tools |

---

## Visual Language

### Color Palette
| Token | Value | Usage |
|---|---|---|
| `--bg` | `#F8F9FA` | Page background |
| `--surface` | `#FFFFFF` | Cards, nav, panels |
| `--border` | `#E5E7EB` | Default borders |
| `--border-md` | `#D1D5DB` | Input borders |
| `--primary` | `#2563EB` | Buttons, active states, brand |
| `--primary-hover` | `#1D4ED8` | Primary hover |
| `--primary-light` | `#EFF6FF` | Active nav bg, focus rings |
| `--text` | `#111827` | Body text |
| `--text-secondary` | `#6B7280` | Labels, secondary info |
| `--text-tertiary` | `#9CA3AF` | Hints, placeholders |
| `--warning-bg` | `#FFFBEB` | PROD warning background |
| `--warning-border` | `#F59E0B` | PROD warning border |
| `--warning-text` | `#92400E` | PROD warning text |
| `--danger` | `#EF4444` | Destructive actions only (Disable, Remove) |
| `--success` | `#10B981` | Completed step indicators |

### Shadow Tokens
| Token | Value | Usage |
|---|---|---|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,.05)` | Nav, filter bar, cards |
| `--shadow` | `0 1px 3px rgba(0,0,0,.1), 0 1px 2px rgba(0,0,0,.06)` | Elevated cards |
| `--shadow-md` | `0 4px 6px -1px rgba(0,0,0,.1), 0 2px 4px -1px rgba(0,0,0,.06)` | Modals (reserved) |

### Typography
- **Font family:** Inter (Google Fonts) with system-ui fallback. The existing `font-family` declaration in `app.css` must be replaced (not appended) with the Inter stack.
- **Body:** 14px / 1.5 line-height
- **Labels/metadata:** 11–12px, uppercase, letter-spacing 0.4–0.5px
- **Headings:** 20px, font-weight 700
- **Nav brand:** 16px, font-weight 700

### Spacing & Radius
- **Page max-width:** 1200px, centered, 24px horizontal padding
- **Page padding-top:** 32px
- **Card radius:** 12px (`--radius-lg`)
- **Input/button radius:** 8px (`--radius`)
- **Card gap:** 16px

---

## Layout

### Navigation
A sticky top bar (56px height) containing:
- **Brand mark** — database SVG icon + "SQLTool" in primary blue
- **Nav links** — Run Query, Manage Queries (admin only), Environments (admin only)
  - Use Blazor `<NavLink>` components with `ActiveClass="active"` for active-state detection
  - Active link: primary blue text + light blue background pill
  - Inactive link: secondary gray, hover darkens
- **User area** (right-aligned) — avatar initials circle, display name, Sign out link

### Run Query Page

The page has no page title — the filter bar is the primary UI element.

**1. Filter Bar** (`filter-bar`)
A white card with a single horizontal row:
```
[ ① Environment ▾ ] — [ ② Database ▾ ] — [ ③ Query ▾ (wider) ]  [ ▶ Run Query ]
```
- Three native `<select>` dropdowns separated by 1px vertical dividers
- Each dropdown has a numbered step badge above it; the badge turns to a green checkmark once that dropdown has a selection
- When a user changes an upstream dropdown, all downstream badges revert to their numbered state (reflecting that downstream selections have been reset)
- Labels: 11px uppercase, gray
- Query dropdown is `flex: 2` (wider than the others)
- Run button: primary blue, right-aligned
  - Disabled until all three dropdowns have a value
  - While the query is running: disabled + text changes to "Running…" with a spinner or ellipsis
  - Returns to "▶ Run Query" on completion or error

**Cascading enable/disable behavior (replaces the existing `_currentStep` wizard):**
- On initial load: Environment dropdown enabled; Database and Query dropdowns disabled
- After an environment is selected: Database dropdown becomes enabled; Query resets + stays disabled
- After a database is selected: Query dropdown becomes enabled; it is populated with queries allowed for the selected environment
- After a query is selected: parameters card appears (if the query has parameters); Run button becomes enabled
- Changing Environment resets Database, Query, parameters, and results; step badges revert
- Changing Database resets Query, parameters, and results; step badges revert from step ② onward
- The existing `_currentStep` / `GoToStep` pattern is removed. The C# code block in `Home.razor` is reworked to use boolean enable/disable flags instead of a step counter. The `SelectEnvironment`, `SelectDatabase`, `SelectQuery`, and `RunQuery` methods are kept but adapted — the cascading reset logic is equivalent, just no longer gated behind `_currentStep`.

**2. PROD Warning Banner** (conditional)
Shown as soon as PROD is selected in the Environment dropdown — before database or query are chosen. It is reactive to the Environment dropdown alone, not gated on any further selection. Amber background, amber border, warning icon. Text: "Production environment — this query runs against live data. Results are read-only."

**3. Parameters Card** (`params-card`)
Shown only when a query is selected and it has parameters.
- Section label: "PARAMETERS" in small uppercase gray
- Auto-grid layout: `repeat(auto-fill, minmax(200px, 1fr))`
- Each parameter: label (with red asterisk if required) + appropriate input (text, date, number, select)
- Validation errors are shown as a styled `<ul>` summary list below the parameter grid (same position as today, re-styled). Per-field inline errors are not used — the existing flat `List<string>` error model in `Home.razor` is preserved unchanged.

**4. Results Card** (`results-card`)
Shown after query executes.
- **Header row:** row count (bold) + elapsed time (gray) + Export CSV / Export Excel buttons (right-aligned)
- **Truncation badge:** when results are truncated, the row count displays as "10,000+" and an amber pill badge ("Showing first 10,000 rows only") is rendered inline after the elapsed time, within the header row
- **Table:** sticky column headers (uppercase, small, gray), alternating row hover state, horizontally scrollable, max-height 480px with vertical scroll

### Manage Queries Page (Admin)

- Page title "Manage Queries" + subtitle showing `queries.json · N queries`
- Action buttons top-right: "↺ Reload Config" (secondary) + "+ Add Query" (primary)
- **Query list:** each query is a card row containing:
  - Name (bold) + metadata line (param count · allowed environments)
  - Active/Disabled badge (green or gray pill)
  - Action buttons (small, right-aligned):
    - **Active queries:** Edit (neutral), Disable (danger), Remove (danger)
    - **Disabled queries:** Edit (neutral), Enable (neutral/secondary — not danger), Remove (danger)
  - Disabled queries: full row at reduced opacity (0.55)

**Inline Edit Form**
Expands directly below the query row (no modal), same card background and border. Layout:
- **Display Name** — full-width text input
- **Description** — full-width text input
- **Allowed Environments** — inline checkboxes in a horizontal row
- **SQL** — full-width `<textarea>` (5 rows), monospace font (`font-family: monospace`), with a helper note "Use @ParameterName for parameters"
- **Parameters sub-editor** — full-width section with a small heading "Parameters" + "+ Add" button; each parameter row contains: SQL Name input, Label input, Type `<select>`, Required checkbox; if Type = dropdown, a sub-textarea for options (value:label, one per line) expands below; a trash icon removes the row
- **Form actions** — right-aligned: Cancel (secondary), Save to config (primary)

### Environments Page (Admin)

- Page title "Environments" + hint about connection string resolution
- Each environment: a card with header (env name + db count) and a standard table (Label, Connection String Key, Timeout)
- Connection string keys displayed as `<code>` with subtle background (`font-size: 12px`, `background: var(--bg)`, `padding: 2px 6px`, `border-radius: 4px`)

---

## Component Inventory

| Component | File | Change |
|---|---|---|
| `MainLayout` | `MainLayout.razor` + `.razor.css` | Full rewrite — new nav markup using `<NavLink>`, CSS variables |
| `StepBar` | `StepBar.razor` | Removed — replaced by numbered badges in the filter bar |
| `Home` | `Home.razor` | Rewrite — single-page layout; `_currentStep`/`GoToStep` replaced with cascading enable/disable flags |
| `ResultsTable` | `ResultsTable.razor` | Restyle — new header, table, export buttons |
| `ManageQueries` | `ManageQueries.razor` | Restyle — query cards, inline edit form with monospace SQL textarea |
| `Environments` | `Environments.razor` | Restyle — env cards with tables |
| `app.css` | `wwwroot/app.css` | Add CSS custom properties (design tokens), global resets, Inter font import; replace existing font-family declaration |
| `DatabaseSelector` | `DatabaseSelector.razor` | Replace list-picker with a native `<select>` element; existing search-input behavior is removed |
| `QuerySelector` | `QuerySelector.razor` | Replace list-picker with a native `<select>` element; existing search-input behavior is removed |
| `ParameterForm` | `ParameterForm.razor` | Restyle — grid layout, styled validation summary list |
| `ProdWarningBanner` | `ProdWarningBanner.razor` | Restyle — amber banner |
| `RedirectToLogin` | `RedirectToLogin.razor` | No change |
| `AccessDenied` | `AccessDenied.razor` | No change |
| `blazor-error-ui` | `MainLayout.razor` (inline div) | No change — keep default Blazor error banner as-is |

---

## CSS Architecture

- **Design tokens** defined as CSS custom properties on `:root` in `app.css`
- **Scoped styles** stay in `.razor.css` files per component
- **No new CSS framework** — Bootstrap is already included but barely used; keep it as a reset/utility base only, do not add Tailwind or other frameworks
- **No JavaScript** beyond the existing `downloadFile` interop for exports

---

## What Does Not Change

- C# service layer, query engine, export service, config service — no changes
- Auth flow (redirect to login, role-based nav visibility) — no changes
- Export functionality (CSV/Excel download via JS interop) — no changes
- Validation error model in `Home.razor` (`List<string>`) — no changes
- `RedirectToLogin` and `AccessDenied` components — no changes
- `blazor-error-ui` error banner — no changes
- No new NuGet or npm dependencies

---

## Reference

Mockup: `docs/mockups/redesign-mockup.html`
