# TaskFlow Demo App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the toy demo (counter, weather, product) with TaskFlow — a realistic project management app showcasing every TheBlazorState feature, styled with Tailwind CSS v4.

**Architecture:** Blazor Server demo with 3 pages (Dashboard, Board, Settings), sidebar with project switcher, mock services, two shared state classes (ProjectState, ThemeState). Each page demonstrates a specific TheBlazorState pattern. State Inspector panel provides educational visibility into what the library does.

**Tech Stack:** .NET 10, Blazor Server, Tailwind CSS v4 (CDN), TheBlazorState library

**Spec:** `docs/superpowers/specs/2026-04-02-taskflow-demo-design.md`

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `TheBlazorState.Demo/Models/Project.cs` | Create | Project record |
| `TheBlazorState.Demo/Models/TaskItem.cs` | Create | TaskItem record + Priority enum |
| `TheBlazorState.Demo/Models/BoardData.cs` | Create | BoardData record |
| `TheBlazorState.Demo/Models/DashboardData.cs` | Create | DashboardData + ActivityItem records |
| `TheBlazorState.Demo/Services/ProjectService.cs` | Rewrite | 3 hardcoded projects |
| `TheBlazorState.Demo/Services/TaskService.cs` | Create | Mock tasks per project with 200ms delay |
| `TheBlazorState.Demo/Services/StatsService.cs` | Create | Dashboard stats from TaskService |
| `TheBlazorState.Demo/State/ProjectState.cs` | Create | [Shared] SelectedProject |
| `TheBlazorState.Demo/State/ThemeState.cs` | Create | [Shared] Theme + Density |
| `TheBlazorState.Demo/Components/App.razor` | Rewrite | Tailwind v4 CDN, remove Bootstrap |
| `TheBlazorState.Demo/Components/_Imports.razor` | Rewrite | Updated usings |
| `TheBlazorState.Demo/Components/Routes.razor` | Keep | Unchanged |
| `TheBlazorState.Demo/Components/Layout/MainLayout.razor` | Rewrite | Tailwind layout, sidebar + main |
| `TheBlazorState.Demo/Components/Layout/Sidebar.razor` | Create | Project switcher + nav links |
| `TheBlazorState.Demo/Components/Pages/Dashboard.razor` + `.razor.cs` | Create | Stat cards, activity, [Persist] with TTL |
| `TheBlazorState.Demo/Components/Pages/Board.razor` + `.razor.cs` | Create | Kanban board, [Persist] with KeySuffix |
| `TheBlazorState.Demo/Components/Pages/Settings.razor` + `.razor.cs` | Create | Preferences with LocalStorage |
| `TheBlazorState.Demo/Components/Shared/StateInspector.razor` + `.razor.cs` | Create | Meta inspector panel |
| `TheBlazorState.Demo/Components/Shared/StateBadge.razor` | Create | Boolean badge component |
| `TheBlazorState.Demo/Program.cs` | Rewrite | New services + state registration |
| Old files | Delete | Counter, Weather, ProductDetail, Home, NavMenu, etc. |

---

### Task 1: Clean Slate — Delete Old Files and Set Up Tailwind

Delete all old demo files and set up the Tailwind v4 foundation.

**Files:**
- Delete: All files listed in spec "What Gets Deleted" section
- Modify: `TheBlazorState.Demo/Components/App.razor`
- Modify: `TheBlazorState.Demo/Components/_Imports.razor`

- [ ] **Step 1: Delete old page components**

```bash
cd C:/repo/POC/BlazorStatePlus
rm TheBlazorState.Demo/Components/Pages/Counter.razor
rm TheBlazorState.Demo/Components/Pages/Counter.razor.cs
rm TheBlazorState.Demo/Components/Pages/Weather.razor
rm TheBlazorState.Demo/Components/Pages/Weather.razor.cs
rm TheBlazorState.Demo/Components/Pages/ProductDetail.razor
rm TheBlazorState.Demo/Components/Pages/ProductDetail.razor.cs
rm TheBlazorState.Demo/Components/Pages/Home.razor
rm TheBlazorState.Demo/Components/Pages/NotFound.razor
rm TheBlazorState.Demo/Components/Pages/Error.razor
```

- [ ] **Step 2: Delete old layout, services, state, and styling**

```bash
rm TheBlazorState.Demo/Components/Layout/NavMenu.razor
rm -f TheBlazorState.Demo/Components/Layout/NavMenu.razor.css
rm TheBlazorState.Demo/Components/Layout/ReconnectModal.razor
rm -f TheBlazorState.Demo/Components/Layout/ReconnectModal.razor.js
rm -f TheBlazorState.Demo/Components/Layout/ReconnectModal.razor.css
rm -f TheBlazorState.Demo/Components/Layout/MainLayout.razor.css
rm TheBlazorState.Demo/Services/WeatherService.cs
rm TheBlazorState.Demo/Services/ProductService.cs
rm TheBlazorState.Demo/State/CartState.cs
rm -f TheBlazorState.Demo/wwwroot/app.css
rm -rf TheBlazorState.Demo/wwwroot/lib/
```

