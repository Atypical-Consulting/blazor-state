# BlazorStatePlus

Ergonomic helpers for Blazor prerendered state persistence (.NET 10+).

Wraps `PersistentComponentState` with a friendlier API that eliminates boilerplate, adds change notifications, staleness detection, and state grouping.

## Setup

```csharp
// Program.cs
builder.Services.AddBlazorStatePlus();
```

## Quick comparison

### Before (raw Blazor API)

```csharp
@page "/weather"
@implements IDisposable
@inject PersistentComponentState ApplicationState

<p>@forecasts?.Length forecasts loaded</p>

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
@page "/weather"
@inherits PersistentComponentBase

<p>@Forecasts.Value?.Length forecasts loaded</p>

@code {
    private IStateSlice<WeatherForecast[]?> Forecasts = default!;

    protected override async Task OnInitializedAsync()
    {
        Forecasts = await State.CreateAndInitAsync<WeatherForecast[]?>(
            "forecasts",
            () => Http.GetFromJsonAsync<WeatherForecast[]>("/api/weather"));
    }
}
```

No `IDisposable`, no manual `TryTakeFromJson`, no `RegisterOnPersisting` callback. The base class handles it all.

---

## Core concepts

### 1. StateSlice<T>

A reactive wrapper around a single persistent value.

```csharp
@inherits PersistentComponentBase

@code {
    private IStateSlice<int> Counter = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // UseSlice creates, restores, and subscribes to changes in one call.
        // The factory is only invoked when no restored value exists.
        Counter = UseSlice("counter", () => Random.Shared.Next(100));
    }

    private void Increment() => Counter.Value++;
    // StateHasChanged is called automatically when Value changes.
}
```

Key properties:
- `Value` — get/set the current value
- `WasRestored` — true if the value came from prerender
- `IsDirty` — true if the value was modified since restore
- `IsStale` — true if the value has exceeded its TTL
- `OnChanged` — event fired on every change

### 2. Staleness / TTL

Flag state as stale after a duration, prompting a re-fetch:

```csharp
protected override async Task OnInitializedAsync()
{
    base.OnInitialized();

    var prices = UseSlice<PriceData[]>("prices", configure: o =>
    {
        o.TimeToLive = TimeSpan.FromMinutes(5);
    });

    // InitializeIfNeededAsync skips the call when restored AND not stale.
    // If TTL expired, the factory runs again.
    await prices.InitializeIfNeededAsync(
        () => PricingService.GetCurrentPricesAsync());
}
```

### 3. State groups

Bundle related properties into one serialization unit to avoid multiple keys:

```csharp
public class ProductPageState : IStateGroup
{
    public ProductDetail? Product { get; set; }
    public ReviewSummary? Reviews { get; set; }
    public bool IsInWishlist { get; set; }
}

@code {
    private IStateSlice<ProductPageState> PageState = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // One key, one serialization, all three properties restored together.
        PageState = UseGroup<ProductPageState>("product-page");
    }
}
```

### 4. Using StateManager directly (without the base class)

If you can't inherit from `PersistentComponentBase`, inject `StateManager`:

```csharp
@implements IDisposable
@inject StateManager State

@code {
    private IStateSlice<string> UserName = default!;

    protected override void OnInitialized()
    {
        UserName = State.CreateSlice("username", "Anonymous");
    }

    void IDisposable.Dispose() => State.Dispose();
}
```

---

## API reference

### StateManager

| Method | Description |
|--------|-------------|
| `CreateSlice<T>(key, default?, configure?)` | Create a slice, restore if available |
| `CreateAndInit<T>(key, factory, configure?)` | Create + sync initialize if not restored |
| `CreateAndInitAsync<T>(key, factory, configure?)` | Create + async initialize if not restored |
| `CreateGroup<T>(key, default?, configure?)` | Create a group slice (single serialization unit) |

### PersistentComponentBase

| Method | Description |
|--------|-------------|
| `UseSlice<T>(key, default?, configure?)` | CreateSlice + auto StateHasChanged on change |
| `UseSlice<T>(key, factory, configure?)` | CreateAndInit + auto StateHasChanged |
| `UseGroup<T>(key, default?, configure?)` | CreateGroup + auto StateHasChanged |

### StateSliceOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Key` | parameter name | Override the persistence key |
| `TimeToLive` | `null` | Duration before `IsStale` returns true |
| `AllowUpdatesOnNavigation` | `false` | Accept new values during enhanced nav |
