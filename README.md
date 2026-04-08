# TheBlazorState

[![CI](https://github.com/Atypical-Consulting/TheBlazorState/actions/workflows/ci.yml/badge.svg)](https://github.com/Atypical-Consulting/TheBlazorState/actions/workflows/ci.yml)

Ergonomic state management for Blazor with source generators. Persist state across prerender-to-interactive transitions, share reactive state between components, and sync across browser tabs — all with simple attributes on partial properties.

## Features

- **`[Persist]`** — Survive prerender-to-interactive transitions with automatic serialization, optional TTL, and pluggable storage strategies (memory, browser localStorage, server cache)
- **`[Shared]`** — Reactive state classes that auto-notify injected components on property changes
- **Source generators** — Zero boilerplate: the generator emits property backing fields, `StateMeta` companions, and lifecycle wiring
- **Cross-tab sync** — Real-time state synchronization across browser tabs via `localStorage` events and SignalR
- **Pluggable storage** — Built-in strategies for memory, browser storage (`localStorage`/`sessionStorage`), and server-side `IMemoryCache`
- **Auto-subscription** — Components automatically re-render when injected `[Shared]` state changes — no manual `StateHasChanged()` calls

## Quick start

### 1. Install

```bash
dotnet add package TheBlazorState
```

### 2. Register services

```csharp
builder.Services.AddTheBlazorState(options =>
{
    options.DefaultStrategy = StorageStrategy.Browser;
});
```

### 3. Define state with attributes

```csharp
public partial class CounterState : SharedStateBase
{
    [Persist]
    [Shared]
    public partial int Count { get; set; }

    [Persist(TimeToLive = "00:05:00")]
    [Shared]
    public partial string? UserName { get; set; }
}
```

### 4. Use in components

```razor
@inject CounterState Counter

<p>Count: @Counter.Count</p>
<button @onclick="() => Counter.Count++">Increment</button>
```

Components automatically re-render when `[Shared]` properties change.

## Project structure

| Project | Description |
|---|---|
| `TheBlazorState` | Core library — attributes, services, storage strategies |
| `TheBlazorState.Generators` | Roslyn incremental source generators for `[Persist]` and `[Shared]` |
| `TheBlazorState.Tests` | Runtime and integration tests |
| `TheBlazorState.Generators.Tests` | Source generator diagnostic and output tests |
| `TheBlazorState.Demo` | TaskFlow demo app showcasing all features |

## Requirements

- .NET 10 Preview (or later)
- Blazor Server or Blazor WebAssembly

## License

MIT