- [ ] **Step 3: Rewrite App.razor with Tailwind v4**

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4" rel="stylesheet" />
    <style type="text/tailwindcss">
        @theme {
            --font-sans: 'Inter', ui-sans-serif, system-ui, sans-serif;
        }
    </style>
    <HeadOutlet />
</head>
<body class="font-sans bg-gray-50 text-slate-900 antialiased">
    <Routes />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

- [ ] **Step 4: Rewrite _Imports.razor**

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.JSInterop
@using TheBlazorState.Demo
@using TheBlazorState.Demo.Components
@using TheBlazorState.Demo.Components.Layout
@using TheBlazorState.Demo.Components.Shared
@using TheBlazorState.Demo.Models
@using TheBlazorState.Demo.State
@using TheBlazorState.Abstractions
@using TheBlazorState.Attributes
```

- [ ] **Step 5: Update Routes.razor to remove NotFound reference**

```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: clean slate — remove old demo, add Tailwind v4"
```

---

### Task 2: Models and Mock Services

Create all data models and mock services.

**Files:**
- Create: `TheBlazorState.Demo/Models/Project.cs`
- Create: `TheBlazorState.Demo/Models/TaskItem.cs`
- Create: `TheBlazorState.Demo/Models/BoardData.cs`
- Create: `TheBlazorState.Demo/Models/DashboardData.cs`
- Create: `TheBlazorState.Demo/Services/ProjectService.cs` (rewrite)
- Create: `TheBlazorState.Demo/Services/TaskService.cs`
- Create: `TheBlazorState.Demo/Services/StatsService.cs`

- [ ] **Step 1: Create Models**

`Models/Project.cs`:
```csharp
namespace TheBlazorState.Demo.Models;

public record Project(int Id, string Name, string Color);
```

`Models/TaskItem.cs`:
```csharp
namespace TheBlazorState.Demo.Models;

public record TaskItem(int Id, string Title, Priority Priority, string AssigneeName, string AssigneeInitials);

public enum Priority { Low, Medium, High }
```

`Models/BoardData.cs`:
```csharp
namespace TheBlazorState.Demo.Models;

public record BoardData(List<TaskItem> ToDo, List<TaskItem> InProgress, List<TaskItem> Done);
```

`Models/DashboardData.cs`:
```csharp
namespace TheBlazorState.Demo.Models;

public record DashboardData(int TotalTasks, int InProgress, int Completed, List<ActivityItem> RecentActivity);

