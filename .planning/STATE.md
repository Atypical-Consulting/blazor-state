# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-24)

**Core value:** Minimal boilerplate state management with exceptional debugging experience
**Current focus:** Phase 2: Core Store (Complete)

## Current Position

Phase: 2 of 6 (Core Store)
Plan: 4 of 4 in current phase
Status: Phase complete
Last activity: 2026-01-24 - Completed 02-04-PLAN.md (Phase 2 Test Suite)

Progress: [██████████] 100%

## Performance Metrics

**Velocity:**
- Total plans completed: 7
- Average duration: 2.8 min
- Total execution time: 21.7 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 3 | 8 min | 2.7 min |
| 02-core-store | 4 | 13.7 min | 3.4 min |

**Recent Trend:**
- Last 5 plans: 02-01 (2.5 min), 02-02 (2.2 min), 02-03 (2 min), 02-04 (7 min)
- Note: 02-04 longer due to comprehensive test suite creation

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Roadmap]: Multi-mode architecture (Phase 1) before features to avoid retrofitting
- [Roadmap]: DevTools as separate package (Bustand.DevTools) to prevent production bloat
- [Roadmap]: Persistence split from Middleware phase due to JS interop complexity
- [01-01]: Simplified MODE-05 - subscribers call InvokeAsync in handlers (ComponentBase.InvokeAsync is protected)
- [01-01]: Used classic .sln format over .slnx for broader tool compatibility
- [01-02]: Used OperatingSystem.IsBrowser() for WASM detection (most reliable .NET 6+ approach)
- [01-02]: Post-process pattern for lifetime overrides (Scrutor scans first, then re-register with explicit lifetimes)
- [01-02]: Console.WriteLine for warnings (simple, no logging dependency required in Phase 1)
- [01-03]: Added InternalsVisibleTo for test access to GetRegisteredLifetime helper
- [01-03]: Used BunitContext instead of deprecated TestContext for component testing
- [02-01]: Abstract InitialState property instead of constructor parameter (cleaner API, enforces initialization)
- [02-01]: SynchronizationContext.Current capture for SetAsync (proper thread marshalling)
- [02-02]: IInternalSubscription<TState> interface for polymorphic notification
- [02-02]: Reference equality for slice change detection (works with C# records)
- [02-02]: Unsubscribe() method on ISubscription for semantic clarity
- [02-03]: Two component base classes: ZustandComponent (DI) and ZustandComponentScoped (CascadingParameter)
- [02-03]: UseStateResult<T> struct with implicit T conversion for ergonomic usage
- [02-04]: Reference equality for value types always triggers notifications due to boxing
- [02-04]: Store method invocation in bUnit tests for reliable event testing
- [02-04]: Switched test project to Microsoft.NET.Sdk.Razor for component compilation

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-24T12:39:14Z
Stopped at: Completed 02-04-PLAN.md
Resume file: None
