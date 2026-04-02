# TaskFlow Demo App — Design Specification

**Date:** 2026-04-02
**Status:** Draft

## Overview

TaskFlow is a project management demo app that replaces the current toy examples (counter, weather, product) with a cohesive, realistic application. It demonstrates every TheBlazorState feature in context: `[Persist]`, `[Shared]`, `[Shared, Persist]`, TTL, `LoadFrom()`, `KeySuffix()`, storage strategies, and Meta companions.

Styled with Tailwind CSS v4. Light theme, full sidebar, Linear/Notion inspired.

## Pages

### Sidebar (always visible)

**Purpose:** Demonstrates `[Shared]` — the selected project is shared state that all pages react to.

**Content:**
- App logo + name: "TaskFlow"
- "Projects" section with 3 items: Marketing, Engineering, Design
- Active project highlighted with indigo background
- "Views" section with nav links: Dashboard, Board, Settings
- Active route highlighted

**State:**
- Injects `ProjectState` (shared)
- Clicking a project sets `ProjectState.SelectedProject`
- All other pages react to this change automatically

### Dashboard (`/`)

**Purpose:** Demonstrates `[Persist]` with TTL and `LoadFrom()` for expensive API data that should survive prerender.

**Content:**
- Page title: "Dashboard" with project name subtitle
- 3 stat cards in a row:
  - Total Tasks (number + icon)
  - In Progress (number + icon, indigo accent)
  - Completed (number + icon, green accent)
- Staleness banner: when `StatsMeta.IsStale`, show "Data is stale — last updated X. [Refresh]"
- Recent activity list: 5 items with task name, action, and relative timestamp
- State inspector panel (collapsible, bottom)

**State:**
```csharp
[Persist(TimeToLive = "00:02:00")]
public partial DashboardData? Stats { get; set; }

partial void ConfigureState(__StateContext ctx)
{
    ctx.Stats
        .KeySuffix(Project.SelectedProject.Id)
        .LoadFrom(() => StatsService.GetDashboardAsync(Project.SelectedProject.Id));
}
```

**Features demonstrated:**
- `[Persist]` with TTL (2 minutes)
- `LoadFrom()` async factory
- `KeySuffix()` for per-project caching
- `StatsMeta.WasRestored` / `StatsMeta.IsStale` / `StatsMeta.LastUpdated`
- Injected `[Shared]` ProjectState triggers re-load on project switch

### Board (`/board`)

**Purpose:** Demonstrates `[Persist]` for complex nested state with per-project keying, and `[Shared]` cross-component reactivity.

**Content:**
- Page title: "Board" with project name subtitle
- 3 Kanban columns: To Do, In Progress, Done
- Each column has a header with task count
- Task cards with:
  - Title
  - Priority badge (High = red, Medium = yellow, Low = green)
  - Assignee initials avatar (colored circle)
- Move buttons on each card (left/right arrows) to move between columns
- State inspector panel (collapsible, bottom)

**State:**
```csharp
[Persist(TimeToLive = "00:05:00")]
public partial BoardData? Board { get; set; }

partial void ConfigureState(__StateContext ctx)
{
    ctx.Board
        .KeySuffix(Project.SelectedProject.Id)
        .LoadFrom(() => TaskService.GetBoardAsync(Project.SelectedProject.Id));
}
```

**Features demonstrated:**
- `[Persist]` with TTL (5 minutes)
- Complex nested state (board with columns containing task lists)
- `KeySuffix()` — switching projects loads a different cached board
- State mutation (moving tasks) triggers `IsDirty`
- `[Shared]` ProjectState integration

### Settings (`/settings`)

**Purpose:** Demonstrates `[Persist]` with `StorageStrategy.LocalStorage()` for preferences that survive browser refresh.

**Content:**
- Page title: "Settings"
- "Appearance" section:
  - Theme toggle: Light / Dark (two buttons, active highlighted)
  - Density toggle: Comfortable / Compact (two buttons, active highlighted)
- "About TheBlazorState" section:
  - Brief explanation of what each storage strategy does
  - Visual indicator showing which strategy is used for these preferences
- State inspector panel (collapsible, bottom)

**State:**
```csharp
[Inject] public ThemeState Theme { get; set; } = default!;

[Persist]
public partial string SavedTheme { get; set; }

[Persist]
public partial string SavedDensity { get; set; }

partial void ConfigureState(__StateContext ctx)
{
    ctx.SavedTheme.Storage = StorageStrategy.LocalStorage();
    ctx.SavedDensity.Storage = StorageStrategy.LocalStorage();
}
```

On init, if `SavedThemeMeta.WasRestored`, push saved values into ThemeState for cross-component reactivity.

**Features demonstrated:**
- `[Persist]` with `StorageStrategy.LocalStorage()` — explicit storage strategy, survives browser refresh
- `[Shared]` ThemeState — entire app reacts to theme/density changes
- Pattern: persist for durability + shared for reactivity, working together

## State Inspector Panel

A collapsible panel shown at the bottom of Dashboard, Board, and Settings pages. Educational tool for developers exploring the library.

**Content (per persisted property):**
- Property name and type
- Storage strategy being used
- `WasRestored`: boolean badge (green/gray)
- `IsDirty`: boolean badge
- `IsStale`: boolean badge (red when stale)
- `LastUpdated`: relative timestamp
- Key: the resolved storage key

**Implementation:** A reusable Razor component `StateInspector.razor` that accepts `StateMeta` instances and renders the panel.

## Shared State Classes

