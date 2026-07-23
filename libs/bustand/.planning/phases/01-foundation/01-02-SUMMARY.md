---
phase: 01-foundation
plan: 02
subsystem: di
tags: [scrutor, dependency-injection, blazor, wasm, server]

# Dependency graph
requires:
  - phase: 01-01
    provides: IStore, ZustandStore<T> base classes
provides:
  - BustandStoreAttribute for opt-in store discovery
  - BustandOptions for fluent configuration
  - BlazorModeDetector for WASM vs Server detection
  - AddBustand() DI registration extension
affects: [component-binding, devtools, testing]

# Tech tracking
tech-stack:
  added: []
  patterns: [attribute-based-discovery, mode-aware-lifetime, scrutor-scanning]

key-files:
  created:
    - src/Bustand/Attributes/BustandStoreAttribute.cs
    - src/Bustand/Configuration/BustandOptions.cs
    - src/Bustand/Detection/BlazorModeDetector.cs
    - src/Bustand/Extensions/ServiceCollectionExtensions.cs
    - src/Bustand/Bustand.cs
  modified: []

key-decisions:
  - "Used OperatingSystem.IsBrowser() for WASM detection (most reliable .NET 6+ approach)"
  - "Post-process pattern for lifetime overrides (Scrutor scans first, then re-register with explicit lifetimes)"
  - "Console.WriteLine for warnings (simple, no logging dependency required in Phase 1)"

patterns-established:
  - "Attribute-based discovery: [BustandStore] marks stores for auto-registration"
  - "Mode-aware defaults: Singleton in WASM, Scoped in Server"
  - "Fluent configuration: options.ScanAssemblyContaining<T>() chaining"

# Metrics
duration: 2min
completed: 2026-01-24
---

# Phase 1 Plan 02: DI Registration System Summary

**Scrutor-based AddBustand() extension with mode-aware lifetime defaults and per-store lifetime overrides via [BustandStore] attribute**

## Performance

- **Duration:** 2 min
- **Started:** 2026-01-24T11:07:35Z
- **Completed:** 2026-01-24T11:09:35Z
- **Tasks:** 3
- **Files created:** 5

## Accomplishments

- BustandStoreAttribute with optional ServiceLifetime parameter for explicit overrides
- BlazorModeDetector using OperatingSystem.IsBrowser() for accurate WASM detection
- AddBustand() extension leveraging Scrutor for automatic store discovery
- Per-store lifetime override mechanism via ApplyPerStoreLifetimeOverrides
- Warning system for Singleton stores in Server mode (potential data leak)

## Task Commits

Each task was committed atomically:

1. **Task 1: Create BustandStore attribute and configuration types** - `7748b4a` (feat)
2. **Task 2: Create BlazorModeDetector** - `982d9ab` (feat)
3. **Task 3: Create DI extensions with lifetime override verification** - `9b5f098` (feat)

**Plan metadata:** (pending)

## Files Created/Modified

- `src/Bustand/Attributes/BustandStoreAttribute.cs` - Attribute for opt-in store discovery with optional lifetime
- `src/Bustand/Configuration/BustandOptions.cs` - Fluent configuration for assembly scanning and defaults
- `src/Bustand/Detection/BlazorModeDetector.cs` - WASM vs Server detection via OperatingSystem.IsBrowser()
- `src/Bustand/Extensions/ServiceCollectionExtensions.cs` - AddBustand() with Scrutor scanning and lifetime overrides
- `src/Bustand/Bustand.cs` - Global usings for convenient imports

## Decisions Made

1. **OperatingSystem.IsBrowser() for detection** - Most reliable .NET 6+ approach, works at service registration time
2. **Post-process pattern for overrides** - Scrutor scans with default lifetime, then we replace descriptors for stores with explicit lifetime in attribute
3. **Console.WriteLine for warnings** - Simple approach for Phase 1, no logging framework dependency needed
4. **Internal GetRegisteredLifetime helper** - Enables unit testing of lifetime override behavior

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- DI registration system complete with mode-aware defaults
- Ready for component binding implementation (Plan 03)
- Stores can now be marked with [BustandStore] and auto-registered via AddBustand()

---
*Phase: 01-foundation*
*Completed: 2026-01-24*