public record ActivityItem(string TaskTitle, string Action, string Actor, DateTimeOffset Timestamp);
```

- [ ] **Step 2: Rewrite ProjectService**

`Services/ProjectService.cs`:
```csharp
using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class ProjectService
{
    private static readonly List<Project> Projects =
    [
        new(1, "Marketing", "#6366f1"),
        new(2, "Engineering", "#0ea5e9"),
        new(3, "Design", "#f59e0b")
    ];

    public List<Project> GetAll() => Projects;

    public Project GetDefault() => Projects[0];
}
```

- [ ] **Step 3: Create TaskService**

`Services/TaskService.cs`:
```csharp
using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class TaskService
{
    private static readonly Dictionary<int, BoardData> Boards = new()
    {
        [1] = new BoardData(
            ToDo: [
                new(1, "Write blog post", Priority.High, "Alice", "AL"),
                new(2, "Design social media graphics", Priority.Medium, "Bob", "BO"),
                new(3, "Plan Q3 campaign", Priority.Low, "Carol", "CA"),
                new(4, "Update landing page copy", Priority.Medium, "Alice", "AL")
            ],
            InProgress: [
                new(5, "A/B test email subject lines", Priority.High, "Bob", "BO"),
                new(6, "Prepare press kit", Priority.Medium, "Carol", "CA"),
                new(7, "Review analytics report", Priority.Low, "Alice", "AL")
            ],
            Done: [
                new(8, "Launch newsletter", Priority.High, "Bob", "BO"),
                new(9, "Set up tracking pixels", Priority.Medium, "Carol", "CA"),
                new(10, "Redesign email template", Priority.Low, "Alice", "AL"),
                new(11, "Create brand guidelines", Priority.High, "Bob", "BO"),
                new(12, "Audit competitor content", Priority.Medium, "Carol", "CA")
            ]),
        [2] = new BoardData(
            ToDo: [
                new(20, "Implement auth middleware", Priority.High, "Dave", "DA"),
                new(21, "Add rate limiting", Priority.High, "Eve", "EV"),
                new(22, "Write integration tests", Priority.Medium, "Frank", "FR"),
                new(23, "Set up monitoring", Priority.Low, "Dave", "DA"),
                new(24, "Optimize database queries", Priority.High, "Eve", "EV"),
                new(25, "Migrate to .NET 10", Priority.Medium, "Frank", "FR")
            ],
            InProgress: [
                new(26, "Build REST API v2", Priority.High, "Dave", "DA"),
                new(27, "Refactor data layer", Priority.Medium, "Eve", "EV"),
                new(28, "Configure CI/CD pipeline", Priority.High, "Frank", "FR"),
                new(29, "Implement caching strategy", Priority.Medium, "Dave", "DA")
            ],
            Done: [
                new(30, "Set up project structure", Priority.High, "Eve", "EV"),
                new(31, "Create development environment", Priority.Medium, "Frank", "FR")
            ]),
        [3] = new BoardData(
            ToDo: [
                new(40, "Design onboarding flow", Priority.High, "Grace", "GR"),
                new(41, "Create icon set", Priority.Medium, "Hank", "HA"),
                new(42, "Prototype mobile layout", Priority.Low, "Grace", "GR")
            ],
            InProgress: [
                new(43, "Redesign settings page", Priority.Medium, "Hank", "HA"),
                new(44, "Update color palette", Priority.High, "Grace", "GR")
            ],
            Done: [
                new(45, "Design system foundations", Priority.High, "Hank", "HA"),
                new(46, "Component library v1", Priority.High, "Grace", "GR"),
                new(47, "Accessibility audit", Priority.Medium, "Hank", "HA"),
                new(48, "User research interviews", Priority.Low, "Grace", "GR"),
                new(49, "Wireframe dashboard", Priority.Medium, "Hank", "HA"),
                new(50, "Style guide documentation", Priority.Low, "Grace", "GR"),
                new(51, "Logo redesign", Priority.High, "Grace", "GR")
            ])
    };

    public async Task<BoardData> GetBoardAsync(int projectId)
    {
        await Task.Delay(200); // simulate API latency
        return Boards.GetValueOrDefault(projectId) ?? Boards[1];
    }
}
```

- [ ] **Step 4: Create StatsService**

`Services/StatsService.cs`:
```csharp
using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class StatsService
{
    private readonly TaskService _tasks;

    public StatsService(TaskService tasks) => _tasks = tasks;

    public async Task<DashboardData> GetDashboardAsync(int projectId)
    {
        var board = await _tasks.GetBoardAsync(projectId);
        var total = board.ToDo.Count + board.InProgress.Count + board.Done.Count;
        var now = DateTimeOffset.UtcNow;

        var activities = board.Done
            .Take(5)
            .Select((t, i) => new ActivityItem(
                t.Title,
                "completed",
                t.AssigneeName,
                now.AddMinutes(-(i * 37 + 12))))
            .ToList();

        return new DashboardData(total, board.InProgress.Count, board.Done.Count, activities);
    }
}
```

- [ ] **Step 5: Build**

```bash
dotnet build TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

Expected: Build may fail because pages don't exist yet. That's fine — models and services compile independently.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add TaskFlow models and mock services"
```

---

### Task 3: Shared State Classes and Program.cs

**Files:**
- Create: `TheBlazorState.Demo/State/ProjectState.cs`
- Create: `TheBlazorState.Demo/State/ThemeState.cs`
- Rewrite: `TheBlazorState.Demo/Program.cs`

- [ ] **Step 1: Create ProjectState**

`State/ProjectState.cs`:
```csharp
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.State;

public partial class ProjectState
{
    [Shared]
    public partial Project SelectedProject { get; set; }

    public ProjectState()
    {
        SelectedProject = new Project(1, "Marketing", "#6366f1");
    }
}
```

- [ ] **Step 2: Create ThemeState**

`State/ThemeState.cs`:
```csharp
using TheBlazorState.Attributes;

namespace TheBlazorState.Demo.State;

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

- [ ] **Step 3: Rewrite Program.cs**

```csharp
using TheBlazorState.Demo.Components;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddTheBlazorState();

builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<ProjectState>();
builder.Services.AddScoped<ThemeState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

namespace TheBlazorState.Demo { public partial class Program { } }
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add ProjectState, ThemeState, and updated Program.cs"
```

---

### Task 4: Layout and Sidebar

**Files:**
- Rewrite: `TheBlazorState.Demo/Components/Layout/MainLayout.razor`
- Create: `TheBlazorState.Demo/Components/Layout/Sidebar.razor`

- [ ] **Step 1: Create Sidebar.razor**

