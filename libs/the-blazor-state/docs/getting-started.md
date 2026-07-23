# Getting Started

This guide walks you through adding TheBlazorState to a Blazor project and building your first stateful component.

## Prerequisites

- .NET 10 SDK (Preview or later)
- A Blazor Server or Blazor WebAssembly project

## Installation

```bash
dotnet add package TheBlazorState
```

The NuGet package includes both the runtime library and the source generators — no separate analyzer package needed.

## Register Services

In your `Program.cs`, call `AddTheBlazorState()`:

```csharp
builder.Services.AddTheBlazorState();
```

This registers:
- `StateManager` — orchestrates persistence and restoration
- `CrossTabHub` — server-side pub/sub for cross-tab sync
- `CrossTabSyncService` — client-side listener for storage events
- `BrowserStorageService` — JS interop for browser storage

### With options

```csharp
builder.Services.AddTheBlazorState(options =>
{
    // Change the default storage strategy (default is PrerenderHtml)
    options.DefaultStorage = StorageStrategy.ServerMemoryCache();
});
```

## Your First Shared State

Create a state class with `[Shared]` properties. The class and properties must be `partial`:

```csharp
using TheBlazorState.Attributes;

public partial class CounterState
{
    [Shared]
    public partial int Count { get; set; }

    [Shared]
    public partial string? Message { get; set; }
}
```

Register it as a scoped service:

```csharp
builder.Services.AddScoped<CounterState>();
```

### Use it in a component

```razor
@page "/counter"
@inject CounterState Counter

<h1>Counter: @Counter.Count</h1>
<p>@Counter.Message</p>

<button @onclick="Increment">Increment</button>
<button @onclick="() => Counter.Message = $\"Count is {Counter.Count}\"">Update Message</button>

@code {
    private void Increment() => Counter.Count++;
}
```

That's it. Any other component that injects `CounterState` will automatically re-render when `Count` or `Message` changes. No manual subscriptions, no `StateHasChanged()` calls.

## Adding Persistence

To survive prerender-to-interactive transitions, add `[Persist]` to component properties:

```csharp
public partial class CounterPage : ComponentBase
{
    [Inject] public CounterState Counter { get; set; } = null!;

    [Persist]
    public partial int VisitCount { get; set; }
}
```

The `VisitCount` property is now automatically:
1. Saved to the prerender cache during SSR
2. Restored when the component becomes interactive
3. Tracked with a `VisitCountMeta` companion for observability

### With async data loading

```csharp
public partial class Dashboard : ComponentBase
{
    [Inject] private DataService Data { get; set; } = null!;

    [Persist(TimeToLive = "00:05:00")]
    public partial DashboardData? Stats { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.Stats.LoadFrom(async () => await Data.GetStatsAsync());
    }
}
```

The `ConfigureState` partial method is your hook for advanced configuration. Here, `LoadFrom` provides an async factory that runs on first load and whenever the TTL expires.

## What Happens Under the Hood

When you add `[Persist]` or `[Shared]`, the source generator creates:

1. **Backing field** — stores the actual value
2. **Property implementation** — getter/setter with change detection
3. **StateMeta companion** — `{PropertyName}Meta` with `WasRestored`, `IsDirty`, `IsStale`, `LastUpdated`, `ChangeLog`
4. **Lifecycle hooks** — `OnInitialized` / `OnInitializedAsync` / `Dispose` overrides
5. **Subscription wiring** — for `[Shared]` state, components auto-subscribe via `InjectSubscriptionGenerator`

You never write this plumbing. The generator does.

## Next Steps

- **[The `[Persist]` Attribute](persist-attribute.md)** — TTL, storage strategies, ConfigureState, StateMeta
- **[The `[Shared]` Attribute](shared-attribute.md)** — Reactive state patterns and auto-subscription
- **[Storage Strategies](storage-strategies.md)** — Choose where state lives
- **[Cross-Tab Sync](cross-tab-sync.md)** — Real-time synchronization across browser tabs
- **[Source Generators](source-generators.md)** — Diagnostics, generated code, troubleshooting
