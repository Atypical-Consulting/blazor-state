# Architecture Research: Blazor State Management Libraries

**Domain:** Blazor state management library (Zustand-inspired)
**Researched:** 2026-01-24
**Confidence:** MEDIUM-HIGH

## System Overview

```
+-----------------------------------------------------------------------------+
|                           BLAZOR APPLICATION                                  |
+-----------------------------------------------------------------------------+
|  COMPONENT LAYER                                                              |
|  +----------+   +----------+   +----------+   +----------+                   |
|  | Counter  |   | Profile  |   | Settings |   | TodoList |   (Components)    |
|  +----+-----+   +----+-----+   +----+-----+   +----+-----+                   |
|       |              |              |              |                          |
|       +------+-------+-------+------+              |                          |
|              |               |                     |                          |
|              v               v                     v                          |
+-----------------------------------------------------------------------------+
|  SCOPE LAYER (ZustandScope - CascadingValue)                                 |
|  +-----------------------------------------------------------------------+   |
|  | Provides: Store Registry, Change Notification, Scope Isolation        |   |
|  +-----------------------------------------------------------------------+   |
+-----------------------------------------------------------------------------+
|  STORE LAYER                                                                 |
|  +----------------+  +----------------+  +----------------+                   |
|  | CounterStore   |  | UserStore      |  | TodoStore      |  (Stores)        |
|  | State: TState  |  | State: TState  |  | State: TState  |                  |
|  | Set()          |  | Set()          |  | Set()          |                  |
|  | Subscribe()    |  | Subscribe()    |  | Subscribe()    |                  |
|  +-------+--------+  +-------+--------+  +-------+--------+                  |
|          |                   |                   |                           |
|          +--------+----------+----------+--------+                           |
|                   |                     |                                    |
|                   v                     v                                    |
+-----------------------------------------------------------------------------+
|  MIDDLEWARE PIPELINE                                                         |
|  +---------+   +---------+   +---------+   +---------+                       |
|  | Logging |-->| Persist |-->| DevTools|-->| Custom  |   (Interceptors)     |
|  +---------+   +---------+   +---------+   +---------+                       |
+-----------------------------------------------------------------------------+
|  DEVTOOLS SERVICE                                                            |
|  +-----------------------------------------------------------------------+   |
|  | State Snapshots | Action History | Time Travel | Browser Extension    |   |
|  +-----------------------------------------------------------------------+   |
+-----------------------------------------------------------------------------+
```

## Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **ZustandStore<TState>** | Holds state, provides Set() for mutations, manages subscriptions | Abstract base class that stores implement |
| **ZustandScope** | Provides cascading scope context, manages store lifecycle, enables disposal | CascadingValue wrapper component |
| **IStoreSubscription** | Component interface for subscribing to state changes | Interface with OnStateChanged event |
| **MiddlewarePipeline** | Intercepts Set() calls, chains middleware | Delegate chain pattern |
| **IMiddleware** | Individual middleware interceptor | Interface with Before/After methods |
| **DevToolsService** | Captures state snapshots, broadcasts to dev tools | Singleton service with JS interop or SignalR |
| **StoreRegistry** | Tracks all active stores for auto-discovery | Scoped service populated via Scrutor |

## Recommended Project Structure

```
src/
Bustand/
+-- Core/                          # Core abstractions (no Blazor dependency)
|   +-- ZustandStore.cs            # Abstract base class for stores
|   +-- IStoreState.cs             # State marker interface
|   +-- StateChangedEventArgs.cs   # Event args for state changes
|   +-- Selectors/
|       +-- Selector.cs            # Selector for derived state
|       +-- SelectorEqualityComparer.cs
|
+-- Middleware/                    # Middleware pipeline
|   +-- IMiddleware.cs             # Middleware interface
|   +-- MiddlewarePipeline.cs      # Pipeline execution engine
|   +-- MiddlewareContext.cs       # Context passed to middleware
|   +-- Built-in/
|       +-- LoggingMiddleware.cs
|       +-- PersistMiddleware.cs
|       +-- DevToolsMiddleware.cs
|
+-- Components/                    # Blazor integration
|   +-- ZustandScope.razor         # CascadingValue scope component
|   +-- ZustandComponent.cs        # Base component with auto-subscription
|   +-- UseStore.cs                # Hook-like helper for functional approach
|
+-- DependencyInjection/           # DI registration
|   +-- ServiceCollectionExtensions.cs
|   +-- ZustandOptions.cs
|   +-- StoreRegistry.cs
|
+-- DevTools/                      # Developer tools integration
    +-- DevToolsService.cs         # State capture & broadcast
    +-- IDevToolsTransport.cs      # Abstraction for communication
    +-- Transports/
        +-- JSInteropTransport.cs  # For WebAssembly (JS interop)
        +-- SignalRTransport.cs    # For Server mode
```

