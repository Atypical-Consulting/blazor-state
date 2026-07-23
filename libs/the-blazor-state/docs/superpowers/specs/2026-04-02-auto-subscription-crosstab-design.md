# Auto-Subscription & Cross-Tab Sync — Design Specification

**Date:** 2026-04-02
**Status:** Draft

## Overview

Two features that make TheBlazorState's reactive state truly zero-ceremony:

1. **InjectSubscriptionGenerator** — A third source generator that automatically subscribes components to `[Shared]` state changes when they inject shared state classes. No `[Persist]` property required, no manual subscription.

2. **Cross-Tab Sync** — When a `[Persist]` property using `StorageStrategy.LocalStorage()` changes in one browser tab, all other tabs with the same app automatically receive the updated value and re-render.

## Feature 1: InjectSubscriptionGenerator

### Problem

Currently, components need `[Persist]` properties for the PersistGenerator to fire and emit shared state subscriptions. Components that only inject `[Shared]` state (like MainLayout) require manual `((INotifyStateChanged)X).StateChanged += handler` wiring. This is error-prone and defeats the library's zero-boilerplate promise.

Additionally, the PersistGenerator's auto-subscription detection checks `AllInterfaces` for `INotifyStateChanged`, which is added by the SharedGenerator — a sibling generator can't see another generator's output.

### Solution

A new `InjectSubscriptionGenerator` that:

1. Scans all `partial class` types inheriting `ComponentBase`
2. Finds `[Inject]` properties whose type has properties decorated with `[Shared]` attribute (not checking `INotifyStateChanged` — detects the attribute directly from source)
3. Emits `OnInitialized` subscription, handler method, and `Dispose` cleanup

### Detection Logic

Instead of checking `INotifyStateChanged` (which is generated and invisible to sibling generators), check if the injected type has any members with `[Shared]` attribute:

```
For each [Inject] property P on the class:
  For each member M of P.Type:
    If M has [SharedAttribute]:
      → P.Type is a shared state class
      → Subscribe to P.StateChanged
```

This works because `[Shared]` is user-written source code, visible to all generators.

### Ownership

InjectSubscriptionGenerator owns ALL shared state subscriptions. The existing subscription logic in PersistEmitter is removed. This avoids conflicts and ensures one generator handles one concern.

### Generated Code

For a component like:
```csharp
public partial class MainLayout : LayoutComponentBase
{
    [Inject] public ThemeState Theme { get; set; } = default!;
}
```

The generator emits:
```csharp
partial class MainLayout : IDisposable
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Theme.StateChanged += __OnSharedStateChanged;
    }

    private void __OnSharedStateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Theme.StateChanged -= __OnSharedStateChanged;
    }
}
```

For multiple injected shared states:
```csharp
protected override void OnInitialized()
{
    base.OnInitialized();
    Theme.StateChanged += __OnSharedStateChanged;
    Project.StateChanged += __OnSharedStateChanged;
}

public void Dispose()
{
    Theme.StateChanged -= __OnSharedStateChanged;
    Project.StateChanged -= __OnSharedStateChanged;
}
```

### Conflict with PersistGenerator

Both generators may emit `OnInitialized` for the same class — PersistGenerator for `[Persist]` property wiring, InjectSubscriptionGenerator for shared state subscriptions.

Resolution: The InjectSubscriptionGenerator emits into a DIFFERENT partial method. Instead of `OnInitialized`, it overrides `OnAfterRender(bool firstRender)` and subscribes on first render. This avoids the duplicate `OnInitialized` conflict:

```csharp
partial class Dashboard : IDisposable
{
    private bool __sharedStateSubscribed;

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (!__sharedStateSubscribed)
        {
            __sharedStateSubscribed = true;
            Project.StateChanged += __OnSharedStateChanged;
        }
    }

    private void __OnSharedStateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Project.StateChanged -= __OnSharedStateChanged;
    }
}
```

Wait — this still conflicts if PersistGenerator also emits `Dispose`. 

Better approach: InjectSubscriptionGenerator checks if the class has `[Persist]` properties. If yes, it emits subscription code into the SAME generated file via a different hint name, using methods that don't conflict:
- Subscription: done in the existing generated `OnInitialized` via a partial method hook
- Or: use a different mechanism entirely

Simplest conflict-free approach: **InjectSubscriptionGenerator emits a separate partial class file** with:
- A `__SubscribeSharedState()` method and `__UnsubscribeSharedState()` method
- PersistEmitter calls `__SubscribeSharedState()` from its `OnInitialized` and `__UnsubscribeSharedState()` from its `Dispose`
- For components WITHOUT `[Persist]`, InjectSubscriptionGenerator emits full `OnInitialized` + `Dispose`

Actually, the cleanest approach:

**For components WITHOUT [Persist] properties:**
InjectSubscriptionGenerator emits full `OnInitialized`, handler, and `Dispose`.

**For components WITH [Persist] properties:**
InjectSubscriptionGenerator emits only a `partial void __SubscribeToSharedState()` and `partial void __UnsubscribeFromSharedState()`. PersistEmitter declares these as partial methods and calls them from its `OnInitialized` and `Dispose`.

