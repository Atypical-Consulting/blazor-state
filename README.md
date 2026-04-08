# TheBlazorState

[![CI](https://github.com/Atypical-Consulting/TheBlazorState/actions/workflows/ci.yml/badge.svg)](https://github.com/Atypical-Consulting/TheBlazorState/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Two attributes. Zero boilerplate. State that just works.**

TheBlazorState is a source-generator-powered state management library for Blazor. Drop `[Persist]` and `[Shared]` on your properties — the generator handles serialization, restoration, change tracking, cross-tab sync, and component re-rendering. No actions, no reducers, no dispatch.

---

## The Problem

Blazor's prerender-to-interactive transition **loses all component state**. Developers end up writing repetitive plumbing: manual serialization, cache keys, lifecycle hooks, event subscriptions, disposal cleanup. Every. Single. Component.

Cross-tab synchronization? That's another layer of SignalR hubs, localStorage listeners, and careful thread-safety on top.

## The Solution

```csharp
[Persist(TimeToLive = "00:05:00")]
[Shared]
public partial int Count { get; set; }
```

That's it. The source generator emits everything else:
- Backing field and property implementation with change detection
- `StateMeta` companion (`WasRestored`, `IsDirty`, `IsStale`, `LastUpdated`)
- Lifecycle wiring (`OnInitialized` / `Dispose`)
- Automatic component re-rendering on state changes
- Cross-tab synchronization when using browser storage

---

## Why Not Redux / Fluxor?

Redux-inspired libraries (Fluxor, Blazor-State, etc.) bring a **JavaScript ecosystem pattern** into C#. They work — but they fight the language instead of embracing it.

| | Redux / Fluxor | TheBlazorState |
|---|---|---|
| **Mental model** | Actions → Reducers → Store → Subscribe | Properties. Just properties. |
| **To change state** | Create action record, write reducer method, dispatch | `state.Count++` |
| **To read state** | Inject `IState<T>`, subscribe, call `StateHasChanged` | `@inject AppState` — done |
| **Boilerplate per feature** | Action + Reducer + Effect + Feature class | One `[Shared]` attribute |
| **Persistence** | Manual middleware or side-effects | `[Persist]` — built in |
| **Cross-tab sync** | Build it yourself | Built in with `LocalStorage` strategy |
| **Compile-time safety** | String-based action matching, runtime errors | Source generators catch errors at build time |
| **Learning curve** | Flux architecture, middleware, effects, selectors | Two attributes and a partial method |

### The philosophy

Redux was designed for JavaScript — a language without strong typing, interfaces, or events. C# already has all of these. TheBlazorState leans into what C# is good at:

- **Partial properties** — the compiler enforces the contract, the generator fills in the implementation
- **Source generators** — zero runtime reflection, zero magic strings, full IDE support with go-to-definition
- **`INotifyStateChanged`** — standard .NET observer pattern instead of action/reducer indirection
- **Attribute composition** — `[Persist] [Shared]` reads like intent, not ceremony

The result: state management that feels like writing normal C# properties, because it *is* normal C# properties. The complexity lives in the generator, not in your code.

> **"The best architecture is the one you don't notice."**

---

## Quick Start

### 1. Install

```bash
dotnet add package TheBlazorState
```

### 2. Register services

```csharp
builder.Services.AddTheBlazorState();
```

### 3. Define shared state

```csharp
public partial class AppState
{
    [Shared]
    public partial string? UserName { get; set; }

    [Shared]
    public partial int NotificationCount { get; set; }
}
```

### 4. Use in any component

```razor
@inject AppState App

<h1>Welcome, @App.UserName!</h1>
<span class="badge">@App.NotificationCount</span>
<button @onclick="() => App.NotificationCount++">+1</button>
```

Change `NotificationCount` anywhere — every component that injects `AppState` re-renders automatically.

### 5. Add persistence

```csharp
public partial class Dashboard : ComponentBase
{
    [Persist(TimeToLive = "00:02:00")]
    public partial DashboardData? Stats { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.Stats
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(async () => await StatsService.GetDashboardAsync(Project.SelectedProject.Id));
    }
}
```

On first render, `Stats` loads from the async factory. On subsequent navigations within the TTL window, it restores instantly from cache. After TTL expires, the factory is called again.

---

## Before & After

<table>
<tr><th>Traditional Blazor</th><th>TheBlazorState</th></tr>
<tr>
<td>

```csharp
// Define state class
// Define action records
// Define reducer methods
// Wire up IDispatcher
// Subscribe in OnInitialized
// Call StateHasChanged manually
// Unsubscribe in Dispose
// Serialize to cache manually
// Deserialize on restore
// Handle cross-tab with SignalR
// ~80 lines of plumbing
```

</td>
<td>

```csharp
[Persist]
[Shared]
public partial int Count { get; set; }
// Done. 3 lines.
```

</td>
</tr>
</table>

---

## Storage Strategies

Choose where state lives — per property, if needed:

| Strategy | Survives | Best For |
|---|---|---|
| **PrerenderHtml** *(default)* | Page load | SSR hydration, zero JS calls |
| **ServerMemoryCache** | Server restart | Server-side sessions |
| **LocalStorage** | Browser restart | User preferences, cross-tab sync |
| **SessionStorage** | Tab close | Session-scoped data |
| **IndexedDB** | Browser restart | Large datasets (>5 MB) |

```csharp
partial void ConfigureState(__StateContext ctx)
{
    ctx.Theme.Storage = StorageStrategy.LocalStorage();
    ctx.DraftEmail.Storage = StorageStrategy.SessionStorage();
    ctx.LargeDataset.Storage = StorageStrategy.IndexedDb();
}
```

## StateMeta: Built-in Observability

Every `[Persist]` property gets a companion `Meta` object — no extra code needed:

```csharp
// Was this value restored from cache or freshly loaded?
if (StatsMeta.WasRestored) { /* skip expensive reload */ }

// Has the user modified anything?
if (FormDataMeta.IsDirty) { /* show "unsaved changes" warning */ }

// Has the TTL expired?
if (StatsMeta.IsStale) { Stats = await Reload(); }

// Audit trail: last 10 changes with timestamps and source
foreach (var entry in CountMeta.ChangeLog)
    Console.WriteLine($"{entry.Timestamp}: {entry.OldValue} → {entry.NewValue} ({entry.Source})");
```

## Cross-Tab Sync

Properties using `LocalStorage` strategy automatically sync across browser tabs:

```
Tab A: user clicks "Add to cart"
  → CartState.Items updated
  → Written to localStorage
  → CrossTabHub notifies all circuits
Tab B: cart badge updates instantly
```

No extra configuration. Inherit from `StateComponentBase` to opt in to cross-tab re-renders.

---

## Documentation

- [Getting Started](docs/getting-started.md) — Installation, setup, and your first stateful component
- [The `[Persist]` Attribute](docs/persist-attribute.md) — Persistence, TTL, ConfigureState, and StateMeta
- [The `[Shared]` Attribute](docs/shared-attribute.md) — Reactive state and auto-subscription
- [Storage Strategies](docs/storage-strategies.md) — Built-in strategies and custom implementations
- [Cross-Tab Sync](docs/cross-tab-sync.md) — Real-time synchronization across browser tabs
- [Source Generators](docs/source-generators.md) — What gets generated, diagnostics, and troubleshooting

---

## Project Structure

| Project | Description |
|---|---|
| **TheBlazorState** | Core library — attributes, services, storage strategies |
| **TheBlazorState.Generators** | Roslyn incremental source generators |
| **TheBlazorState.Tests** | Runtime and bUnit integration tests |
| **TheBlazorState.Generators.Tests** | Generator diagnostic and output snapshot tests |
| **TheBlazorState.Demo** | TaskFlow demo app showcasing all features |

## Requirements

- .NET 10 (Preview or later)
- Blazor Server or Blazor WebAssembly

## Contributing

Contributions are welcome! Please open an issue first to discuss what you'd like to change.

## License

[MIT](LICENSE)
