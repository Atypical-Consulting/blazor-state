---
phase: 04-persistence
plan: 03
subsystem: persistence-di
tags: [di, integration, blazor, browser-storage, middleware]

dependency_graph:
  requires:
    - 04-01  # IBrowserStorage, BrowserStorageService
    - 04-02  # PersistenceMiddleware
  provides:
    - DI registration for IBrowserStorage
    - Auto-configuration of PersistenceMiddleware for [Persist] stores
    - State restoration during store construction
    - BustandInitializer component for JS interop availability
  affects:
    - 04-04  # Tests will verify this integration
    - 04-05  # Documentation will reference this

tech_stack:
  added: []
  patterns:
    - factory-based-di
    - scoped-storage-service
    - lifecycle-component

key_files:
  created:
    - src/Bustand/Blazor/BustandInitializer.razor
  modified:
    - src/Bustand/Core/ZustandStore.cs
    - src/Bustand/Extensions/ServiceCollectionExtensions.cs

decisions:
  - id: scoped-storage
    choice: "IBrowserStorage registered as Scoped"
    rationale: "One storage service per circuit/user in Server mode"
  - id: factory-restore
    choice: "State restoration in factory, not constructor"
    rationale: "DI factories can handle reflection-based middleware creation"
  - id: sync-restore
    choice: "Synchronous restore check only if task completed"
    rationale: "Can't await in DI factory; works for WASM, InitialState fallback for Server prerender"
  - id: initializer-component
    choice: "BustandInitializer component triggers SetAvailable()"
    rationale: "Users must place component in App.razor; explicit vs magic"

metrics:
  duration: 2 min
  completed: 2026-01-24
---

# Phase 4 Plan 3: DI Integration Summary

**One-liner:** Persistence wired into DI via factory registration with BustandInitializer component for JS interop availability.

## What Was Built

### ZustandStore State Restoration (Task 1)

Added internal methods to `ZustandStore<TState>` for persistence restoration:

```csharp
// Set restored state during construction
internal void SetRestoredState(TState? restoredState)
{
    if (restoredState is null) return;
    lock (_lock)
    {
        if (_stateInitialized) return;
        _state = restoredState;
        _stateInitialized = true;
    }
}

// Get initial state for merging scenarios
internal TState GetInitialState() => InitialState;
```

### DI Registration (Task 2)

Updated `ServiceCollectionExtensions` with persistence integration:

1. **IBrowserStorage Registration:** Scoped service registered when any stores have `[Persist]` attribute
2. **PersistenceMiddleware Creation:** Factory creates middleware per-store with proper storage key
3. **State Restoration:** Attempts synchronous restore if storage is available
4. **Graceful Fallback:** Uses `InitialState` when storage unavailable (prerender, first load)

### BustandInitializer Component

Created `src/Bustand/Blazor/BustandInitializer.razor`:

```razor
@inject IBrowserStorage BrowserStorage

@code {
    private bool _initialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _initialized = true;
            BrowserStorage.SetAvailable();
        }
    }
}
```

**Usage:** Users add `<BustandInitializer />` to App.razor or MainLayout.razor to enable persistence.

## Key Implementation Details

### Factory-Based DI Registration

The `CreateStoreWithPipeline` method:

1. Creates store instance via `ActivatorUtilities.CreateInstance`
2. Builds middleware list (user middleware + persistence if applicable)
3. Checks for `[Persist]` attribute
4. Creates `PersistenceMiddleware<TState>` with correct storage key
5. Attempts state restoration if storage is immediately available
6. Creates and injects the middleware pipeline

### State Restoration Flow

```
DI Resolution
    |
    v
CreateStoreWithPipeline()
    |
    v
Create PersistenceMiddleware
    |
    v
TryRestoreState()
    |
    +-- Task completed? --> Get Result --> SetRestoredState()
    |
    +-- Task not completed (prerender) --> Skip, InitialState used
    |
    v
Return store with pipeline
```

### Storage Availability Flow

```
App starts (prerender)
    |
    v
Store resolved, IsAvailable=false --> InitialState used
    |
    v
First render (OnAfterRenderAsync)
    |
    v
BustandInitializer calls SetAvailable()
    |
    v
Storage operations now work
    |
    v
State changes persist via PersistenceMiddleware
```

## Files Changed

| File | Changes |
|------|---------|
| `src/Bustand/Core/ZustandStore.cs` | +30 lines: SetRestoredState, GetInitialState methods |
| `src/Bustand/Extensions/ServiceCollectionExtensions.cs` | +166 lines: persistence registration, factory creation |
| `src/Bustand/Blazor/BustandInitializer.razor` | New file: triggers storage availability |

## Verification

All checks passed:

- [x] `dotnet build src/Bustand/Bustand.csproj` - compiles without errors
- [x] ZustandStore has SetRestoredState and GetInitialState internal methods
- [x] ServiceCollectionExtensions registers IBrowserStorage
- [x] ServiceCollectionExtensions creates PersistenceMiddleware for [Persist] stores
- [x] State restoration logic handles both available and unavailable storage
- [x] BustandInitializer.razor exists and calls SetAvailable() after first render

## Deviations from Plan

None - plan executed exactly as written.

## Commits

| Hash | Message |
|------|---------|
| 10c0bf7 | feat(04-03): add state restoration support to ZustandStore |
| eef1135 | feat(04-03): wire persistence into DI and add BustandInitializer component |

## Next Phase Readiness

**Ready for 04-04:** Persistence Integration Tests

Prerequisites met:
- IBrowserStorage registered in DI
- PersistenceMiddleware auto-configured for [Persist] stores
- State restoration implemented
- BustandInitializer component available for triggering storage availability
