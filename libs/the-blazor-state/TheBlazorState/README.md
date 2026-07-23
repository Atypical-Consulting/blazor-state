# TheBlazorState

Ergonomic state management for Blazor. Two attributes -- `[Persist]` for prerender-to-interactive persistence and `[Shared]` for cross-component reactive state -- cover the most common state challenges in Blazor apps. A Roslyn source generator handles all the wiring: no boilerplate, no base classes, no ceremony.

## Quick Start

```csharp
// Program.cs
builder.Services.AddTheBlazorState();
```

```csharp
// Counter.razor.cs
public partial class Counter : ComponentBase
{
    [Persist]
    public partial int Count { get; set; }

    private void Increment() => Count++;
}
```

That is the entire setup. The source generator emits the backing field, persistence hooks, lifecycle overrides, and disposal logic.

## Before / After

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

### After (with TheBlazorState)

```csharp
public partial class Weather : ComponentBase
{
    [Inject] private WeatherService WeatherSvc { get; set; } = default!;

    [Persist(TimeToLive = "00:05:00")]
    public partial WeatherForecast[]? Forecasts { get; set; }

    partial void ConfigureState(StateContext ctx)
    {
        ctx.Forecasts.LoadFrom(() => WeatherSvc.GetForecastAsync());
    }
}
```

No `IDisposable`, no manual `TryTakeFromJson`, no `RegisterOnPersisting` callback.

## `[Persist]` -- Prerender-to-Interactive Persistence

### Simple Property

```csharp
public partial class Counter : ComponentBase
{
    [Persist]
    public partial int Count { get; set; }
}
```

### With TTL

Mark data as stale after a duration. When the value exceeds its TTL, the `LoadFrom` factory runs instead of using the restored value:

```csharp
[Persist(TimeToLive = "00:05:00")]
public partial Product? Product { get; set; }
```

### Async Factory via ConfigureState

The `ConfigureState` partial method provides a `StateContext` for runtime configuration. `LoadFrom` supplies an async factory that runs only when no fresh value was restored:

```csharp
partial void ConfigureState(StateContext ctx)
{
    ctx.Product.LoadFrom(() => ProductService.GetAsync(ProductId));
}
```

### With `[Parameter]`

A property can be both a Blazor parameter and persisted. The persisted value is available immediately when interactive mode starts, before the parent re-renders:

```csharp
[Persist, Parameter]
public partial string? Theme { get; set; }
```

### Meta Companion

Every `[Persist]` property `Foo` gets a generated read-only companion `FooMeta`:

| Property | Type | Description |
|----------|------|-------------|
| `WasRestored` | `bool` | `true` if the value was restored from persistence |
| `IsDirty` | `bool` | `true` if the value was modified after initialization |
| `IsStale` | `bool` | `true` if the value has exceeded its TTL |
| `LastUpdated` | `DateTimeOffset` | UTC timestamp of last value change |
| `OnChanged` | `Action` | Event fired on every value change |

```razor
@if (ProductMeta.WasRestored)
{
    <span>Cached -- @ProductMeta.LastUpdated.LocalDateTime</span>
}
@if (ProductMeta.IsStale)
{
    <button @onclick="Refresh">Data is stale -- refresh</button>
}
```

## `[Shared]` -- Cross-Component Reactive State

Define a state class with `[Shared]` properties. Any component injecting the class re-renders automatically when a property changes.

### Define State

```csharp
public partial class CartState
{
    [Shared]
    public partial List<CartItem> Items { get; set; } = [];

    [Shared]
    public partial decimal Total { get; set; }

    public void AddItem(CartItem item)
    {
        Items = [..Items, item];
        Total = Items.Sum(i => i.Price * i.Quantity);
    }
}
```

Shared state classes are registered as scoped services automatically (one instance per circuit on Server, one per tab on WASM).

### Inject and Use