```razor
@inject ProjectService ProjectSvc
@inject ProjectState ProjectState
@inject NavigationManager Nav

<aside class="w-64 bg-white border-r border-gray-200 flex flex-col h-screen sticky top-0">
    <!-- Logo -->
    <div class="px-5 py-4 border-b border-gray-100">
        <h1 class="text-lg font-bold text-slate-900 tracking-tight">TaskFlow</h1>
        <p class="text-xs text-slate-400 mt-0.5">TheBlazorState Demo</p>
    </div>

    <!-- Projects -->
    <div class="px-3 py-4">
        <p class="px-2 mb-2 text-xs font-semibold text-slate-400 uppercase tracking-wider">Projects</p>
        @foreach (var project in ProjectSvc.GetAll())
        {
            var isActive = ProjectState.SelectedProject.Id == project.Id;
            <button @onclick="() => SelectProject(project)"
                    class="@($"w-full text-left px-3 py-2 rounded-lg text-sm mb-0.5 flex items-center gap-2.5 transition-colors {(isActive ? "bg-indigo-50 text-indigo-700 font-medium" : "text-slate-600 hover:bg-gray-50")}")">
                <span class="w-2.5 h-2.5 rounded-full flex-shrink-0" style="background-color: @project.Color"></span>
                @project.Name
            </button>
        }
    </div>

    <!-- Navigation -->
    <div class="px-3 py-2 border-t border-gray-100">
        <p class="px-2 mb-2 text-xs font-semibold text-slate-400 uppercase tracking-wider">Views</p>
        <NavLink href="" Match="NavLinkMatch.All"
                 class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-slate-600 hover:bg-gray-50 transition-colors"
                 ActiveClass="bg-indigo-50 text-indigo-700 font-medium">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zm10 0a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zm10 0a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"/></svg>
            Dashboard
        </NavLink>
        <NavLink href="board"
                 class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-slate-600 hover:bg-gray-50 transition-colors"
                 ActiveClass="bg-indigo-50 text-indigo-700 font-medium">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2"/></svg>
            Board
        </NavLink>
        <NavLink href="settings"
                 class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-slate-600 hover:bg-gray-50 transition-colors"
                 ActiveClass="bg-indigo-50 text-indigo-700 font-medium">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/></svg>
            Settings
        </NavLink>
    </div>

    <!-- Footer -->
    <div class="mt-auto px-5 py-4 border-t border-gray-100">
        <p class="text-xs text-slate-400">Powered by <span class="font-medium text-indigo-500">TheBlazorState</span></p>
    </div>
</aside>

@code {
    private void SelectProject(Project project)
    {
        ProjectState.SelectedProject = project;
    }
}
```

- [ ] **Step 2: Rewrite MainLayout.razor**

```razor
@inherits LayoutComponentBase

<div class="flex min-h-screen">
    <Sidebar />
    <main class="flex-1 overflow-auto">
        <div class="max-w-6xl mx-auto px-8 py-8">
            @Body
        </div>
    </main>
</div>
```

- [ ] **Step 3: Build and run**

```bash
dotnet build TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

Expected: May fail because no pages exist. Create a minimal Dashboard placeholder to verify layout.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add Tailwind layout with sidebar and project switcher"
```

---

### Task 5: State Inspector Component

Reusable educational panel showing Meta state for each persisted property.

**Files:**
- Create: `TheBlazorState.Demo/Components/Shared/StateBadge.razor`
- Create: `TheBlazorState.Demo/Components/Shared/StateInspector.razor`
- Create: `TheBlazorState.Demo/Components/Shared/StateInspector.razor.cs`

- [ ] **Step 1: Create StateBadge.razor**

```razor
@if (Value)
{
    <span class="@($"inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium {TrueClass}")">@TrueLabel</span>
}
else
{
    <span class="@($"inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium {FalseClass}")">@FalseLabel</span>
}

@code {
    [Parameter, EditorRequired] public bool Value { get; set; }
    [Parameter] public string TrueLabel { get; set; } = "Yes";
    [Parameter] public string FalseLabel { get; set; } = "No";
    [Parameter] public string TrueClass { get; set; } = "bg-emerald-100 text-emerald-700";
    [Parameter] public string FalseClass { get; set; } = "bg-gray-100 text-gray-500";
}
```

- [ ] **Step 2: Create StateInspector**

