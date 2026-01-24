# Pitfalls Research: Blazor State Management Library Development

**Domain:** Blazor state management library (Bustand - Zustand-inspired)
**Researched:** 2026-01-24
**Confidence:** MEDIUM-HIGH (verified with Microsoft docs + community patterns)

---

## Critical Pitfalls

Mistakes that cause rewrites or major architectural issues.

### Pitfall 1: Render Mode Amnesia

**What goes wrong:**
State management library works perfectly in one render mode (e.g., Blazor Server) but fails silently or crashes in others (WebAssembly, SSR, Auto). The library assumes a single execution model and breaks when components transition between modes.

**Why it happens:**
- Different render modes have fundamentally different lifecycles
- SSR components render once and never become interactive
- Auto mode starts on Server, then transitions to WASM mid-session
- Service lifetimes differ: Scoped in Server = per-circuit, Scoped in WASM = per-tab (effectively singleton)

**How to avoid:**
1. Design the state container interface to be mode-agnostic from day one
2. Use `RendererInfo.IsInteractive` and `RendererInfo.Name` to detect current mode at runtime
3. Test all four modes explicitly: Static SSR, Interactive Server, Interactive WebAssembly, Interactive Auto
4. Handle the Auto mode transition gracefully - state must serialize/deserialize when moving from Server to WASM

**Warning signs:**
- Unit tests only run in one hosting model
- No integration tests for mode transitions
- Components work in dev (Server) but fail in production (WASM)
- `InvalidOperationException` about missing services in specific modes

**Phase to address:**
Phase 1 (Foundation) - Core architecture must be mode-aware from the start