### Structure Rationale

- **Core/:** Framework-agnostic state logic, enables unit testing without Blazor
- **Middleware/:** Separated concern for intercepting state changes
- **Components/:** Blazor-specific integration, kept minimal
- **DependencyInjection/:** Clean registration, Scrutor integration point
- **DevTools/:** Optional feature, abstracted transport for render mode flexibility

## Architectural Patterns

### Pattern 1: Unidirectional Data Flow (Flux-inspired)

**What:** State changes flow in one direction: Action -> Store.Set() -> Middleware -> State Update -> Subscriber Notification -> Component Re-render

**When to use:** Always - this is the core pattern for predictable state management

**Trade-offs:**
- Pros: Predictable, debuggable, time-travel possible
- Cons: More indirection than direct mutation

**Example:**
```csharp
// Store definition
public class CounterStore : ZustandStore<CounterState>
{
    public CounterStore() : base(new CounterState(Count: 0)) { }

    public void Increment()
    {
        // Set() goes through middleware pipeline
        Set(state => state with { Count = state.Count + 1 });
    }
}

// State is immutable record
public record CounterState(int Count);
```

### Pattern 2: Subscription-Based Component Updates

**What:** Components subscribe to specific stores and re-render only when subscribed state changes. Uses IDisposable pattern for cleanup.

**When to use:** For any component consuming store state

**Trade-offs:**
- Pros: Granular re-renders, memory-safe with proper disposal
- Cons: Requires explicit subscription management

**Example:**
```csharp
// Component subscribing to store
@implements IDisposable
@inject CounterStore CounterStore

<p>Count: @CounterStore.State.Count</p>
<button @onclick="CounterStore.Increment">+</button>

@code {
    protected override void OnInitialized()
    {
        CounterStore.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        CounterStore.StateChanged -= OnStateChanged;
    }
}
```

### Pattern 3: Selector-Based Optimization

**What:** Selectors derive computed values from state and use equality comparison to prevent unnecessary re-renders

**When to use:** When components only need part of the state or derived values

**Trade-offs:**
- Pros: Prevents unnecessary re-renders, better performance
- Cons: Additional complexity, selector definition overhead

**Example:**
```csharp
// Selector that only triggers when specific value changes
var completedCount = todoStore.Select(
    state => state.Todos.Count(t => t.IsComplete),
    EqualityComparer<int>.Default
);

// Component only re-renders when completedCount changes
```

### Pattern 4: Middleware Pipeline (Interceptor Chain)

**What:** Each Set() call passes through a chain of middleware that can observe, modify, or reject state changes

**When to use:** For cross-cutting concerns (logging, persistence, DevTools integration)

**Trade-offs:**
- Pros: Clean separation of concerns, extensible
- Cons: Debugging middleware order issues, performance overhead for many middleware

**Example:**
```csharp
public class LoggingMiddleware : IMiddleware
{
    public async Task<TState> InvokeAsync<TState>(
        MiddlewareContext<TState> context,
        Func<Task<TState>> next)
    {
        Console.WriteLine($"Before: {context.CurrentState}");
        var newState = await next();
        Console.WriteLine($"After: {newState}");
        return newState;
    }
}
```

## Data Flow

### State Change Flow

```
[Component Action]
        |
        v
[Store.Set(mutator)]
        |
        v
[Middleware Pipeline] --> [DevTools: Capture snapshot]
        |                  [Persist: Save to storage]
        |                  [Logging: Log changes]
        v
[State Updated (immutable)]
        |
        v
[StateChanged Event Raised]
        |
        +---> [Subscriber 1: InvokeAsync(StateHasChanged)]
        +---> [Subscriber 2: InvokeAsync(StateHasChanged)]
        +---> [Subscriber N: InvokeAsync(StateHasChanged)]
        |
        v
[Components Re-render]
```

### Multi-Mode Rendering Considerations