`StateInspector.razor`:
```razor
<div class="mt-8 border border-gray-200 rounded-xl overflow-hidden">
    <button @onclick="Toggle"
            class="w-full flex items-center justify-between px-4 py-3 bg-gray-50 hover:bg-gray-100 transition-colors text-sm">
        <span class="font-semibold text-slate-700 flex items-center gap-2">
            <svg class="w-4 h-4 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/></svg>
            State Inspector
        </span>
        <svg class="@($"w-4 h-4 text-slate-400 transition-transform {(_open ? "rotate-180" : "")}")" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
    </button>
    @if (_open)
    {
        <div class="p-4 bg-white">
            <table class="w-full text-sm">
                <thead>
                    <tr class="text-left text-xs text-slate-400 uppercase tracking-wider">
                        <th class="pb-2 pr-4">Property</th>
                        <th class="pb-2 pr-4">Storage</th>
                        <th class="pb-2 pr-4">Restored</th>
                        <th class="pb-2 pr-4">Dirty</th>
                        <th class="pb-2 pr-4">Stale</th>
                        <th class="pb-2">Last Updated</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                    @foreach (var entry in Entries)
                    {
                        <tr>
                            <td class="py-2 pr-4 font-mono text-xs text-slate-700">@entry.Name</td>
                            <td class="py-2 pr-4 text-xs text-slate-500">@entry.Strategy</td>
                            <td class="py-2 pr-4"><StateBadge Value="entry.Meta.WasRestored" TrueLabel="Restored" FalseLabel="Fresh" /></td>
                            <td class="py-2 pr-4"><StateBadge Value="entry.Meta.IsDirty" TrueLabel="Dirty" FalseLabel="Clean" TrueClass="bg-amber-100 text-amber-700" /></td>
                            <td class="py-2 pr-4"><StateBadge Value="entry.Meta.IsStale" TrueLabel="Stale" FalseLabel="Fresh" TrueClass="bg-rose-100 text-rose-700" /></td>
                            <td class="py-2 text-xs text-slate-500">@FormatTime(entry.Meta.LastUpdated)</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
```

`StateInspector.razor.cs`:
```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Abstractions;

namespace TheBlazorState.Demo.Components.Shared;

public partial class StateInspector : ComponentBase
{
    [Parameter, EditorRequired]
    public List<StateInspectorEntry> Entries { get; set; } = [];

    private bool _open = false;

    private void Toggle() => _open = !_open;

    private static string FormatTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        return time.LocalDateTime.ToString("HH:mm:ss");
    }
}

public record StateInspectorEntry(string Name, string Strategy, StateMeta Meta);
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add StateInspector and StateBadge reusable components"
```

---

### Task 6: Dashboard Page

**Files:**
- Create: `TheBlazorState.Demo/Components/Pages/Dashboard.razor`
- Create: `TheBlazorState.Demo/Components/Pages/Dashboard.razor.cs`

- [ ] **Step 1: Create Dashboard.razor.cs**

```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] public ProjectState Project { get; set; } = default!;
    [Inject] private StatsService StatsService { get; set; } = default!;

    [Persist(TimeToLive = "00:02:00")]
    public partial DashboardData? Stats { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.Stats
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(() => StatsService.GetDashboardAsync(Project.SelectedProject.Id));
    }

    private async Task Refresh()
    {
        Stats = await StatsService.GetDashboardAsync(Project.SelectedProject.Id);
    }

    private List<StateInspectorEntry> InspectorEntries =>
    [
        new("Stats", "PrerenderHtml (default)", StatsMeta)
    ];

    private static string FormatRelativeTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}
```

- [ ] **Step 2: Create Dashboard.razor**

```razor
@page "/"
@rendermode InteractiveServer

<PageTitle>Dashboard — TaskFlow</PageTitle>

<div class="mb-6">
    <h1 class="text-2xl font-bold text-slate-900">Dashboard</h1>
    <p class="text-sm text-slate-500 mt-1">@Project.SelectedProject.Name project overview</p>
</div>

@if (Stats is null)
{
    <div class="flex items-center gap-3 text-slate-400 py-12">
        <svg class="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path></svg>
        Loading dashboard...
    </div>
}
else
{
    @if (StatsMeta.IsStale)
    {
        <div class="mb-6 flex items-center gap-3 bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 rounded-lg text-sm">
            <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"/></svg>
            <span>Data is stale — last updated @StatsMeta.LastUpdated.LocalDateTime.ToString("HH:mm:ss")</span>
            <button @onclick="Refresh" class="ml-auto text-amber-700 font-medium hover:underline">Refresh</button>
        </div>
    }

    @if (StatsMeta.WasRestored)
    {
        <div class="mb-6 flex items-center gap-2 text-xs text-emerald-600 bg-emerald-50 px-3 py-2 rounded-lg">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
            Restored from prerender cache — no redundant API call
        </div>
    }

    <!-- Stat Cards -->
    <div class="grid grid-cols-3 gap-5 mb-8">
        <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-sm font-medium text-slate-500">Total Tasks</p>
            <p class="text-3xl font-bold text-slate-900 mt-1">@Stats.TotalTasks</p>
        </div>
        <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-sm font-medium text-slate-500">In Progress</p>
            <p class="text-3xl font-bold text-indigo-600 mt-1">@Stats.InProgress</p>
        </div>
        <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-sm font-medium text-slate-500">Completed</p>
            <p class="text-3xl font-bold text-emerald-600 mt-1">@Stats.Completed</p>
        </div>
    </div>

    <!-- Recent Activity -->
    <div class="bg-white rounded-xl border border-gray-200">
        <div class="px-5 py-4 border-b border-gray-100">
            <h2 class="text-sm font-semibold text-slate-700">Recent Activity</h2>
        </div>
        <ul class="divide-y divide-gray-100">
            @foreach (var activity in Stats.RecentActivity)
            {
                <li class="px-5 py-3 flex items-center justify-between">
                    <div>
                        <span class="text-sm text-slate-700 font-medium">@activity.TaskTitle</span>
                        <span class="text-sm text-slate-400 ml-1">@activity.Action by @activity.Actor</span>
                    </div>
                    <span class="text-xs text-slate-400">@FormatRelativeTime(activity.Timestamp)</span>
                </li>
            }
        </ul>
    </div>

    <StateInspector Entries="InspectorEntries" />
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add Dashboard page with [Persist] TTL and stat cards"
```

