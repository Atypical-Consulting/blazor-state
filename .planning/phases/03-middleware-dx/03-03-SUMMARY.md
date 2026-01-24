---
phase: 03-middleware-dx
plan: 03
subsystem: middleware
tags: [logging, ilogger, comparenetobjects, state-diffing, source-generator]

# Dependency graph
requires:
  - phase: 03-01
    provides: IMiddleware<TState> interface and MiddlewareContext
provides:
  - LoggingMiddleware<TState> for state change logging
  - LoggingMiddlewareOptions for store filtering
  - LoggingExtensions with source-generated log methods
affects: [03-04-devtools, future-debugging]

# Tech tracking
tech-stack:
  added:
    - Microsoft.Extensions.Logging.Abstractions 10.0.0
    - CompareNETObjects 4.84.0
  patterns:
    - Source-generated logging via [LoggerMessage]
    - Options pattern for middleware configuration
    - IsEnabled guard for zero-cost when logging disabled

key-files:
  created:
    - src/Bustand/Middleware/LoggingMiddleware.cs
    - src/Bustand/Middleware/LoggingMiddlewareOptions.cs
    - src/Bustand/Middleware/LoggingExtensions.cs
  modified:
    - src/Bustand/Bustand.csproj

key-decisions:
  - "IOptions<LoggingMiddlewareOptions> nullable for flexibility without DI registration"
  - "IsEnabled check before diffing for zero-cost when logging disabled"
  - "Store type filtering applies before expensive diff operation"
  - "CompareLogic instance per middleware for thread safety"

patterns-established:
  - "Logging middleware pattern: guard with IsEnabled, filter by store, then compute"
  - "Source-generated logging for all middleware log output"

# Metrics
duration: 2min
completed: 2026-01-24
---

# Phase 3 Plan 3: Logging Middleware Summary

**LoggingMiddleware with CompareNETObjects diffing and source-generated ILogger output, featuring store filtering and zero-cost when disabled**

## Performance

- **Duration:** 2 min
- **Started:** 2026-01-24T18:05:00Z
- **Completed:** 2026-01-24T18:07:00Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments

- Built-in logging middleware for state change observability
- Deep state comparison using CompareNETObjects library
- High-performance logging via LoggerMessage source generator
- Configurable store filtering (include/exclude patterns)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add NuGet dependencies** - `ad2f5ed` (chore)
2. **Task 2: Create LoggingMiddlewareOptions and LoggingExtensions** - `23df10b` (feat)
3. **Task 3: Create LoggingMiddleware** - `0c5c831` (feat)

## Files Created/Modified

- `src/Bustand/Bustand.csproj` - Added CompareNETObjects and Logging.Abstractions packages
- `src/Bustand/Middleware/LoggingMiddleware.cs` - Main middleware implementation with state diffing
- `src/Bustand/Middleware/LoggingMiddlewareOptions.cs` - Configuration for store filtering and max differences
- `src/Bustand/Middleware/LoggingExtensions.cs` - Source-generated log methods for high-perf logging

## Decisions Made

- **IOptions<T> nullable in constructor**: Allows LoggingMiddleware to work without DI registration of options (defaults to new instance)
- **IsEnabled guard first**: Check `_logger.IsEnabled(LogLevel.Debug)` before any diffing to ensure zero-cost when logging disabled
- **Store filter before diff**: Apply `ShouldLog(storeType)` check before expensive `Compare()` operation
- **CompareLogic per instance**: Each LoggingMiddleware instance owns its CompareLogic for thread safety

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- LoggingMiddleware ready for use in middleware pipelines
- Can be registered with `UseMiddleware<LoggingMiddleware<TState>>()`
- Options can be configured via DI or defaults work out of the box
- Ready for 03-04 DevTools integration (can complement or replace logging)

---
*Phase: 03-middleware-dx*
*Completed: 2026-01-24*