```
+-------------------+-------------------+-------------------+
|   STATIC SSR      |  INTERACTIVE      |  INTERACTIVE      |
|                   |  SERVER           |  WEBASSEMBLY      |
+-------------------+-------------------+-------------------+
| No interactivity  | SignalR circuit   | In-browser        |
| State per-request | State per-circuit | State in WASM     |
| Stores: Transient | Stores: Scoped    | Stores: Scoped    |
|                   | (circuit-scoped)  | (app lifetime)    |
+-------------------+-------------------+-------------------+
| DevTools: N/A     | DevTools: SignalR | DevTools: JS      |
| (no events)       | to browser ext    | interop direct    |
+-------------------+-------------------+-------------------+
```

### Key Data Flows

1. **Component -> Store:** Components call store methods (actions) that internally call Set()
2. **Store -> Middleware:** Set() invokes the middleware pipeline with current and proposed state
3. **Middleware -> DevTools:** DevTools middleware captures state snapshots and broadcasts
4. **Store -> Subscribers:** After state update, StateChanged event notifies all subscribers
5. **Subscriber -> Component:** Subscribers call InvokeAsync(StateHasChanged) to trigger re-render

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Small app (<10 stores) | Simple approach: single ZustandScope at root, all stores scoped |
| Medium app (10-50 stores) | Consider: lazy store initialization, selector optimization |
| Large app (50+ stores) | Consider: nested scopes for module isolation, store slicing patterns |

### Scaling Priorities

1. **First bottleneck:** Component re-render frequency. Fix with selectors and fine-grained subscriptions.
2. **Second bottleneck:** Memory from storing all state snapshots in DevTools. Fix with configurable snapshot limits.
3. **Third bottleneck:** Middleware pipeline overhead. Fix with conditional middleware execution.

## Anti-Patterns

### Anti-Pattern 1: Direct State Mutation

**What people do:** Modify state object properties directly without going through Set()
**Why it's wrong:** Bypasses middleware, breaks change detection, no DevTools history
**Do this instead:** Always use Set() with immutable updates (records with `with` expressions)

### Anti-Pattern 2: Forgetting to Unsubscribe

**What people do:** Subscribe to StateChanged in OnInitialized without implementing IDisposable
**Why it's wrong:** Memory leak, components continue receiving updates after disposal
**Do this instead:** Always implement IDisposable and unsubscribe in Dispose()

### Anti-Pattern 3: Calling StateHasChanged Directly (Not InvokeAsync)

**What people do:** Call StateHasChanged() directly in event handler from store
**Why it's wrong:** Crashes in Blazor Server when not on render thread
**Do this instead:** Always use `InvokeAsync(StateHasChanged)` for thread safety

### Anti-Pattern 4: Storing Non-Serializable State

**What people do:** Store Func<>, Action, HttpClient, or other non-serializable objects in state
**Why it's wrong:** Breaks persistence, DevTools, and prerendering scenarios
**Do this instead:** Store only serializable data; inject services separately

### Anti-Pattern 5: Global Singleton Stores in Multi-Tenant Server Mode

**What people do:** Register stores as Singleton in Blazor Server
**Why it's wrong:** State bleeds between user circuits, security vulnerability
**Do this instead:** Use Scoped lifetime (per-circuit in Server mode)

## Integration Points

### Blazor Lifecycle Integration

| Lifecycle Method | State Management Hook |
|------------------|----------------------|
| `OnInitialized(Async)` | Subscribe to stores, initialize from persisted state |
| `OnParametersSet(Async)` | React to parameter changes that affect state |
| `OnAfterRender(Async)` | N/A (state changes here cause infinite loops) |
| `Dispose` | Unsubscribe from all stores |

### External Service Integration

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| Browser DevTools | JS Interop (WASM) / SignalR (Server) | Abstracted via IDevToolsTransport |
| Local Storage | PersistMiddleware with IJSRuntime | Consider encryption for sensitive data |
| Backend APIs | Effects/Actions, not in middleware | Middleware for sync operations only |
| SignalR (real-time) | Effects subscribe to hub events | Dispatch actions on hub messages |

### Internal Component Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Component <-> Store | Direct method calls + events | Store methods are actions |
| Store <-> Middleware | Pipeline delegation | Middleware has access to context |
| Middleware <-> DevTools | Service injection | DevTools service is injected |
| Scope <-> Stores | CascadingValue + Registry | Scope manages store lifecycle |

## Render Mode-Specific Considerations

