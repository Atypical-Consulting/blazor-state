# Phase 2: Core Store - Context

**Gathered:** 2026-01-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Developers can create stores, update state immutably, and components automatically re-render on changes. This phase delivers the core state management functionality that makes Bustand usable.

</domain>

<decisions>
## Implementation Decisions

### State update API
- **Set() overloads**: Support both full state replacement `Set(newState)` and updater function `Set(state => state with { ... })`
- **Immutability validation**: Trust developers (no validation) — assume records are used, prioritize performance
- **Async support**: Provide `SetAsync()` helper that handles InvokeAsync internally
- **Action methods**: Just public methods — developers write `public void Increment() => Set(s => s with { Count = s.Count + 1 })`

### Subscription granularity
- **Subscription model**: Selector-based subscription — `Subscribe(state => state.SomeProperty)` re-renders only when selected slice changes
- **Equality checking**: Reference equality by default (works naturally with C# records)
- **Lifecycle management**: Auto-dispose pattern — track component lifetime, auto-dispose when component disposes (safer default, prevents memory leaks)
- **Markup consumption**: Helper method pattern — `UseState(store => store.Count)` helper that subscribes + returns value

### Re-render behavior
- **Update triggering**: Immediate StateHasChanged() on Set() — simple, predictable
- **Background threads**: Automatic InvokeAsync — store detects context and calls InvokeAsync if needed (safer default)
- **Render loop protection**: Detect and throw exception — track render phase, throw if Set() called during StateHasChanged()
- **Disposal edge case**: Silently ignore subscriptions during component disposal (graceful degradation)

### Store initialization
- **Initial state definition**: Abstract property override — `protected abstract TState InitialState { get; }` (cleaner API, enforces initialization)
- **Async initialization**: Optional `InitializeAsync()` virtual hook for async setup
- **InitializeAsync() timing**: Automatically called on first DI resolve (better ergonomics)
- **Pre-initialization access**: Return InitialState immediately if accessed before InitializeAsync() completes (resilient, allows progressive loading)

### Claude's Discretion
- Exact implementation of render loop detection mechanism
- Internal subscription tracking data structures
- Error message wording and diagnostics

</decisions>

<specifics>
## Specific Ideas

- API should feel natural to C# developers using records with `with` expressions
- SetAsync() should make background updates "just work" without developers thinking about InvokeAsync
- UseState() pattern should be as clean as React hooks for component consumption

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-core-store*
*Context gathered: 2026-01-24*
