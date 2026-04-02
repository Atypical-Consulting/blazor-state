# TheBlazorState — Design Specification

**Date:** 2026-04-02  
**Status:** Draft  
**Supersedes:** `2026-04-01-source-generator-design.md` (BlazorStatePlus)

## Overview

TheBlazorState is a source-generator-powered state management library for Blazor. It eliminates boilerplate for two common problems:

1. **Prerender-to-interactive state persistence** — expensive data fetched during server prerender should not be re-fetched when the component goes interactive.
2. **Cross-component reactive state** — when one component changes a value, other components that use it should re-render automatically.

The library uses two attributes (`[Persist]` and `[Shared]`) on `partial` properties. A Roslyn incremental source generator emits all wiring code. No actions, reducers, middleware, or global singleton store.

## Design Principles

- **Natural C#** — state is just properties. No `.Value` wrappers, no dispatch, no ceremony.
- **Simple and focused** — solves persistence and shared state. Nothing more.
- **Extensible via composition** — storage strategies and companion packages, not core complexity.
- **YAGNI** — no features beyond what developers actually need.

## Public API

### Registration

```csharp
// Program.cs
builder.Services.AddTheBlazorState();

// With global storage default
builder.Services.AddTheBlazorState(options =>
{
    options.DefaultStorage = StorageStrategy.LocalStorage();
});
```

### Attribute: `[Persist]`

Marks a component property whose value should survive a state transition (prerender-to-interactive, page refresh, browser session — depending on storage strategy).

Requires `partial` on the property and `partial` on the containing class.

```csharp
public partial class ProductDetail : ComponentBase
{
    [Parameter]
    public int ProductId { get; set; }

    [Persist(TimeToLive = "00:05:00")]
    public partial Product? Product { get; set; }

    [Persist, Parameter]
    public partial string? Theme { get; set; }
}
```

**Attribute parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TimeToLive` | `string` | `null` | Duration format (`"HH:mm:ss"`). When elapsed, the value is considered stale. |

**Works with `[Parameter]`:** A property can be both a Blazor parameter and persisted. During prerender the parent sets it; the persisted value is available immediately when interactive mode starts, before the parent re-renders.

### Attribute: `[Shared]`

Marks a property in a state class as reactive across components. Any component injecting the state class re-renders when the property changes.

```csharp
public partial class CartState
{
    [Shared]
    public partial List<CartItem> Items { get; set; } = [];

    [Shared]
    public partial decimal Total { get; set; }
}
```

**Composing `[Shared]` with `[Persist]`:**

A shared state property can also be persisted. The two attributes compose naturally:

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

This means: reactive across components AND survives browser refresh.

### Meta Companion

Every `[Persist]` or `[Shared]` property `Foo` gets a generated read-only companion property `FooMeta` exposing:

| Property | Type | Description |
|----------|------|-------------|
| `WasRestored` | `bool` | `true` if value was restored from persistence. Always `false` for `[Shared]`-only properties. |
| `IsDirty` | `bool` | `true` if value was modified after initialization. |
| `IsStale` | `bool` | `true` if value has exceeded its TTL. Always `false` when no TTL is set. |
| `LastUpdated` | `DateTimeOffset` | UTC timestamp of last value change. |
| `OnChanged` | `Action` | Event fired on every value change. |

**Usage:**

```razor
<h1>@Product?.Name</h1>
@if (ProductMeta.WasRestored)
{
    <span>Cached · @ProductMeta.LastUpdated</span>
}
@if (ProductMeta.IsStale)
{
    <button @onclick="Refresh">Data is stale — refresh</button>
}
```

### ConfigureState

A partial method discovered by the generator. Provides a `StateContext` for runtime configuration of annotated properties.

```csharp
partial void ConfigureState(StateContext ctx)
{
    ctx.Product.LoadFrom(() => ProductService.GetAsync(ProductId));
    ctx.Product.KeySuffix(ProductId);
    ctx.Product.Storage = StorageStrategy.SessionStorage();
}
```

**Available configuration per property:**

| Method / Property | Description |
|-------------------|-------------|
| `LoadFrom(Func<Task<T>> factory)` | Async factory called during `OnInitializedAsync` if value was not restored (or is stale). |
| `KeySuffix(params object[] parts)` | Appends dynamic segments to the auto-derived storage key. |
| `KeyOverride(string key)` | Replaces the auto-derived storage key entirely. |
| `Storage` | Sets the storage strategy for this property (overrides component/global default). |

**Component-level configuration:**

```csharp
partial void ConfigureState(StateContext ctx)
{
    ctx.Storage = StorageStrategy.SessionStorage(); // all [Persist] in this component
}
```

## Shared State Classes

### Registration

Shared state classes are detected by the source generator and registered as **scoped** services:
- Blazor Server: one instance per circuit
- Blazor WASM: one instance per tab

### Consumption

Inject and use. No subscriptions, no manual disposal:

```csharp
@inject CartState Cart

<span>Items: @Cart.Items.Count</span>
<span>Total: @Cart.Total</span>
```

The generator wires `OnChanged` from every injected shared state property to `StateHasChanged()` on the consuming component. Disposal of subscriptions is auto-generated.

### ConfigureState in State Classes

State classes support the same `ConfigureState` partial method:

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

## Storage Strategy System

### Interface

```csharp
public interface IStorageStrategy
{
    Task<StorageResult<T>> RestoreAsync<T>(string key);
    Task PersistAsync<T>(string key, T value, StorageMetadata metadata);
    Task RemoveAsync(string key);
}

public record StorageResult<T>(bool Found, T? Value, DateTimeOffset? PersistedAt);