### ProjectState

```csharp
public partial class ProjectState
{
    [Shared]
    public partial Project SelectedProject { get; set; }
}

public record Project(int Id, string Name, string Color);
```

Registered as scoped. Sidebar sets it, Dashboard and Board consume it.

### ThemeState

```csharp
public partial class ThemeState
{
    [Shared]
    public partial string Theme { get; set; }

    [Shared]
    public partial string Density { get; set; }

    public ThemeState()
    {
        Theme = "light";
        Density = "comfortable";
    }
}
```

Registered as scoped. Settings page modifies it, MainLayout applies it (CSS classes on body).

The Settings page also has `[Persist]` properties with `StorageStrategy.LocalStorage()` to save preferences across browser refresh, and syncs them into ThemeState on restore:

```csharp
// In Settings.razor.cs
[Persist]
public partial string SavedTheme { get; set; }

[Persist]
public partial string SavedDensity { get; set; }

partial void ConfigureState(__StateContext ctx)
{
    ctx.SavedTheme.Storage = StorageStrategy.LocalStorage();
    ctx.SavedDensity.Storage = StorageStrategy.LocalStorage();
}
```

On initialization, if `SavedThemeMeta.WasRestored`, the Settings page pushes the saved values into `ThemeState`. This demonstrates both `[Persist]` with localStorage AND `[Shared]` reactivity working together, without requiring `[Shared, Persist]` composition on the same property (which is a future enhancement).

## Mock Services

### ProjectService

Returns 3 hardcoded projects:
- Marketing (Id: 1, Color: "#6366f1" indigo)
- Engineering (Id: 2, Color: "#0ea5e9" sky blue)
- Design (Id: 3, Color: "#f59e0b" amber)

### TaskService

Returns mock tasks per project. Each project has a different distribution:
- Marketing: 4 To Do, 3 In Progress, 5 Done
- Engineering: 6 To Do, 4 In Progress, 2 Done
- Design: 3 To Do, 2 In Progress, 7 Done

Each task has: Id, Title, Priority (High/Medium/Low), Assignee (name + initials), ColumnId.

Includes a simulated 200ms delay to make loading states visible.

### StatsService

Computes dashboard stats from TaskService data. Returns: TotalTasks, InProgress, Completed, RecentActivity (last 5 actions with timestamps).

Same 200ms simulated delay.

## Styling

### Tailwind CSS v4

Include via CDN in App.razor:
```html
<link href="https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4" rel="stylesheet">
```

Remove all Bootstrap references. Remove `app.css` (or reduce to minimal reset). Remove scoped CSS files from layout components.

### Design Tokens

- Primary: Indigo (indigo-600 for actions, indigo-50 for active backgrounds)
- Success: Emerald (emerald-500 for completed/done)
- Warning: Amber (amber-500 for medium priority)
- Danger: Rose (rose-500 for high priority)
- Background: white (main), gray-50 (columns/cards background)
- Sidebar: white with gray-200 border-right
- Text: slate-900 (primary), slate-500 (secondary)
- Font: Inter via CDN, fallback to system-ui

### Responsive

Desktop-only is acceptable for a demo. The sidebar is always visible. Minimum viewport: 1024px.

## Data Models

```csharp
public record Project(int Id, string Name, string Color);

public record TaskItem(
    int Id,
    string Title,
    Priority Priority,
    string AssigneeName,
    string AssigneeInitials,
    int ColumnId);

public enum Priority { Low, Medium, High }

public record BoardData(
    List<TaskItem> ToDo,
    List<TaskItem> InProgress,
    List<TaskItem> Done);

public record DashboardData(
    int TotalTasks,
    int InProgress,
    int Completed,
    List<ActivityItem> RecentActivity);

public record ActivityItem(
    string TaskTitle,
    string Action,
    string Actor,
    DateTimeOffset Timestamp);
```

## File Structure

```
TheBlazorState.Demo/
  Program.cs
  Components/
    App.razor
    Routes.razor
    _Imports.razor
    Layout/
      MainLayout.razor
      Sidebar.razor
    Pages/
      Dashboard.razor + .razor.cs
      Board.razor + .razor.cs
      Settings.razor + .razor.cs
    Shared/
      StateInspector.razor + .razor.cs
      StateBadge.razor
  Services/
    ProjectService.cs
    TaskService.cs
    StatsService.cs
  State/
    ProjectState.cs
    ThemeState.cs
  Models/
    Project.cs
    TaskItem.cs
    BoardData.cs
    DashboardData.cs
```

## What Gets Deleted

- `Components/Pages/Counter.razor` + `.razor.cs`
- `Components/Pages/Weather.razor` + `.razor.cs`
- `Components/Pages/ProductDetail.razor` + `.razor.cs`
- `Components/Pages/Home.razor`
- `Components/Pages/NotFound.razor`
- `Components/Pages/Error.razor`
- `Components/Layout/NavMenu.razor` + scoped CSS
- `Components/Layout/ReconnectModal.razor` + JS + scoped CSS
- `Components/Layout/MainLayout.razor` scoped CSS
- `Services/WeatherService.cs`
- `Services/ProductService.cs`
- `State/CartState.cs`
- `wwwroot/app.css`
- `wwwroot/lib/bootstrap/` (entire directory)

## Out of Scope

- Drag and drop for task cards (move buttons are sufficient)
- Real backend / database
- Authentication
- Mobile responsive design
- Dark mode implementation (toggle exists but only applies a CSS class — actual dark styles are stretch goal)
