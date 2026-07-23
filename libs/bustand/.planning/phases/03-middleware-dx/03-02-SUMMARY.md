---
phase: 03-middleware-dx
plan: 02
subsystem: core
tags: [middleware, state-management, dependency-injection, extensibility]

# Dependency graph
requires:
  - phase: 03-01
    provides: MiddlewarePipeline, IMiddleware, MiddlewareContext
provides:
  - BustandOptions.UseMiddleware<T>() fluent registration API
  - ZustandStore middleware pipeline integration in Set methods
  - Automatic pipeline injection via ServiceCollectionExtensions
  - CallerMemberName capture for action names
affects: [03-middleware-dx (logging middleware tests), 05-devtools (DevTools integration)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Factory-based DI registration for pipeline injection"
    - "CallerMemberName attribute for automatic action naming"
    - "Reflection-based pipeline wiring for generic type handling"

key-files:
  created: []
  modified:
    - src/Bustand/Configuration/BustandOptions.cs
    - src/Bustand/Core/ZustandStore.cs
    - src/Bustand/Extensions/ServiceCollectionExtensions.cs

key-decisions:
  - "UseMiddleware<T> uses class constraint instead of IMiddleware<> to support open generics"
  - "SetPipeline is internal method for DI to inject pipeline"
  - "Factory-based DI registration to wrap store construction with pipeline injection"
  - "Middleware types are closed over each store's state type during registration"

patterns-established:
  - "Fluent configuration via UseMiddleware<T>() returning this for chaining"
  - "Empty pipeline pattern: stores without middleware use static Empty instance"
  - "CallerMemberName for automatic action name capture in middleware context"

# Metrics
duration: 3min
completed: 2026-01-24
---

# Phase 03 Plan 02: Middleware Integration Summary

**Middleware pipeline integrated into ZustandStore Set methods with fluent UseMiddleware<T> registration API and automatic DI-based pipeline injection**

## Performance

- **Duration:** 3 min
- **Started:** 2026-01-24T17:48:00Z
- **Completed:** 2026-01-24T17:51:00Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- BustandOptions.UseMiddleware<T>() provides fluent middleware registration
- ZustandStore.Set() methods now invoke middleware pipeline before/after state changes
- Middleware can block state changes by returning false from OnBeforeChange
- Action names captured automatically via [CallerMemberName] attribute
- ServiceCollectionExtensions builds and injects pipelines during DI registration
- All 73 existing tests continue to pass (full backward compatibility)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add middleware registration to BustandOptions** - `bd60e05` (feat)
2. **Task 2: Integrate middleware into ZustandStore** - `4440691` (feat)
3. **Task 3: Wire middleware in ServiceCollectionExtensions** - `764746e` (feat)

## Files Created/Modified
- `src/Bustand/Configuration/BustandOptions.cs` - Added MiddlewareTypes list and UseMiddleware<T>() method
- `src/Bustand/Core/ZustandStore.cs` - Added _pipeline field, SetPipeline() method, middleware invocation in all Set methods
- `src/Bustand/Extensions/ServiceCollectionExtensions.cs` - Added RegisterMiddlewareAndPipelines, GetStoreStateType, InjectPipeline helpers

## Decisions Made
- **UseMiddleware<T> uses `where TMiddleware : class`:** IMiddleware<> constraint not possible for open generics, validated at runtime instead
- **SetPipeline is internal:** Only DI should inject pipelines, not user code
- **Factory-based registration:** Wrapping store construction with factory allows pipeline injection after ActivatorUtilities.CreateInstance
- **Middleware types closed per-store:** Each store gets its own closed generic middleware instances (e.g., LoggingMiddleware<CounterState>)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Middleware integration complete, ready for LoggingMiddleware implementation (03-03)
- UseMiddleware<T>() API ready for consumer use
- Pipeline injection pattern established for future middleware types
- CallerMemberName pattern provides good default action names

---
*Phase: 03-middleware-dx*
*Completed: 2026-01-24*
