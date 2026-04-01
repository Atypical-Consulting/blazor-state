# BlazorStatePlus

Zero-boilerplate prerender-to-interactive state handoff for Blazor components (.NET 10+).

> **What this is:** A source-generator-powered library that wraps `PersistentComponentState` to eliminate the manual `TryTakeFromJson` / `RegisterOnPersisting` / `IDisposable` ceremony.
>
> **What this is NOT:** This library does **not** persist state across page refreshes, navigation, browser sessions, or server restarts. It handles the prerender-to-interactive handoff only — the moment between when the server renders HTML and when the interactive runtime takes over.

## Setup

```csharp
// Program.cs
builder.Services.AddBlazorStatePlus();
```

## Quick Comparison

### Before (raw Blazor API)

```csharp
@page "/weather"
@implements IDisposable
@inject PersistentComponentState ApplicationState

@code {
    private WeatherForecast[]? forecasts;
    private PersistingComponentStateSubscription sub;

    protected override async Task OnInitializedAsync()
    {
        if (!ApplicationState.TryTakeFromJson<WeatherForecast[]>(
                "forecasts", out var restored))
        {
            forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("/api/weather");
        }
        else
        {
            forecasts = restored;
        }

        sub = ApplicationState.RegisterOnPersisting(() =>
        {
            ApplicationState.PersistAsJson("forecasts", forecasts);
            return Task.CompletedTask;
        });
    }

    void IDisposable.Dispose() => sub.Dispose();
}
```

### After (with BlazorStatePlus)

```csharp
public partial class Weather : ComponentBase
{
    [Inject] private WeatherService WeatherSvc { get; set; } = null!;

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<WeatherForecast[]> _forecasts = null!;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Forecasts.InitializeFrom(() => WeatherSvc.GetForecastAsync());
    }
}
```

No `IDisposable`, no manual `TryTakeFromJson`, no `RegisterOnPersisting` callback. The source generator handles it all.

## How It Works

1. Mark fields with `[Slice]` on a `partial class` that extends `ComponentBase`
2. The source generator emits `OnInitialized` / `OnInitializedAsync` / `Dispose` overrides
3. On prerender: `StateManager` registers an `OnPersisting` callback that serializes each slice's value as JSON into the prerendered HTML
4. On interactive boot: `StateManager` calls `TryTakeFromJson` to restore each slice's value from the embedded JSON

## Core Concepts

### The `[Slice]` Attribute

Marks a field of type `IStateSlice<T>` for automatic wiring:

```csharp
public partial class Counter : ComponentBase
{
    [Slice]
    private IStateSlice<int> _counter = null!;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Counter.DefaultValue(Random.Shared.Next(100));
    }

    private void Increment() => _counter.Value++;
}
```

### Runtime Configuration via `OnInitializeSlices`

The generated `SliceInitContext` provides a fluent builder per field:

```csharp
partial void OnInitializeSlices(SliceInitContext ctx)
{
    ctx.Page
       .KeySuffix(ProductId)          // Dynamic key: "ProductDetail.page:42"
       .InitializeFrom(async () =>    // Async factory (skipped if restored)
           new ProductPageState
           {
               Product = await Products.GetAsync(ProductId),
               Reviews = await Reviews.GetSummaryAsync(ProductId)
           });
}
```

**Builder methods:**
| Method | Description |
|--------|-------------|
| `DefaultValue(T value)` | Fallback value when nothing is restored |
| `KeySuffix(params object[] parts)` | Append dynamic segments to the auto-derived key |
| `KeyOverride(string key)` | Replace the auto-derived key entirely |
| `InitializeFrom(Func<Task<T>> factory)` | Async factory called only when no restored value exists (or when stale) |

### Staleness / TTL

Flag state as stale after a duration. When the prerendered data is older than the TTL (e.g., cached by a CDN or delayed by a slow connection), the library falls back to the default and the `InitializeFrom` factory runs instead:

```csharp
[Slice(TimeToLive = "00:05:00")]
private IStateSlice<WeatherForecast[]> _forecasts = null!;
```

### IStateSlice&lt;T&gt; Properties

| Property | Description |
|----------|-------------|
| `Value` | Get/set the current value. Fires `OnChanged` on mutation. |
| `WasRestored` | `true` if the value was restored from prerendered state |
| `IsDirty` | `true` if the value was modified after creation |
| `IsStale` | `true` if the value has exceeded its configured TTL |
| `LastUpdated` | UTC timestamp of last value change |
| `OnChanged` | Event fired on every value change |
| `InitializeIfNeeded(T)` | Sets value only if not restored (sync) |
| `InitializeIfNeededAsync(Func<Task<T>>)` | Calls factory only if not restored (async) |

### Direct `StateManager` Usage

If you cannot use `[Slice]` (e.g., non-partial class), inject `StateManager` directly:

```csharp
@inject StateManager State
@implements IDisposable

@code {
    private IStateSlice<string> UserName = default!;

    protected override void OnInitialized()
    {
        UserName = State.CreateSlice("username", "Anonymous");
    }

    void IDisposable.Dispose() => State.Dispose();
}
```

## Security

Slice values are serialized as JSON into the prerendered HTML response and are visible in the page source. **Do not store sensitive data** (auth tokens, PII, secrets, role information) in state slices.

## Diagnostics

The source generator emits compile-time diagnostics:

| ID | Severity | Description |
|---|---|---|
| BSP001 | Error | `[Slice]` on non-partial class |
| BSP002 | Error | Field type is not `IStateSlice<T>` |
| BSP003 | Error | Class doesn't inherit `ComponentBase` |
| BSP005 | Error | Invalid `TimeToLive` format |
| BSP006 | Warning | Class already implements `IDisposable` — call `__DisposeSlices()` manually |
| BSP007 | Error | Duplicate slice keys |
| BSP008 | Error | `[Slice]` on static field |
| BSP011 | Warning | Class overrides `OnInitialized` — use `OnAfterSlicesCreated` instead |
| BSP012 | Warning | Field has initializer (will be overwritten by generator) |