### Static SSR Mode

- **State:** Transient, reset on each request
- **Events:** Not triggered (no interactivity)
- **DevTools:** Not applicable
- **Stores:** Can be instantiated but mutations have no effect on UI
- **Recommendation:** Use minimal store interaction; consider hydration for transition to interactive mode

### Interactive Server Mode

- **State:** Scoped to SignalR circuit (user session)
- **Events:** Fully functional via SignalR
- **DevTools:** Requires SignalR-based transport to browser extension
- **Stores:** Circuit-scoped, persist across navigation within session
- **Recommendation:** Use .NET 10's `[PersistentState]` for circuit recovery
- **Gotcha:** InvokeAsync required for StateHasChanged

### Interactive WebAssembly Mode

- **State:** Lives in browser memory (WASM runtime)
- **Events:** Fully functional, local execution
- **DevTools:** JS interop to Redux DevTools extension
- **Stores:** Application-scoped (until page refresh)
- **Recommendation:** Leverage local storage persistence for refresh survival

### Interactive Auto Mode

- **State:** Initially Server, then transitions to WASM
- **Events:** Work in both modes
- **DevTools:** Must support both transports
- **Stores:** State must be serializable for prerender -> hydration
- **Gotcha:** Component may initialize twice (prerender + hydrate)
- **Recommendation:** Use PersistentComponentState for seamless transition

## Build Order Dependencies

Based on the architecture above, the recommended build order is:

```
Phase 1: Core Store Foundation
  +-- ZustandStore<TState> base class
  +-- State change notification (event)
  +-- Basic Set() method

Phase 2: Blazor Integration
  +-- ZustandScope component (depends on Phase 1)
  +-- IDisposable subscription pattern
  +-- InvokeAsync wrapper

Phase 3: Middleware Pipeline (depends on Phase 1)
  +-- IMiddleware interface
  +-- MiddlewarePipeline execution
  +-- Set() integration with pipeline

Phase 4: Auto-Discovery (depends on Phase 2)
  +-- StoreRegistry
  +-- Scrutor integration
  +-- ServiceCollection extensions

Phase 5: Selector Optimization (depends on Phase 1)
  +-- Selector<TState, TResult>
  +-- Equality comparison
  +-- Memoization

Phase 6: DevTools Integration (depends on Phase 3)
  +-- DevToolsService
  +-- IDevToolsTransport abstraction
  +-- JSInteropTransport (WASM)
  +-- SignalRTransport (Server)

Phase 7: Built-in Middleware (depends on Phase 3, 6)
  +-- LoggingMiddleware
  +-- PersistMiddleware
  +-- DevToolsMiddleware
```

## Sources

### Official Documentation (HIGH confidence)
- [Microsoft Blazor Render Modes (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)
- [Microsoft Blazor Component Lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0)
- [Microsoft Blazor State Management Overview](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/?view=aspnetcore-10.0)
- [Microsoft Blazor Server-Side State Management (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/server?view=aspnetcore-10.0)

### Library References (MEDIUM-HIGH confidence)
- [Fluxor GitHub Repository](https://github.com/mrpmorris/Fluxor)
- [TimeWarp.State GitHub Repository](https://github.com/TimeWarpEngineering/timewarp-state)
- [Zustand Documentation](https://zustand.docs.pmnd.rs/)
- [Scrutor GitHub Repository](https://github.com/khellang/Scrutor)

### Community Resources (MEDIUM confidence)
- [Code Maze: Fluxor for State Management in Blazor](https://code-maze.com/fluxor-for-state-management-in-blazor/)
- [Chris Sainty: 3 Ways to Communicate Between Components](https://chrissainty.com/3-ways-to-communicate-between-components-in-blazor/)
- [Telerik: Blazor Render Modes in .NET 8](https://www.telerik.com/blogs/blazor-basics-blazor-render-modes-net-8)

### .NET 10 Specific (MEDIUM confidence - recent feature)
- [Building Resilient Blazor Server Apps with Persistent State in .NET 10](https://atalupadhyay.wordpress.com/2025/06/11/building-resilient-blazor-server-apps-with-persistent-state-in-net-10/)
- [GitHub Issue: Persisting circuit state for Blazor applications](https://github.com/dotnet/aspnetcore/issues/60494)

---
*Architecture research for: Bustand - Zustand-inspired Blazor state management*
*Researched: 2026-01-24*