public record StorageMetadata(string Key, TimeSpan? TimeToLive, DateTimeOffset Timestamp);
```

### Built-in Strategies

| Strategy | Survives prerender | Survives refresh | Survives session | Blazor Server | Blazor WASM |
|----------|:-:|:-:|:-:|:-:|:-:|
| `StorageStrategy.PrerenderHtml()` | yes | no | no | yes | yes |
| `StorageStrategy.ServerMemoryCache()` | yes | yes* | no | yes | no |
| `StorageStrategy.SessionStorage()` | yes | yes | no | no | yes |
| `StorageStrategy.LocalStorage()` | yes | yes | yes | no | yes |
| `StorageStrategy.IndexedDb()` | yes | yes | yes | no | yes |

*with sliding expiration

### Cascade Resolution

Storage strategy is resolved with the following priority:

```
Property-level  →  Component/StateClass-level  →  Global  →  PrerenderHtml()
```

If no storage is configured at any level, `PrerenderHtml()` is used as the default.

### Custom Strategies

Implement `IStorageStrategy` and register:

```csharp
builder.Services.AddTheBlazorState(options =>
{
    options.AddStorage<RedisStorageStrategy>("redis");
});

// Usage in ConfigureState:
ctx.Product.Storage = StorageStrategy.Custom("redis");
```

### Key Generation

Storage keys are derived automatically:

- Components: `{ComponentFullName}.{PropertyName}`
- State classes: `{StateClassName}.{PropertyName}`

Customizable via `KeySuffix(params object[] parts)` or `KeyOverride(string key)` in `ConfigureState`.

### Serialization

JSON via `System.Text.Json`. Configurable per strategy if needed.

## Source Generator

### Targets

**Component properties (`[Persist]`):**
- Emits backing field + property implementation with persistence hooks
- Emits `{Name}Meta` companion property
- Emits `OnInitialized` / `OnInitializedAsync` / `Dispose` lifecycle wiring
- Detects `[Parameter]` co-presence and handles accordingly
- Discovers `ConfigureState` partial method

**State class properties (`[Shared]`):**
- Emits backing field + property implementation with change notification
- Emits `{Name}Meta` companion property
- Implements `INotifyStateChanged` interface so components can subscribe
- When `[Shared, Persist]` are combined, emits both persistence + notification logic

### Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| `TBS001` | Error | `[Persist]` or `[Shared]` on non-partial property |
| `TBS002` | Error | `[Persist]` or `[Shared]` on non-partial class |
| `TBS003` | Warning | `[Persist]` on a shared state class property without `[Shared]` (probably a mistake) |
| `TBS004` | Error | Invalid `TimeToLive` format |
| `TBS005` | Warning | `ConfigureState` references a property that has no `[Persist]` or `[Shared]` attribute |

## Complete Example

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTheBlazorState(options =>
{
    options.DefaultStorage = StorageStrategy.PrerenderHtml();
});

var app = builder.Build();
app.Run();
```

### Shared State: CartState.cs

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

### Component: ProductDetail.razor.cs

```csharp
public partial class ProductDetail : ComponentBase
{
    [Parameter]
    public int ProductId { get; set; }

    [Persist(TimeToLive = "00:05:00")]
    public partial Product? Product { get; set; }

    [Inject]
    private ProductService ProductService { get; set; } = default!;

    [Inject]
    private CartState Cart { get; set; } = default!;

    partial void ConfigureState(StateContext ctx)
    {
        ctx.Product.LoadFrom(() => ProductService.GetAsync(ProductId));
        ctx.Product.KeySuffix(ProductId);
        ctx.Product.Storage = StorageStrategy.SessionStorage();
    }

    private void AddToCart()
    {
        if (Product is not null)
            Cart.AddItem(new CartItem(Product.Id, Product.Name, Product.Price, 1));
    }

    private async Task Refresh()
    {
        Product = await ProductService.GetAsync(ProductId);
    }
}
```

### Component: ProductDetail.razor

```razor
@page "/product/{ProductId:int}"

@if (Product is null)
{
    <p>Loading...</p>
}
else
{
    <h1>@Product.Name</h1>
    <p>@Product.Price.ToString("C")</p>

    @if (ProductMeta.WasRestored)
    {
        <span>Cached · @ProductMeta.LastUpdated.LocalDateTime</span>
    }
    @if (ProductMeta.IsStale)
    {
        <button @onclick="Refresh">Data is stale — refresh</button>
    }

    <button @onclick="AddToCart">Add to Cart</button>
    <span>Cart total: @Cart.Total.ToString("C")</span>
}
```

## Out of Scope

The following are deliberately excluded from TheBlazorState:

- **No actions/reducers/dispatch** — state mutation is direct property assignment
- **No middleware pipeline** — no interceptors, no logging chain
- **No time-travel debugging** — no history of state changes
- **No global singleton store** — shared state is scoped per circuit/tab
- **No server-to-client sync** — no SignalR push of state changes
- **No encryption at rest** — storage strategies store plain JSON

Any of these could be added later via `IStorageStrategy` extensions or companion packages without breaking the core API.

## Security Note

Values persisted to browser storage (LocalStorage, SessionStorage, IndexedDb) or prerender HTML are visible to the user. Do not persist sensitive data (tokens, passwords, PII) without a custom encrypted storage strategy.

## Migration from BlazorStatePlus

| BlazorStatePlus | TheBlazorState |
|-----------------|----------------|
| `[Slice]` | `[Persist]` |
| `IStateSlice<T>` | `partial` property (no wrapper type) |
| `_field.Value` | Direct property access |
| `SliceBuilder<T>` | `StateContext` in `ConfigureState` |
| `SliceInitContext` | `StateContext` |
| `OnInitializeSlices` | `ConfigureState` |
| `AddBlazorStatePlus()` | `AddTheBlazorState()` |