---

### Task 7: Board Page

**Files:**
- Create: `TheBlazorState.Demo/Components/Pages/Board.razor`
- Create: `TheBlazorState.Demo/Components/Pages/Board.razor.cs`

- [ ] **Step 1: Create Board.razor.cs**

```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Board : ComponentBase
{
    [Inject] public ProjectState Project { get; set; } = default!;
    [Inject] private TaskService TaskService { get; set; } = default!;

    [Persist(TimeToLive = "00:05:00")]
    public partial BoardData? BoardState { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.BoardState
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(() => TaskService.GetBoardAsync(Project.SelectedProject.Id));
    }

    private void MoveTask(TaskItem task, string from, string to)
    {
        if (BoardState is null) return;

        var todo = new List<TaskItem>(BoardState.ToDo);
        var inProgress = new List<TaskItem>(BoardState.InProgress);
        var done = new List<TaskItem>(BoardState.Done);

        GetList(from, todo, inProgress, done).Remove(task);
        GetList(to, todo, inProgress, done).Add(task);

        BoardState = new BoardData(todo, inProgress, done);
    }

    private static List<TaskItem> GetList(string column, List<TaskItem> todo, List<TaskItem> inProgress, List<TaskItem> done)
        => column switch
        {
            "todo" => todo,
            "inprogress" => inProgress,
            "done" => done,
            _ => todo
        };

    private static string PriorityClasses(Priority p) => p switch
    {
        Priority.High => "bg-rose-100 text-rose-700",
        Priority.Medium => "bg-amber-100 text-amber-700",
        Priority.Low => "bg-emerald-100 text-emerald-700",
        _ => "bg-gray-100 text-gray-600"
    };

    private static string AvatarColor(string initials) =>
        (initials[0] % 5) switch
        {
            0 => "bg-indigo-500",
            1 => "bg-emerald-500",
            2 => "bg-amber-500",
            3 => "bg-rose-500",
            _ => "bg-sky-500"
        };

    private List<StateInspectorEntry> InspectorEntries =>
    [
        new("BoardState", "PrerenderHtml (default)", BoardStateMeta)
    ];
}
```

- [ ] **Step 2: Create Board.razor**

