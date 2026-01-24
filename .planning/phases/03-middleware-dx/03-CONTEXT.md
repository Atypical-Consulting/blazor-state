# Phase 3: Middleware & DX - Context

**Gathered:** 2026-01-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Extensibility layer that allows developers to intercept and augment store behavior through middleware pipeline, plus automatic store discovery to eliminate manual registration boilerplate. This phase delivers infrastructure for extension, not specific middleware implementations beyond logging.

</domain>

<decisions>
## Implementation Decisions

### Middleware API Design
- Interface-based pattern: `IMiddleware<TState>` for structured implementation
- Support both sync (`IMiddleware.Invoke`) and async (`IAsyncMiddleware.InvokeAsync`) variants
- Middleware receives comprehensive context:
  - Old and new state (for comparison/diff)
  - Store instance reference
  - Action metadata (identifier/name if available)
  - Timestamp of change
- Registration via fluent API: `services.AddStore<MyStore>().UseMiddleware<Logger>()`

### Pipeline Execution
- Middleware executes in registration order (first registered runs first)
- Middleware can block state changes (validation/short-circuit capability)
- Middleware can hook into both timing points:
  - BeforeChange (validation, preprocessing)
  - AfterChange (logging, side effects)
  - Middleware chooses which hook(s) to implement

### Logging Middleware Specifics
- Logs all four context elements:
  - State snapshots (old → new)
  - Action identifier
  - Store identifier
  - Timing information
- Output format: Diff only (show what changed, not full state)
- Integration: Use `ILogger` from Microsoft.Extensions.Logging
- Filtering: Support store-based filtering (include/exclude specific stores)

### Auto-Discovery Mechanics
- Discovers stores only (not middleware)
- Requires explicit opt-in: `[BustandStore]` attribute on store classes
- Scans configurable assembly list: `AddBustand(assemblies: [...])` parameter
- Lifetime determination: Always read from attribute `[BustandStore(Lifetime.Scoped)]`

### Claude's Discretion
- Exception handling in pipeline (bubble up vs stop vs continue)
- Diff algorithm/library choice for logging
- Performance optimizations for assembly scanning
- Default assembly selection when not explicitly configured

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches that fit the established pattern.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 03-middleware-dx*
*Context gathered: 2026-01-24*
