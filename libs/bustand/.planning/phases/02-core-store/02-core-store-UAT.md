---
status: complete
phase: 02-core-store
source: 02-01-SUMMARY.md, 02-02-SUMMARY.md, 02-03-SUMMARY.md, 02-04-SUMMARY.md
started: 2026-01-24T13:00:00Z
updated: 2026-01-24T13:05:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Define store with InitialState property
expected: Create a new store by inheriting from ZustandStore<TState> with a record state type. Define the InitialState as an abstract property implementation. The store should compile and be usable with minimal boilerplate (under 10 lines).
result: pass

### 2. Update state using Set with mutator function
expected: Call Set() with a mutator function (e.g., state => state with { Count = state.Count + 1 }). The state should update immutably and the new state should be accessible.
result: pass

### 3. Update state using Set with direct replacement
expected: Call Set() with a new state object directly (e.g., Set(new CounterState(42))). The state should be replaced entirely with the provided object.
result: pass

### 4. Update state from background thread using SetAsync
expected: Call SetAsync from a background thread (Task.Run). The state update should marshal to the UI thread automatically without crashes or errors, particularly in Blazor Server mode.
result: pass

### 5. Render loop protection throws exception
expected: Attempt to call Set() during component render (between BeginRender and EndRender). A RenderLoopException should be thrown with a clear error message explaining the infinite loop risk.
result: pass

### 6. Async initialization via InitializeAsync
expected: Override InitializeAsync in a store to perform async setup (e.g., loading data). Call EnsureInitializedAsync before using the store. The IsInitialized property should be true after completion, and subsequent calls should be idempotent.
result: pass

### 7. Subscribe to full state changes
expected: Call Subscribe(callback) on a store. When state changes via Set(), the callback should be invoked with the new state.
result: pass

### 8. Subscribe to state slice with selector
expected: Call Subscribe<TSlice>(selector, callback) with a selector function (e.g., state => state.Count). When that specific property changes, the callback should be invoked. When other properties change, the callback should NOT be invoked (reference equality filtering).
result: pass

### 9. Unsubscribe stops receiving notifications
expected: Create a subscription, then call Unsubscribe() on the returned ISubscription. After unsubscribing, state changes should NOT trigger the callback anymore.
result: pass

### 10. Component auto-renders on state change (DI store)
expected: Create a component inheriting from ZustandComponent<TStore, TState> with [Inject] store. Use UseState() or UseState<TSlice>(selector) in the component. When the store state changes, the component should automatically re-render with the new state displayed.
result: pass

### 11. Component auto-renders on state change (CascadingParameter store)
expected: Wrap a component inheriting from ZustandComponentScoped<TStore, TState> with a ZustandScope<TStore, TState>. The scoped component should receive the store via CascadingParameter. When state changes, the component should automatically re-render.
result: pass

### 12. UseState with selector only re-renders on slice change
expected: Use UseState<TSlice>(selector) in a component to subscribe to a specific property. When that property changes, the component should re-render. When other properties change, the component should NOT re-render (optimized rendering).
result: pass

### 13. Component subscriptions dispose on component disposal
expected: Render a component that subscribes to a store. Dispose the component. The subscription should be cleaned up automatically (SubscriptionCount on store should decrease), preventing memory leaks.
result: pass

### 14. ZustandScope provides isolated store instance
expected: Wrap multiple components with separate ZustandScope instances for the same store type. Each scope should have its own isolated store instance with independent state (changes in one scope don't affect the other).
result: pass

### 15. No @rendermode directives in library components
expected: Inspect ZustandComponent, ZustandComponentScoped, and ZustandScope source files. None of these components should specify a @rendermode directive (mode-agnostic design per MODE-07).
result: pass

### 16. Graceful disposal during async callbacks
expected: Trigger an async state change, then dispose the component before the callback completes. The system should handle the ObjectDisposedException gracefully without crashing or logging errors.
result: pass

## Summary

total: 16
passed: 16
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