```razor
@page "/board"
@rendermode InteractiveServer

<PageTitle>Board — TaskFlow</PageTitle>

<div class="mb-6">
    <h1 class="text-2xl font-bold text-slate-900">Board</h1>
    <p class="text-sm text-slate-500 mt-1">@Project.SelectedProject.Name — Kanban view</p>
</div>

@if (BoardState is null)
{
    <div class="flex items-center gap-3 text-slate-400 py-12">
        <svg class="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path></svg>
        Loading board...
    </div>
}
else
{
    @if (BoardStateMeta.WasRestored)
    {
        <div class="mb-4 flex items-center gap-2 text-xs text-emerald-600 bg-emerald-50 px-3 py-2 rounded-lg">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
            Board restored from cache — instant render
        </div>
    }

    <div class="grid grid-cols-3 gap-5">
        @{ RenderColumn("To Do", BoardState.ToDo, "todo", null, "inprogress"); }
        @{ RenderColumn("In Progress", BoardState.InProgress, "inprogress", "todo", "done"); }
        @{ RenderColumn("Done", BoardState.Done, "done", "inprogress", null); }
    </div>

    <StateInspector Entries="InspectorEntries" />
}

@{ void RenderColumn(string title, List<TaskItem> tasks, string columnId, string? prevColumn, string? nextColumn)
{
    <div class="bg-gray-100 rounded-xl p-4">
        <div class="flex items-center justify-between mb-3">
            <h3 class="text-sm font-semibold text-slate-700">@title</h3>
            <span class="text-xs text-slate-400 bg-white px-2 py-0.5 rounded-full">@tasks.Count</span>
        </div>
        <div class="space-y-2.5">
            @foreach (var task in tasks)
            {
                <div class="bg-white rounded-lg border border-gray-200 p-3 shadow-sm">
                    <p class="text-sm font-medium text-slate-800 mb-2">@task.Title</p>
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-2">
                            <span class="@($"text-xs px-1.5 py-0.5 rounded font-medium {PriorityClasses(task.Priority)}")">@task.Priority</span>
                            <span class="@($"w-5 h-5 rounded-full text-white text-[10px] flex items-center justify-center font-medium {AvatarColor(task.AssigneeInitials)}")" title="@task.AssigneeName">@task.AssigneeInitials</span>
                        </div>
                        <div class="flex gap-1">
                            @if (prevColumn is not null)
                            {
                                var prev = prevColumn;
                                <button @onclick="() => MoveTask(task, columnId, prev)"
                                        class="text-slate-400 hover:text-slate-600 p-0.5" title="Move left">
                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
                                </button>
                            }
                            @if (nextColumn is not null)
                            {
                                var next = nextColumn;
                                <button @onclick="() => MoveTask(task, columnId, next)"
                                        class="text-slate-400 hover:text-slate-600 p-0.5" title="Move right">
                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
                                </button>
                            }
                        </div>
                    </div>
                </div>
            }
        </div>
    </div>
} }
```

- [ ] **Step 3: Build**

```bash
dotnet build TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add Board page with Kanban columns and [Persist] KeySuffix"
```

---

### Task 8: Settings Page

**Files:**
- Create: `TheBlazorState.Demo/Components/Pages/Settings.razor`
- Create: `TheBlazorState.Demo/Components/Pages/Settings.razor.cs`

- [ ] **Step 1: Create Settings.razor.cs**

```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Settings : ComponentBase
{
    [Inject] public ThemeState Theme { get; set; } = default!;

    [Persist]
    public partial string? SavedTheme { get; set; }

    [Persist]
    public partial string? SavedDensity { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.SavedTheme.Storage = StorageStrategy.LocalStorage();
        ctx.SavedDensity.Storage = StorageStrategy.LocalStorage();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && SavedThemeMeta?.WasRestored == true)
        {
            if (SavedTheme is not null) Theme.Theme = SavedTheme;
            if (SavedDensity is not null) Theme.Density = SavedDensity;
            StateHasChanged();
        }
    }

    private void SetTheme(string theme)
    {
        Theme.Theme = theme;
        SavedTheme = theme;
    }

    private void SetDensity(string density)
    {
        Theme.Density = density;
        SavedDensity = density;
    }

    private List<StateInspectorEntry> InspectorEntries =>
    [
        new("SavedTheme", "LocalStorage", SavedThemeMeta),
        new("SavedDensity", "LocalStorage", SavedDensityMeta)
    ];
}
```

- [ ] **Step 2: Create Settings.razor**