**Sources:**
- [Microsoft Render Modes Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0) (HIGH confidence)
- [Fluxor .NET 8 Issues](https://github.com/mrpmorris/Fluxor/issues/459) (MEDIUM confidence)

---

### Pitfall 2: Prerendering State Mismatch

**What goes wrong:**
Components fetch data during prerendering, display it briefly, then flash to empty/different state when hydration occurs. The infamous "double render" problem where OnInitializedAsync runs twice with different results.

**Why it happens:**
- Prerendering executes component lifecycle on server
- Hydration re-executes lifecycle on client (or new circuit)
- State from prerender is lost unless explicitly persisted
- Different services may return different values server vs. client

**How to avoid:**
1. Use `PersistentComponentState` to serialize state between prerender and hydration
2. In .NET 10, leverage the new `[PersistentState]` attribute for automatic persistence
3. Provide library-level hooks for state persistence (serialize on prerender, deserialize on hydration)
4. Document the prerendering story clearly - users need to understand this

**Warning signs:**
- UI flashes briefly then resets
- Data loads twice (visible in network tab or logs)
- Users report "ghost data" that disappears

**Phase to address:**
Phase 2 (Core Store) - State persistence must be designed into the store mechanism

**Sources:**
- [.NET 10 Persistent State Fix](https://www.telerik.com/blogs/net-10-preview-release-6-tackles-blazor-server-lost-state-problem) (MEDIUM confidence)
- [Blazor Rehydration Patterns](https://erwinkn.com/tech/blazor-rehydration/) (MEDIUM confidence)

---

### Pitfall 3: Synchronization Context Violations

**What goes wrong:**
State updates from background services, timers, or external events cause `InvalidOperationException: The current thread is not associated with the Dispatcher` or, worse, silent race conditions that corrupt state.

**Why it happens:**
- Blazor Server maintains a synchronization context per circuit
- StateHasChanged must be called on the renderer's sync context
- Background tasks, timers, SignalR callbacks run on thread pool threads
- WASM is single-threaded, masking issues that explode in Server mode

**How to avoid:**
1. All state change notifications must go through `InvokeAsync()`
2. Provide built-in support for background updates that handles dispatch internally
3. State container should wrap notification callbacks in proper dispatching
4. Test with actual background services and timers in Server mode

```csharp
// WRONG - will throw or cause race conditions
public void UpdateFromBackground(T newState)
{
    _state = newState;
    OnStateChanged?.Invoke(); // Called from wrong thread!
}

// RIGHT - library handles dispatching internally
public async Task UpdateFromBackgroundAsync(T newState)
{
    _state = newState;
    await InvokeAsync(() => OnStateChanged?.Invoke());
}
```

**Warning signs:**
- Works in WASM but throws in Server
- Intermittent "thread not associated with Dispatcher" errors
- State appears to update but UI doesn't reflect changes
- Race conditions that manifest as missing updates

**Phase to address:**
Phase 2 (Core Store) - Notification mechanism must handle sync context properly

**Sources:**
- [Microsoft Synchronization Context Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0) (HIGH confidence)
- [Blazor University - InvokeAsync](https://blazor-university.com/components/multi-threaded-rendering/invokeasync/) (MEDIUM confidence)

---

### Pitfall 4: Memory Leaks from Undisposed Subscriptions

**What goes wrong:**
Components subscribe to state changes but don't unsubscribe on disposal. In Blazor Server, circuits hold references to "dead" components, causing memory to grow continuously until app restart.

**Why it happens:**
- Event handlers maintain strong references to subscribers
- If event source (store) outlives subscriber (component), reference is held
- Blazor Server circuits can live for hours - accumulates dead subscriptions
- Transient services with IDisposable are NOT disposed in Blazor Server DI scope

**How to avoid:**
1. Require components to implement `IDisposable` and unsubscribe
2. Provide base component that auto-unsubscribes: `BustandComponent<TState>`
3. Use weak references for subscriptions where appropriate
4. Document the disposal requirement prominently
5. Consider providing analyzer/source generator to detect missing unsubscribe

```csharp
// Pattern users MUST follow
@implements IDisposable

@code {
    private IDisposable? _subscription;

    protected override void OnInitialized()
    {
        _subscription = Store.Subscribe(OnStateChanged);
    }

    public void Dispose()
    {
        _subscription?.Dispose(); // CRITICAL!
    }
}
```

**Warning signs:**
- Server memory grows steadily over time
- Components render for "disconnected" users
- Performance degrades after hours of uptime
- Memory profiler shows retained component references

**Phase to address:**
Phase 2 (Core Store) - Subscription API must return IDisposable, document disposal requirement

**Sources:**
- [Blazor Server Memory Management](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/) (MEDIUM confidence)
- [Microsoft Component Disposal](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0) (HIGH confidence)

---

### Pitfall 5: Shallow Immutability Illusion

**What goes wrong:**
Library uses C# records for state, advertising immutability. Users mutate nested reference types, corrupting state history and breaking time-travel debugging.

**Why it happens:**
- C# records only provide shallow immutability
- `with` expressions create shallow copies
- Nested classes/records within state are copied by reference
- Users assume "record = immutable" and mutate nested objects

```csharp
public record AppState(UserProfile User);
public record UserProfile(List<string> Permissions);

// This MUTATES the original state!
var newState = state with { };
newState.User.Permissions.Add("admin"); // Modifies BOTH states
```

**How to avoid:**
1. Document the shallow copy limitation prominently
2. Provide immutable collection wrappers or require `ImmutableList<T>`, `ImmutableDictionary<K,V>`
3. Consider deep clone utilities for complex state
4. In DevTools, detect and warn about mutation of "previous" states
5. Recommend record hierarchies all the way down

```csharp
// RECOMMENDED pattern
public record AppState(UserProfile User);
public record UserProfile(ImmutableList<string> Permissions);

// Safe with expression
var newState = state with
{
    User = state.User with
    {
        Permissions = state.User.Permissions.Add("admin")
    }
};
```

**Warning signs:**
- Time-travel shows "same" state at different points
- Undo doesn't work correctly
- State comparisons report equality when they shouldn't

**Phase to address:**
Phase 2 (Core Store) - State guidelines and optional deep-clone utilities
Phase 4 (DevTools) - Mutation detection in DevTools

**Sources:**
- [C# Records Deep Dive](https://amarozka.dev/csharp-record-types-immutable-data/) (MEDIUM confidence)
- [Microsoft Records Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) (HIGH confidence)

---

## Moderate Pitfalls

Mistakes that cause delays, poor DX, or technical debt.

### Pitfall 6: StateHasChanged Flooding

**What goes wrong:**
Every state change triggers re-render of every subscribed component, even when the slice of state that component cares about hasn't changed. Results in laggy UI with thousands of unnecessary renders.

**Why it happens:**
- Naive subscription model notifies all subscribers on any change
- No selector mechanism to filter relevant changes
- Components re-render for unrelated state updates
- Especially problematic with high-frequency updates (typing, dragging)

**How to avoid:**
1. Implement selector-based subscriptions (Zustand pattern)
2. Use equality comparers to skip unchanged state slices
3. Batch rapid updates (coalesce 100 updates/second into fewer renders)
4. Provide `subscribeWithSelector` that only notifies on actual changes

```csharp
// BAD - re-renders on ANY state change
var state = Store.UseStore();

// GOOD - only re-renders when Count changes
var count = Store.UseStore(s => s.Count);
```

**Prevention:**
- Benchmark with 100+ components and rapid updates
- Measure re-render counts in DevTools
- Default to selector-based subscriptions

**Phase to address:**
Phase 2 (Core Store) - Selector mechanism is core to performance

**Sources:**
- [Blazor Rendering Performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering?view=aspnetcore-10.0) (HIGH confidence)
- [Zustand Selector Pattern](https://tkdodo.eu/blog/working-with-zustand) (HIGH confidence)

---

### Pitfall 7: Heavy Assembly Scanning at Startup

**What goes wrong:**
Library scans all assemblies for stores/actions/reducers at startup. Works fine in dev, but production apps with many assemblies have 5-10 second startup delays.

**Why it happens:**
- Reflection-based discovery is convenient for DX
- Assembly scanning is O(n) where n = number of types in all assemblies
- Large enterprise apps have hundreds of assemblies
- Blazor WASM startup is already slow; this makes it worse

**How to avoid:**
1. Use source generators instead of runtime reflection
2. If reflection required, allow explicit registration as alternative
3. Scan only explicitly-marked assemblies, not all loaded
4. Cache discovery results (compile-time if possible)
5. Provide startup time metrics in DevTools

```csharp
// SLOW - scans everything
builder.Services.AddBustand(o => o.ScanAllAssemblies());

// FAST - explicit registration (source-generated)
builder.Services.AddBustand(o => o
    .AddStore<CounterStore>()
    .AddStore<TodoStore>());
```

**Prevention:**
- Benchmark startup time with 50+ assemblies
- Profile in Release mode, not Debug
- Provide both discovery modes

**Phase to address:**
Phase 3 (DX Features) - Auto-discovery should be opt-in, not default

---

### Pitfall 8: DI Service Lifetime Mismatch

**What goes wrong:**
Store registered as Singleton holds references to Scoped services, or Scoped store is accidentally shared between circuits. Results in cross-user data leakage or stale dependencies.

**Why it happens:**
- Blazor Server DI scopes are per-circuit, not per-request
- Singleton stores injecting Scoped services capture first circuit's services
- WASM treats Scoped as Singleton (same lifetime)
- Easy to accidentally register store with wrong lifetime

**How to avoid:**
1. Document service lifetime requirements clearly
2. Stores should be Scoped for Blazor Server (per-user state)
3. Stores can be Singleton only if truly app-wide shared state
4. Validate at startup: Singleton cannot depend on Scoped
5. Provide helper methods that enforce correct registration

```csharp
// Validates dependencies at registration time
builder.Services.AddBustandStore<TodoStore>(ServiceLifetime.Scoped);
```

**Prevention:**
- Integration tests that verify isolation between circuits
- Document "one store per user" vs "shared global store" patterns

**Phase to address:**
Phase 1 (Foundation) - DI integration must handle lifetimes correctly

**Sources:**
- [Blazor DI Scopes](https://www.thinktecture.com/en/blazor/dependency-injection-scopes-in-blazor/) (MEDIUM confidence)
- [Microsoft DI Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-10.0) (HIGH confidence)

---

### Pitfall 9: Middleware Complexity Explosion

**What goes wrong:**
Middleware pipeline becomes a tangled mess of interceptors that make debugging impossible. Actions pass through 7 middleware layers, each mutating or delaying, and developers can't trace what happened.

**Why it happens:**
- Middleware is powerful and tempting to overuse
- Each feature gets its own middleware: logging, persistence, throttling, validation...
- Order dependencies between middleware create hidden coupling
- No visibility into what middleware did to an action

**How to avoid:**
1. Limit built-in middleware to essential concerns (logging, devtools)
2. Provide clear ordering documentation
3. DevTools should visualize middleware pipeline execution
4. Consider simpler alternatives to middleware for common cases
5. Middleware should be opt-in, not on by default

**Prevention:**
- Review if middleware is really needed vs. simpler alternatives
- DevTools shows middleware chain and timing

**Phase to address:**
Phase 3 (DX Features) - Middleware is nice-to-have, not core

---

### Pitfall 10: DevTools Production Leakage

**What goes wrong:**
DevTools code ships to production, either exposing internal state to users or bloating bundle size unnecessarily.

**Why it happens:**
- Conditional compilation not used correctly
- DevTools dependencies included in main package
- No tree-shaking or dead code elimination
- `#if DEBUG` doesn't work in Blazor WASM as expected

**How to avoid:**
1. Separate NuGet package for DevTools: `Bustand.DevTools`
2. DevTools should not be referenced by main `Bustand` package
3. Use MSBuild conditions to exclude DevTools from Release builds
4. Verify production bundle doesn't contain DevTools code
5. Document: "Add DevTools only in Debug configuration"

```xml
<!-- In .csproj -->
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <PackageReference Include="Bustand.DevTools" Version="*" />
</ItemGroup>
```

**Prevention:**
- CI step that verifies DevTools not in production build
- Bundle size checks in release pipeline

**Phase to address:**
Phase 4 (DevTools) - DevTools must be in separate package from start

---

## Minor Pitfalls

Mistakes that cause annoyance but are recoverable.

### Pitfall 11: Inconsistent Naming Conventions

**What goes wrong:**
API uses mix of naming patterns: `Store.GetState()` vs `Store.State` property vs `Store.Use()`. Users constantly check docs for the right method.

**How to avoid:**
- Follow single convention: Zustand uses functions (`useStore`)
- In C#, prefer properties for accessors: `Store.State`
- Actions should be methods: `Store.Increment()`
- Document naming conventions in contribution guide

**Phase to address:**
Phase 2 (Core Store) - Establish conventions early

---

### Pitfall 12: Poor Error Messages

**What goes wrong:**
Errors say "State update failed" without context. Users can't diagnose issues without attaching debugger.

**How to avoid:**
- Include action name, current state type, and stack trace in errors
- Catch common mistakes and provide specific guidance
- "Did you forget to..." suggestions for frequent issues

**Phase to address:**
Phase 2 (Core Store) - Error handling strategy

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Global static store | Simple API | No DI, testing nightmares | Never |
| No selector support | Faster MVP | Performance crisis at scale | Never |
| Runtime reflection for discovery | Easy setup | Slow startup, AOT incompatible | Prototype only |
| Sync-only state updates | Simpler code | Blocks UI, poor perceived perf | Never for public API |
| Single-mode testing | Faster CI | Production failures | Never |
| Mutable state objects | Familiar patterns | Time-travel broken, race conditions | Never |

---

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Notify all subscribers | Lag on any state change | Selectors with memoization | 50+ subscribers |
| Deep equality checks | Slow comparisons | Shallow + reference equality | Nested state > 3 levels |
| Synchronous renders | Blocked UI on updates | Batching with RAF | 10+ updates/second |
| Full state serialization | Slow persistence | Incremental/partial serialization | State > 100KB |
| String-based action types | Typo-prone, no completion | Strongly-typed actions | 20+ action types |

---

## Security Considerations

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Exposing full state in DevTools production | State inspection by users | Separate DevTools package, Debug-only |
| Logging state with sensitive data | Credential exposure in logs | Redact sensitive fields in middleware |
| Cross-circuit state access (Server) | User data leakage | Scoped stores per circuit |
| State persistence without encryption | Data exposure in browser storage | Provide encrypted storage adapter |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **State subscriptions:** Often missing disposal - verify unsubscribe pattern documented
- [ ] **Render mode support:** Often missing Auto mode - verify transition from Server to WASM works
- [ ] **Prerendering:** Often missing persistence - verify state survives hydration
- [ ] **Background updates:** Often missing sync context - verify InvokeAsync wrapping
- [ ] **Selectors:** Often missing memoization - verify unchanged slices don't re-notify
- [ ] **NuGet package:** Often missing multi-target - verify net8.0 AND net9.0 AND net10.0
- [ ] **DevTools:** Often bundled with core - verify separate package exists
- [ ] **Tests:** Often single-mode only - verify Server AND WASM AND SSR tests exist

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Render mode incompatibility | HIGH | Abstract renderer access, add mode detection layer |
| Memory leaks from subscriptions | MEDIUM | Add weak reference support, provide migration guide |
| StateHasChanged flooding | MEDIUM | Add selector API, provide migration path |
| Shallow immutability issues | HIGH | Require immutable collections, breaking change |
| Assembly scanning slowness | LOW | Add explicit registration mode, make scanning opt-in |
| DevTools in production | LOW | Extract to separate package, semver major bump |

---

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Render mode amnesia | Phase 1 (Foundation) | Integration tests for all 4 modes |
| Prerendering state mismatch | Phase 2 (Core Store) | Test with prerender=true, verify no flash |
| Sync context violations | Phase 2 (Core Store) | Background service integration test |
| Subscription memory leaks | Phase 2 (Core Store) | Long-running Server test, memory profiling |
| Shallow immutability | Phase 2 (Core Store) | Document, provide immutable helpers |
| StateHasChanged flooding | Phase 2 (Core Store) | Benchmark 100+ subscribers with rapid updates |
| Assembly scanning | Phase 3 (DX Features) | Startup time benchmark with 50+ assemblies |
| DI lifetime mismatch | Phase 1 (Foundation) | Multi-circuit isolation test |
| Middleware complexity | Phase 3 (DX Features) | DevTools pipeline visualization |
| DevTools production leak | Phase 4 (DevTools) | CI check for bundle contents |

---

## Bustand-Specific Risk Mitigation

Based on stated project risks, specific mitigations:

| Risk Area | Specific Pitfall | Mitigation |
|-----------|------------------|------------|
| Multi-mode support | Pitfall 1, 2, 3 | Design mode-agnostic core; test matrix for all modes |
| Component re-rendering | Pitfall 6 | Selector-based subscriptions from day 1 |
| Middleware pipeline | Pitfall 9 | Keep middleware simple; optional, not default |
| DevTools communication | Server needs SignalR vs WASM different | Abstract transport; WebSocket for Server, postMessage for WASM |
| Auto-discovery | Pitfall 7 | Source generators primary; reflection optional |
| NuGet packaging | Multi-target issues | Target net8.0, net9.0, net10.0; CI matrix builds |
| Immutability | Pitfall 5 | Document; recommend ImmutableCollections; detect mutations in DevTools |

---

## Sources

**Official Documentation (HIGH confidence):**
- [ASP.NET Core Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)
- [Blazor performance best practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering?view=aspnetcore-10.0)
- [Blazor component disposal](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0)
- [Blazor synchronization context](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0)
- [Blazor dependency injection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-10.0)
- [C# Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)

**Community/Library Lessons (MEDIUM confidence):**
- [Fluxor .NET 8 issues](https://github.com/mrpmorris/Fluxor/issues/459)
- [Blazor Server memory management](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/)
- [Zustand selector patterns](https://tkdodo.eu/blog/working-with-zustand)
- [Blazor rehydration patterns](https://erwinkn.com/tech/blazor-rehydration/)

**Lower confidence (single source, unverified):**
- Various Medium articles on state management patterns
- Community discussions on render mode transitions

---
*Pitfalls research for: Blazor state management library (Bustand)*
*Researched: 2026-01-24*