```razor
@inject CartState Cart

<span>Items: @Cart.Items.Count</span>
<span>Total: @Cart.Total.ToString("C")</span>
<button @onclick="() => Cart.AddItem(item)">Add</button>
```

No manual subscriptions. The generator wires `OnChanged` from each shared property to `StateHasChanged()` on consuming components and disposes subscriptions automatically.

## `[Shared, Persist]` -- Composition

The two attributes compose naturally. This means reactive across components AND survives browser refresh:

```csharp
public partial class UserPrefsState
{
    [Shared, Persist(TimeToLive = "24:00:00")]
    public partial string Theme { get; set; } = "light";

    partial void ConfigureState(StateContext ctx)
    {
        ctx.Theme.Storage = StorageStrategy.LocalStorage();
    }
}
```

## Storage Strategies

The default storage uses the prerendered HTML (same as raw `PersistentComponentState`). Other strategies extend persistence beyond the prerender handoff.

| Strategy | Survives prerender | Survives refresh | Survives session | Blazor Server | Blazor WASM |
|----------|:-:|:-:|:-:|:-:|:-:|
| `StorageStrategy.PrerenderHtml()` | yes | no | no | yes | yes |
| `StorageStrategy.ServerMemoryCache()` | yes | yes* | no | yes | no |
| `StorageStrategy.SessionStorage()` | yes | yes | no | no | yes |
| `StorageStrategy.LocalStorage()` | yes | yes | yes | no | yes |
| `StorageStrategy.IndexedDb()` | yes | yes | yes | no | yes |

\* with sliding expiration

Storage is resolved in priority order: **Property-level > Component/StateClass-level > Global > PrerenderHtml()**.

### Global Default

```csharp
builder.Services.AddTheBlazorState(options =>
{
    options.DefaultStorage = StorageStrategy.LocalStorage();
});
```

### Custom Strategies

Implement `IStorageStrategy` and register:

```csharp
builder.Services.AddTheBlazorState(options =>
{
    options.AddStorage<RedisStorageStrategy>("redis");
});

// In ConfigureState:
ctx.Product.Storage = StorageStrategy.Custom("redis");
```

## ConfigureState Reference

The `ConfigureState` partial method provides a `StateContext` with per-property configuration:

| Method / Property | Description |
|-------------------|-------------|
| `LoadFrom(Func<Task<T>> factory)` | Async factory called during `OnInitializedAsync` when value was not restored or is stale |
| `KeySuffix(params object[] parts)` | Appends dynamic segments to the auto-derived storage key |
| `KeyOverride(string key)` | Replaces the auto-derived storage key entirely |
| `Storage` | Sets the storage strategy for this property (overrides component/global default) |

Component-level storage can be set on the context itself:

```csharp
partial void ConfigureState(StateContext ctx)
{
    ctx.Storage = StorageStrategy.SessionStorage(); // applies to all [Persist] in this component
    ctx.Product.LoadFrom(() => ProductService.GetAsync(ProductId));
    ctx.Product.KeySuffix(ProductId);
}
```

## Diagnostics

The source generator emits compile-time diagnostics:

| Code | Severity | Description |
|------|----------|-------------|
| `TBS001` | Error | `[Persist]` or `[Shared]` on non-partial property |
| `TBS002` | Error | `[Persist]` or `[Shared]` on non-partial class |
| `TBS003` | Warning | `[Persist]` on a shared state class property without `[Shared]` (probably a mistake) |
| `TBS004` | Error | Invalid `TimeToLive` format |
| `TBS005` | Warning | `ConfigureState` references a property without `[Persist]` or `[Shared]` |

## Security

Values persisted to browser storage (LocalStorage, SessionStorage, IndexedDb) or prerender HTML are visible to the user in the page source or browser developer tools. **Do not persist sensitive data** (auth tokens, passwords, PII, role claims) without a custom encrypted storage strategy.

## Requirements

- .NET 10+
- Blazor Server, Blazor WebAssembly, or Blazor United (Auto render mode)
