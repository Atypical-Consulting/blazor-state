---
status: complete
phase: 03-middleware-dx
source: [03-01-SUMMARY.md, 03-02-SUMMARY.md, 03-03-SUMMARY.md, 03-04-SUMMARY.md]
started: 2026-01-24T19:30:00Z
updated: 2026-01-24T19:35:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Create custom middleware that intercepts Set() calls
expected: Developer creates a class implementing IMiddleware<TState> with OnBeforeChange and OnAfterChange methods. The middleware is registered via services.AddBustand().UseMiddleware<MyMiddleware>(). When Set() is called on the store, the middleware's OnBeforeChange runs before the state change, and OnAfterChange runs after. Middleware receives MiddlewareContext with OldState, NewState, StoreType, ActionName, and Timestamp.
result: pass

### 2. Multiple middleware execute in configured order
expected: Developer registers multiple middleware via chained .UseMiddleware<FirstMiddleware>().UseMiddleware<SecondMiddleware>() calls. When Set() is called, the middleware execute in the order they were registered (FIFO). BeforeChange hooks run first-to-last, then state changes, then AfterChange hooks run first-to-last.
result: pass

### 3. Middleware can block state changes
expected: Developer creates middleware where OnBeforeChange returns false under certain conditions. When the blocking condition is met and Set() is called, the state change is prevented and the state remains unchanged. AfterChange hooks do not run for blocked changes.
result: pass

### 4. Logging middleware logs state changes to console
expected: Developer registers LoggingMiddleware via .UseMiddleware<LoggingMiddleware<TState>>(). When Set() is called, the middleware logs the state change to the configured ILogger with Debug level. The log output includes the old state, new state, and differences between them.
result: pass

### 5. Logging middleware store filtering works
expected: Developer configures LoggingMiddlewareOptions with IncludedStores or ExcludedStores patterns. When Set() is called on stores matching the filter, they are logged. Stores not matching the filter are not logged (zero overhead for filtered-out stores).
result: pass

### 6. Logging middleware is zero-cost when logging disabled
expected: When the ILogger has Debug level disabled, the logging middleware does not compute diffs or perform expensive operations. IsEnabled check short-circuits the middleware logic for performance.
result: pass

### 7. AddBustand auto-discovers and registers all stores
expected: Developer calls services.AddBustand() without manually registering individual stores. All stores in the assembly marked with [BustandStore] attribute are automatically discovered and registered with DI. Unattributed stores are not registered.
result: pass

### 8. Middleware registration via fluent API
expected: Developer chains multiple .UseMiddleware<T>() calls on BustandOptions when calling AddBustand(). Each middleware type is registered and injected into the appropriate stores. The fluent API returns this for method chaining.
result: pass

### 9. Action names captured automatically via CallerMemberName
expected: When Set() is called from a method, the MiddlewareContext.ActionName automatically captures the caller's method name via [CallerMemberName] attribute. Developer can see which method triggered the state change in logs or middleware hooks without manual annotation.
result: pass

### 10. BeforeChange exceptions bubble to caller
expected: When middleware OnBeforeChange throws an exception (e.g., validation failure), the exception propagates to the caller of Set(). The state change is aborted and the exception is visible for proper error handling.
result: pass

### 11. AfterChange exceptions are logged and continue pipeline
expected: When middleware OnAfterChange throws an exception, the exception is logged via System.Diagnostics.Debug and the pipeline continues executing remaining AfterChange hooks. One failing side effect does not break other middleware.
result: pass

### 12. Test suite passes with all middleware tests
expected: Running `dotnet test` executes all tests including the 18 new middleware tests (MiddlewarePipelineTests, LoggingMiddlewareTests, MiddlewareIntegrationTests). All 91 tests pass with no failures.
result: pass

## Summary

total: 12
passed: 12
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