This way:
- No duplicate lifecycle method conflicts
- PersistEmitter doesn't need to detect shared state itself
- InjectSubscriptionGenerator handles all subscription logic

### Diagnostics

No new diagnostics needed. The generator silently skips classes that don't have shared state injections.

## Feature 2: Cross-Tab Sync

### Problem

When a user opens two tabs of the same Blazor app, and changes a localStorage-persisted setting in Tab A, Tab B doesn't update. The browser stores the value but the Blazor circuit in Tab B has no idea it changed.

### Solution

Leverage the browser's native `storage` event, which fires in all tabs EXCEPT the one that made the change.

### Architecture

**JS Module** (`theblazorstate.js` additions):

```javascript
let dotNetRef = null;

export function registerSyncCallback(dotNetReference) {
    dotNetRef = dotNetReference;
    window.addEventListener('storage', onStorageEvent);
}

export function unregisterSyncCallback() {
    window.removeEventListener('storage', onStorageEvent);
    dotNetRef = null;
}

function onStorageEvent(e) {
    if (dotNetRef && e.key && e.newValue) {
        dotNetRef.invokeMethodAsync('OnStorageChanged', e.key, e.newValue);
    }
}
```

**C# Service** (`CrossTabSyncService.cs`):

A scoped service that:
1. Holds a `DotNetObjectReference<CrossTabSyncService>` for JS callbacks
2. Maintains a `Dictionary<string, Action<string>>` mapping storage keys to update callbacks
3. On `OnStorageChanged` (called from JS): looks up the key, deserializes the new value, invokes the callback

```csharp
public sealed class CrossTabSyncService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<CrossTabSyncService>? _dotNetRef;
    private readonly Dictionary<string, Action<string>> _callbacks = new();
    private bool _listening;

    public void RegisterKey(string key, Action<string> onChanged) { ... }

    [JSInvokable]
    public void OnStorageChanged(string key, string newValue) { ... }

    // Starts listening on first RegisterKey call
    // Stops on DisposeAsync
}
```

**StateManager integration:**

In `RegisterEagerChangeHandler`, when the effective strategy is `LocalStorageStrategy`:
```csharp
if (effectiveStrategy is LocalStorageStrategy)
{
    crossTabSync.RegisterKey(key, rawJson =>
    {
        var envelope = JsonSerializer.Deserialize<PersistedEnvelope<T>>(rawJson);
        if (envelope is not null)
        {
            valueSetter(envelope.Value);
            meta.MarkDirty();
            meta.RaiseChanged();
        }
    });
}
```

### Lifecycle

- `CrossTabSyncService` is registered as scoped (per circuit)
- JS listener registered lazily on first key registration
- JS listener removed on `DisposeAsync` (circuit end)
- `DotNetObjectReference` disposed to prevent memory leaks

### Edge Cases

- **Same tab writes don't trigger:** The browser's `storage` event only fires in OTHER tabs. No duplicate handling needed.
- **Circuit disconnection:** If the circuit drops, the `DotNetObjectReference` becomes invalid. JS calls will fail silently (Blazor handles this).
- **Rapid changes:** The `storage` event is debounced by the browser. No additional throttling needed.
- **Key not registered:** If JS reports a key that wasn't registered (e.g., from a different app), the callback lookup returns null — ignored.

## Changes Summary

### Generator files
| File | Action |
|------|--------|
| `InjectSubscriptionGenerator.cs` | Create |
| `InjectSubscriptionEmitter.cs` | Create |
| `InjectSubscriptionModel.cs` | Create |
| `PersistEmitter.cs` | Remove shared state subscription logic, add partial method calls |
| `PersistIncrementalGenerator.cs` | Remove injected shared state detection |
| `ComponentModel.cs` | Remove `InjectedSharedStates` from `PersistComponentModel` |

### Runtime files
| File | Action |
|------|--------|
| `CrossTabSyncService.cs` | Create |
| `theblazorstate.js` | Add storage event listener + DotNet callback |
| `StateManager.cs` | Register with CrossTabSyncService for localStorage keys, add CrossTabSyncService constructor param |
| `ServiceCollectionExtensions.cs` | Register CrossTabSyncService |

### Demo files
| File | Action |
|------|--------|
| `Dashboard.razor.cs` | Remove manual StateChanged subscription |
| `Board.razor.cs` | Remove manual StateChanged subscription |
| `MainLayout.razor` | Remove manual StateChanged subscription + IDisposable |

### Test files
| File | Action |
|------|--------|
| `InjectSubscriptionGeneratorTests.cs` | Create |
| `CrossTabSyncServiceTests.cs` | Create |
| `SharedSubscriptionTests.cs` | Update (subscriptions now from InjectSubscriptionGenerator) |
| `PersistGeneratorTests.cs` | Update (no more shared state subscription in output) |

## Out of Scope

- Cross-tab sync for SessionStorage (sessionStorage is per-tab by design)
- Cross-tab sync for IndexedDB (no native event — would need polling or BroadcastChannel)
- Conflict resolution for concurrent writes (last-write-wins is sufficient)
- Cross-tab sync for server-side state (SignalR push — different feature)
