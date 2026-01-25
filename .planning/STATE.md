# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-24)

**Core value:** Minimal boilerplate state management with exceptional debugging experience
**Current focus:** Phase 6: Distribution (In Progress)

## Current Position

Phase: 6 of 6 (Distribution)
Plan: 5 of 9 in current phase
Status: In progress
Last activity: 2026-01-25 - Completed 06-05-PLAN.md (Counter Pages)

Progress: [██████████] 100% (35/34 plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 34
- Average duration: 2.5 min
- Total execution time: 84.7 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 3 | 8 min | 2.7 min |
| 02-core-store | 4 | 13.7 min | 3.4 min |
| 03-middleware-dx | 4 | 8 min | 2 min |
| 04-persistence | 5 | 15 min | 3 min |
| 05-devtools | 8 | 22 min | 2.75 min |
| 06-distribution | 10 | 20 min | 2 min |

**Recent Trend:**
- Last 5 plans: 06-03, 06-04, 06-05, 06-06, 06-07 (avg 2 min each)
- Note: Phase 6 complete - sample stores created with full documentation

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
- [04-04]: Interface-based circuit lifecycle testing (Circuit is sealed)
- [04-04]: Storage availability lifecycle tested through SetAvailable/SetUnavailable methods
- [05-01]: ReferenceHandler.IgnoreCycles for circular reference handling in JSON serialization
- [05-01]: 100-entry history limit per store per CONTEXT.md decision
- [05-01]: Branch history model: truncate future entries when new changes occur after time-travel
- [05-01]: RegisterStore internal method for middleware to register store instances
- [05-02]: DevToolsMiddleware always returns true in OnBeforeChange (passive observer)
- [05-02]: Console.WriteLine for production warning (consistent with Phase 1 approach)
- [05-02]: Two AddBustandDevTools overloads: environment-aware (recommended) and manual
- [05-02]: DevToolsEnabled flag for coordination between core and DevTools packages
- [05-03]: Code-behind partial class pattern for DevToolsPage (separation of markup and logic)
- [05-03]: CSS variables for theming (enables future customization)
- [05-03]: Tab bar with placeholders for upcoming panel components
- [05-04]: Depth < 1 for default expansion (top-level expanded, nested collapsed)
- [05-04]: MarkupString for HTML entity icons in export buttons
- [05-04]: JsonDocument.Parse with Clone() for long-lived JsonElement
- [05-05]: Direct field access via reflection for time-travel (SetRestoredState has early-exit check)
- [05-05]: Walk inheritance chain to find ZustandStore<T> base type for field access
- [05-05]: Newest-first ordering using LINQ Reverse() for history display
- [05-06]: CompareNETObjects via transitive reference from Bustand project
- [05-06]: DiffType enum for Added/Removed/Modified categorization
- [05-06]: JSON serialization in DiffResult for side-by-side display
- [05-07]: SetStoreInstance internal method for DI to inject store reference
- [05-07]: Type.GetType with assembly-qualified name for dynamic middleware resolution
- [05-07]: Lazy store registration on first state change (avoids construction order issues)
- [05-07]: TrySetStoreInstance uses reflection for cross-assembly compatibility
- [05-08]: MockStore pattern for time-travel testing (minimal mock with required methods)
- [05-08]: NSubstitute for IDevToolsStore mocking (simpler API over Moq)
- [05-08]: 16 DevTools-specific tests verify core functionality
- [06-01]: Suppress CS1591/CS0419/CS1574 for XML docs until full documentation
- [06-01]: Test project targets net10.0 to avoid static web asset conflicts
- [06-01]: MSBuild target removes duplicate WebAssembly.Authentication assets
- [06-01]: DevTools uses ProjectReference (Debug) and PackageReference [$(Version)] (Release)
- [06-02]: Python PIL for icon generation (ImageMagick not available)
- [06-02]: Blazor purple (#512BD4) for icon background color
- [06-02]: TodoList example in Quick Start (more realistic than Counter)
- [06-02]: DevTools included in Quick Start as recommended practice
- [06-08]: xunit v3 for .NET 10 compatibility (xunit 2.x doesn't discover tests)
- [06-08]: OutputType Exe required by xunit v3 (generates own entry point)
- [06-08]: Removed Microsoft.NET.Test.Sdk (not used by xunit v3)
- [06-07]: DefaultDocumentation 1.2.2 with PrivateAssets=all (no runtime dependency)
- [06-07]: Generated docs gitignored as regeneratable build artifacts
- [06-07]: IsPackable condition ensures only library projects generate docs
- [06-03]: PrivateAssets=all on FrameworkReference prevents WASM consumer build failures
- [06-03]: samples/Directory.Build.props isolates samples to net10.0 TFM
- [06-03]: DevTools needs explicit FrameworkReference due to PrivateAssets propagation block
- [06-03]: Server project needs WebAssembly.Server package for Auto render mode
- [06-04]: StorageType.Local enum value (not LocalStorage)
- [06-04]: Protected InitialState property (not public override)
- [06-04]: XML param tags for record parameters (avoids CS1587)
- [06-04]: SessionStorage for cart (clears on browser close)
- [06-05]: UseState pattern with sliced state for Counter pages
- [06-05]: RenderMode static using in _Imports.razor for clean syntax

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-25T09:40:20Z
Stopped at: Completed 06-05-PLAN.md (Counter Pages)
Resume file: None
