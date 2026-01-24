# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-24)

**Core value:** Minimal boilerplate state management with exceptional debugging experience
**Current focus:** Phase 4: Persistence

## Current Position

Phase: 4 of 6 (Persistence)
Plan: 5 of 5 in current phase
Status: Phase complete
Last activity: 2026-01-24 - Completed 04-05-PLAN.md (Circuit Reconnect Handling)

Progress: [█████████░] 94%

## Performance Metrics

**Velocity:**
- Total plans completed: 15
- Average duration: 2.5 min
- Total execution time: 37.7 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 3 | 8 min | 2.7 min |
| 02-core-store | 4 | 13.7 min | 3.4 min |
| 03-middleware-dx | 4 | 8 min | 2 min |
| 04-persistence | 5 | 10 min | 2 min |

**Recent Trend:**
- Last 5 plans: 03-04 (2 min), 04-01 (2 min), 04-02 (2 min), 04-03 (2 min), 04-05 (2 min)
- Note: Phase 4 complete with consistent 2-min plan execution

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
- [03-01]: BeforeChange exceptions bubble up to caller (validation failures must be visible)
- [03-01]: AfterChange exceptions logged via Debug and continue pipeline (side effects don't break each other)
- [03-01]: Static Empty property on pipeline for zero-allocation when no middleware configured
- [03-02]: UseMiddleware<T> uses class constraint (IMiddleware<> constraint not possible for open generics)
- [03-02]: SetPipeline is internal method (only DI should inject pipelines)
- [03-02]: Factory-based DI registration wraps store construction with pipeline injection
- [03-02]: Middleware types closed per-store (e.g., LoggingMiddleware<CounterState>)
- [03-03]: IOptions<LoggingMiddlewareOptions> nullable for flexibility without DI registration
- [03-03]: IsEnabled check before diffing for zero-cost when logging disabled
- [03-03]: Store type filtering applies before expensive diff operation
- [03-04]: NSubstitute for ILogger mocking over Moq (simpler API)
- [03-04]: Separate test middleware helpers in TestMiddleware namespace for reusability
- [03-04]: OrderTrackingMiddleware uses shared list for cross-instance verification
- [04-01]: volatile bool for IsAvailable flag (thread-safe prerender detection)
- [04-01]: Debug.WriteLine for storage warnings (consistent with Phase 1 approach)
- [04-01]: 100KB threshold for large state warning (per RESEARCH.md recommendation)
- [04-02]: Timer-based debouncing with lock for thread safety
- [04-02]: Dispose does NOT flush - caller must call FlushAsync if data loss matters
- [04-02]: OnBeforeChange always true - persistence middleware never blocks state changes
- [04-03]: IBrowserStorage registered as Scoped (one per circuit/user)
- [04-03]: State restoration in DI factory via reflection (can't await, uses synchronous check)
- [04-03]: BustandInitializer component triggers SetAvailable() on first render
- [04-05]: FrameworkReference Microsoft.AspNetCore.App for CircuitHandler access
- [04-05]: Event-based notification pattern (OnAvailabilityChanged) for availability changes
- [04-05]: SetUnavailable on both connection down and circuit closed

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-24T19:29:13Z
Stopped at: Completed 04-05-PLAN.md (Phase 4 complete)
Resume file: None
