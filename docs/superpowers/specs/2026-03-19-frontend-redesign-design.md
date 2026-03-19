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
| `--danger` | `#EF4444` | Destructive actions |
| `--success` | `#10B981` | Completed step indicators |

### Typography
- **Font family:** Inter (Google Fonts) with system-ui fallback
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
- Three dropdowns separated by 1px vertical dividers
- Each dropdown has a numbered step badge above it (turns green checkmark when selected)
- Labels: 11px uppercase, gray
- Query dropdown is `flex: 2` (wider than the others)
- Run button: primary blue, right-aligned, disabled until all three are selected

**2. PROD Warning Banner** (conditional)
Shown only when Environment = PROD. Amber background, amber border, warning icon. Text: "Production environment — this query runs against live data. Results are read-only."

**3. Parameters Card** (`params-card`)
Shown only when a query is selected and it has parameters.
- Section label: "PARAMETERS" in small uppercase gray
- Auto-grid layout: `repeat(auto-fill, minmax(200px, 1fr))`
- Each parameter: label (with red asterisk if required) + appropriate input (text, date, number, select)
- Validation errors appear below the relevant field (not a separate list)

**4. Results Card** (`results-card`)
Shown after query executes.
- **Header row:** row count (bold) + elapsed time (gray) + Export CSV / Export Excel buttons (right-aligned)
- **Truncation badge:** amber "10,000+ rows" badge shown when results are truncated
- **Table:** sticky column headers (uppercase, small, gray), alternating row hover state, horizontally scrollable, max-height 480px with vertical scroll

### Manage Queries Page (Admin)

- Page title "Manage Queries" + subtitle showing `queries.json · N queries`
- Action buttons top-right: "↺ Reload Config" (secondary) + "+ Add Query" (primary)
- **Query list:** each query is a card row containing:
  - Name (bold) + metadata line (param count · allowed environments)
  - Active/Disabled badge (green or gray pill)
  - Edit / Disable / Remove buttons (small, right-aligned; Disable and Remove are styled as danger)
  - Disabled queries: full row at reduced opacity (0.55)
- **Edit form:** expands inline below the query row (no modal), same card style

### Environments Page (Admin)

- Page title "Environments" + hint about connection string resolution
- Each environment: a card with header (env name + db count) and a standard table (Label, Connection String Key, Timeout)
- Connection string keys displayed as `<code>` with subtle background

---

## Component Inventory

| Component | File | Change |
|---|---|---|
| `MainLayout` | `MainLayout.razor` + `.razor.css` | Full rewrite — new nav markup + CSS variables |
| `StepBar` | `StepBar.razor` | Replace with numbered badges in filter bar (StepBar component removed) |
| `Home` | `Home.razor` | Rewrite — single-page layout, filter bar replaces wizard |
| `ResultsTable` | `ResultsTable.razor` | Restyle — new header, table, export buttons |
| `ManageQueries` | `ManageQueries.razor` | Restyle — query cards, inline edit form |
| `Environments` | `Environments.razor` | Restyle — env cards with tables |
| `app.css` | `wwwroot/app.css` | Add CSS custom properties (design tokens), global resets, Inter font import |
| `DatabaseSelector` | `DatabaseSelector.razor` | Restyle as dropdown select |
| `QuerySelector` | `QuerySelector.razor` | Restyle as dropdown select |
| `ParameterForm` | `ParameterForm.razor` | Restyle — grid layout, per-field validation |
| `ProdWarningBanner` | `ProdWarningBanner.razor` | Restyle — amber banner |

---

## CSS Architecture

- **Design tokens** defined as CSS custom properties on `:root` in `app.css`
- **Scoped styles** stay in `.razor.css` files per component
- **No new CSS framework** — Bootstrap is already included but barely used; keep it as a reset/utility base only, do not add Tailwind or other frameworks
- **No JavaScript** beyond the existing `downloadFile` interop for exports

---

## What Does Not Change

- All Blazor component logic, data binding, and C# code stays unchanged
- Auth flow (redirect to login, role-based nav visibility) stays unchanged
- Export functionality stays unchanged
- No new dependencies

---

## Reference

Mockup: `docs/mockups/redesign-mockup.html`
