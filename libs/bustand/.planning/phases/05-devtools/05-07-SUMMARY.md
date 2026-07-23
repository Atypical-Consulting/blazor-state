---
phase: 05-devtools
plan: 07
subsystem: devtools
tags: [middleware, wiring, store-registration, time-travel, DI]

# Dependency graph
requires:
  - phase: 05-02
    provides: DevToolsMiddleware base implementation
  - phase: 05-05
    provides: Time-travel via JumpToState
  - phase: 05-06
    provides: DiffService for state comparison
provides:
  - Complete DevTools wiring with middleware auto-registration
  - Store instances registered for time-travel
  - Dynamic DevToolsMiddleware resolution via reflection
affects: [05-08]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Reflection-based middleware discovery (assembly-qualified name)
    - SetStoreInstance pattern for middleware-store binding
    - Lazy store registration on first state change

key-files:
  created: []
  modified:
    - src/Bustand.DevTools/Middleware/DevToolsMiddleware.cs
    - src/Bustand/Extensions/ServiceCollectionExtensions.cs
    - src/Bustand.DevTools/Extensions/DevToolsServiceCollectionExtensions.cs

key-decisions:
  - "SetStoreInstance internal method for DI to inject store reference"
  - "Type.GetType with assembly-qualified name for dynamic middleware resolution"
  - "Lazy store registration on first state change to avoid construction order issues"
  - "TrySetStoreInstance uses reflection for cross-assembly compatibility"

patterns-established:
  - "Dynamic middleware discovery without direct assembly reference"
  - "SetStoreInstance pattern for middleware-store binding"
  - "DevToolsEnabled flag triggers pipeline wiring even without other middleware"

# Metrics
duration: 3min
completed: 2026-01-24
---

# Phase 05 Plan 07: DevTools Middleware Wiring Summary

**Complete end-to-end wiring: stores auto-register with DevToolsStore, middleware captures all state changes, time-travel works fully**

## Performance

- **Duration:** 3 min
- **Started:** 2026-01-24T21:12:11Z
- **Completed:** 2026-01-24T21:14:51Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- DevToolsMiddleware now auto-registers stores on first state change
- SetStoreInstance method enables DI to inject store reference
- ServiceCollectionExtensions dynamically resolves DevToolsMiddleware via reflection
- AddBustandDevTools registers all DevTools services including open generic middleware
- Complete registration flow: AddBustand + UseDevTools + AddBustandDevTools wires everything

## Task Commits

Each task was committed atomically:

1. **Task 1: Update DevToolsMiddleware to auto-register stores** - `66c3b45` (feat)
2. **Task 2: Update ServiceCollectionExtensions to wire DevTools middleware** - `af55c7a` (feat)
3. **Task 3: Update AddBustandDevTools to configure middleware** - `a7a3370` (feat)

## Files Modified

- `src/Bustand.DevTools/Middleware/DevToolsMiddleware.cs` - Added SetStoreInstance, lazy store registration on first state change
- `src/Bustand/Extensions/ServiceCollectionExtensions.cs` - Added TrySetStoreInstance, TryAddDevToolsMiddleware, DevToolsEnabled wiring
- `src/Bustand.DevTools/Extensions/DevToolsServiceCollectionExtensions.cs` - Register DevToolsMiddleware<> and DiffService

## Decisions Made

- **SetStoreInstance internal method:** Allows DI factory to inject store reference into middleware, enabling time-travel registration
- **Assembly-qualified type name:** Use `Type.GetType("Bustand.DevTools.Middleware.DevToolsMiddleware`1, Bustand.DevTools")` to resolve middleware without circular dependency
- **Lazy registration:** Register store with DevToolsStore on first state change (not at construction) to avoid ordering issues
- **Reflection for cross-assembly:** TrySetStoreInstance uses reflection to call SetStoreInstance, enabling Bustand core to work with DevTools without direct reference

## Complete Registration Flow

The complete flow is now:

```csharp
// Program.cs
builder.Services.AddBustand(options =>
{
    options.ScanAssemblyContaining<CounterStore>();
    options.UseLogging();      // Optional: other middleware
    options.UseDevTools();     // Sets DevToolsEnabled flag
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddBustandDevTools(builder.Environment);
}
```

**What happens:**
1. `UseDevTools()` sets `DevToolsEnabled = true` in BustandOptions
2. `AddBustandDevTools()` registers:
   - `IDevToolsStore` / `DevToolsStore` (scoped)
   - `DiffService` (scoped)
   - `DevToolsMiddleware<>` (scoped open generic)
3. `AddBustand` sees `DevToolsEnabled = true` and triggers pipeline wiring
4. `TryAddDevToolsMiddleware` resolves closed `DevToolsMiddleware<TState>` from DI
5. `TrySetStoreInstance` passes store reference to middleware
6. On first state change, middleware calls `DevToolsStore.RegisterStore()`
7. Time-travel now works via `JumpToState()` which uses the registered store instance

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Complete DevTools wiring is in place
- Middleware captures state from all stores
- Stores auto-register with DevToolsStore for time-travel
- Ready for integration tests (05-08)

---
*Phase: 05-devtools*
*Completed: 2026-01-24*
