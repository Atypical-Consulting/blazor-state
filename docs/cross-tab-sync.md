# Cross-Tab Sync

TheBlazorState automatically synchronizes state across browser tabs when using the `LocalStorage` strategy. No extra configuration needed — it works out of the box.

## How It Works

```
Tab A                          Server                         Tab B
  │                              │                              │
  │  user clicks "Add to cart"   │                              │
  │  Cart.ItemCount = 5          │                              │
  │                              │                              │
  ├─► localStorage.setItem()     │                              │
  ├─► CrossTabHub.Publish() ────►├─► Notify subscribed circuits │
  │                              │                              │
  │                              ├──────────────────────────────►│
  │                              │    OnStorageChanged("cart", 5)│
  │                              │                              │
  │                              │    Cart.ItemCount = 5        │
  │                              │    StateHasChanged()         │
  │                              │    Component re-renders      │
```

### The sync pipeline

1. **Property changes** — a `[Persist]` property with `LocalStorage` strategy is modified
2. **Eager persist** — the value is immediately written to `localStorage` via JS interop
3. **Hub publish** — `CrossTabHub` broadcasts the change to all other circuits (server-side pub/sub)
4. **Circuit receives** — the `CrossTabSyncService` in the other tab picks up the notification
5. **Suppress echo** — `StateMeta.SuppressPersist` is set to `true` to prevent the receiving tab from re-publishing the same change
6. **Value update** — the property is updated, triggering a re-render
7. **Change logged** — the change is recorded in `StateMeta.ChangeLog` with `ChangeSource.CrossTab`

### Two sync channels

TheBlazorState uses two complementary channels:

| Channel | Mechanism | When |
|---|---|---|
| **CrossTabHub** | Server-side `ConcurrentDictionary` pub/sub | Blazor Server (same process) |
| **BroadcastChannel / storage event** | Browser `storage` event via JS | Blazor WASM or across server instances |

Both channels work together. The hub handles same-server circuits immediately. The JS storage event handles cases where tabs might be on different server instances.

## Enabling Cross-Tab Sync

### Step 1: Use LocalStorage strategy

```csharp
partial void ConfigureState(__StateContext ctx)
{
    ctx.CartItems.Storage = StorageStrategy.LocalStorage();
}
```

That's all you need for the data to sync. The `StateManager` automatically registers hub subscriptions and JS listeners.

### Step 2: Opt in to re-renders (optional)

If your component needs to re-render when **another tab** changes a value, inherit from `StateComponentBase`:

```csharp
public partial class CartBadge : StateComponentBase
{
    [Persist]
    public partial int? CartCount { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.CartCount.Storage = StorageStrategy.LocalStorage();
    }
}
```

`StateComponentBase` subscribes to `CrossTabSyncService.AfterCrossTabChange` and calls `StateHasChanged()` when any cross-tab update arrives.

> **Why is this opt-in?** Not every component needs to react to cross-tab changes. A form component shouldn't re-render because another tab updated a preference. Inherit `StateComponentBase` only for components that display cross-tab-sensitive data.

## Echo Prevention

A naive implementation would create an infinite loop:

```
Tab A changes value → persists → Tab B receives → persists → Tab A receives → ...
```

TheBlazorState prevents this with two mechanisms:

1. **Publisher ID filtering** — `CrossTabHub.Publish()` includes the publisher's circuit ID. Subscribers with the same ID are skipped.
2. **SuppressPersist flag** — When a cross-tab update arrives, `StateMeta.SuppressPersist` is set to `true` before updating the value. The change handler sees this flag and skips writing back to storage.

## Identifying Change Sources

Use `StateMeta.ChangeLog` to distinguish local vs cross-tab changes:

```csharp
var lastChange = CartCountMeta.ChangeLog.FirstOrDefault();

if (lastChange?.Source == ChangeSource.CrossTab)
{
    // This value came from another tab
    ShowToast("Cart updated in another tab");
}
```

## Architecture Details

### CrossTabHub

- **Lifetime:** Singleton (shared across all circuits)
- **Thread safety:** All collections use `ConcurrentDictionary`
- **Dispatch:** Callbacks fire via `Task.Run()` so each circuit processes on its own thread
- **Cleanup:** Subscriptions return `IDisposable` — disposed automatically when components unmount

### CrossTabSyncService

- **Lifetime:** Scoped (one per circuit)
- **JS module:** Lazily loads `_content/TheBlazorState/theblazorstate.js`
- **Prerender safe:** Catches `InvalidOperationException` during SSR (JS interop unavailable)
- **Event:** `AfterCrossTabChange` fires after processing a cross-tab update

### StateComponentBase

- **Lifetime:** Per-component instance
- **Subscribes to:** `CrossTabSyncService.AfterCrossTabChange`
- **Re-render:** Calls `InvokeAsync(StateHasChanged)` (safe for async dispatch)
- **Cleanup:** Unsubscribes in `Dispose()`