```razor
@page "/settings"
@rendermode InteractiveServer

<PageTitle>Settings — TaskFlow</PageTitle>

<div class="mb-6">
    <h1 class="text-2xl font-bold text-slate-900">Settings</h1>
    <p class="text-sm text-slate-500 mt-1">Preferences persisted to localStorage</p>
</div>

@if (SavedThemeMeta?.WasRestored == true)
{
    <div class="mb-6 flex items-center gap-2 text-xs text-emerald-600 bg-emerald-50 px-3 py-2 rounded-lg">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
        Preferences restored from localStorage — survives browser refresh
    </div>
}

<!-- Appearance -->
<div class="bg-white rounded-xl border border-gray-200 mb-6">
    <div class="px-5 py-4 border-b border-gray-100">
        <h2 class="text-sm font-semibold text-slate-700">Appearance</h2>
    </div>
    <div class="p-5 space-y-6">
        <!-- Theme -->
        <div>
            <label class="block text-sm font-medium text-slate-700 mb-2">Theme</label>
            <div class="flex gap-2">
                <button @onclick='() => SetTheme("light")'
                        class="@($"px-4 py-2 rounded-lg text-sm font-medium border transition-colors {(Theme.Theme == "light" ? "bg-indigo-50 border-indigo-300 text-indigo-700" : "bg-white border-gray-200 text-slate-600 hover:bg-gray-50")}")">
                    Light
                </button>
                <button @onclick='() => SetTheme("dark")'
                        class="@($"px-4 py-2 rounded-lg text-sm font-medium border transition-colors {(Theme.Theme == "dark" ? "bg-indigo-50 border-indigo-300 text-indigo-700" : "bg-white border-gray-200 text-slate-600 hover:bg-gray-50")}")">
                    Dark
                </button>
            </div>
        </div>

        <!-- Density -->
        <div>
            <label class="block text-sm font-medium text-slate-700 mb-2">Density</label>
            <div class="flex gap-2">
                <button @onclick='() => SetDensity("comfortable")'
                        class="@($"px-4 py-2 rounded-lg text-sm font-medium border transition-colors {(Theme.Density == "comfortable" ? "bg-indigo-50 border-indigo-300 text-indigo-700" : "bg-white border-gray-200 text-slate-600 hover:bg-gray-50")}")">
                    Comfortable
                </button>
                <button @onclick='() => SetDensity("compact")'
                        class="@($"px-4 py-2 rounded-lg text-sm font-medium border transition-colors {(Theme.Density == "compact" ? "bg-indigo-50 border-indigo-300 text-indigo-700" : "bg-white border-gray-200 text-slate-600 hover:bg-gray-50")}")">
                    Compact
                </button>
            </div>
        </div>
    </div>
</div>

<!-- Storage Strategy Info -->
<div class="bg-white rounded-xl border border-gray-200 mb-6">
    <div class="px-5 py-4 border-b border-gray-100">
        <h2 class="text-sm font-semibold text-slate-700">About Storage Strategies</h2>
    </div>
    <div class="p-5">
        <p class="text-sm text-slate-600 mb-4">
            These preferences use <code class="text-xs bg-gray-100 px-1.5 py-0.5 rounded font-mono">StorageStrategy.LocalStorage()</code>,
            so they survive browser refresh. Try changing a setting, then refresh the page.
        </p>
        <div class="grid grid-cols-2 gap-3 text-sm">
            <div class="bg-gray-50 rounded-lg p-3">
                <p class="font-medium text-slate-700">PrerenderHtml</p>
                <p class="text-xs text-slate-500 mt-0.5">Survives prerender only (default)</p>
            </div>
            <div class="bg-gray-50 rounded-lg p-3">
                <p class="font-medium text-slate-700">ServerMemoryCache</p>
                <p class="text-xs text-slate-500 mt-0.5">Survives page reload (server only)</p>
            </div>
            <div class="bg-indigo-50 rounded-lg p-3 border border-indigo-200">
                <p class="font-medium text-indigo-700">LocalStorage <span class="text-xs">(active)</span></p>
                <p class="text-xs text-indigo-500 mt-0.5">Survives browser restart</p>
            </div>
            <div class="bg-gray-50 rounded-lg p-3">
                <p class="font-medium text-slate-700">SessionStorage</p>
                <p class="text-xs text-slate-500 mt-0.5">Survives refresh, clears on tab close</p>
            </div>
        </div>
    </div>
</div>

<StateInspector Entries="InspectorEntries" />
```

- [ ] **Step 3: Build**

```bash
dotnet build TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add Settings page with LocalStorage strategy and theme controls"
```

---

### Task 9: Final Integration and Cleanup

**Files:**
- Modify: Various files for build fixes
- Modify: `TheBlazorState.Tests/PrerenderIntegrationTests.cs` (update routes)

- [ ] **Step 1: Build the full solution**

```bash
dotnet build TheBlazorState.slnx
```

Fix any compilation errors. Common issues:
- Missing usings in pages
- Type mismatches between models and services
- NavLink ActiveClass not applying (may need CSS tweak)

- [ ] **Step 2: Run all tests**

```bash
dotnet test TheBlazorState.slnx
```

The `PrerenderIntegrationTests` hit `/counter` and `/weather` which no longer exist. Update them to hit `/` (Dashboard) and `/board` instead. Verify they still find the prerender state marker in HTML.

- [ ] **Step 3: Run the demo**

```bash
dotnet run --project TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

Verify in browser:
- Dashboard loads with stat cards
- Switching projects in sidebar updates dashboard
- Board shows Kanban columns with task cards
- Moving tasks between columns works
- Settings toggles work
- State inspector shows correct Meta values
- Refreshing a page restores state (WasRestored = true)

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: complete TaskFlow demo with all TheBlazorState features"
```

---

## Task Dependency Summary

```
Task 1 (clean + tailwind) → Task 2 (models + services) → Task 3 (state + program)
    → Task 4 (layout + sidebar) → Task 5 (inspector) → Task 6 (dashboard)
    → Task 7 (board) → Task 8 (settings) → Task 9 (integration)
```

All tasks are sequential. Each produces a committable increment.
