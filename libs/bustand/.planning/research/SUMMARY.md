# Project Research Summary

**Project:** Bustand - Blazor State Management Library
**Domain:** Blazor state management library (Zustand-inspired)
**Researched:** 2026-01-24
**Confidence:** MEDIUM-HIGH

## Executive Summary

Bustand is a Blazor state management library inspired by Zustand's minimal API design, positioned to compete with existing solutions (Fluxor, TimeWarp.State, EasyAppDev.Blazor.Store) by combining simplicity with exceptional debugging tools. Research shows that Zustand's core value proposition—minimal boilerplate without Redux ceremony—translates well to C# using records and a simple `Set()` API. The recommended approach is to build a lightweight core using .NET 10's features (LTS until 2028), multi-targeting .NET 8 and .NET 10 for maximum adoption, with a clean separation between the core library and DevTools package.

The key technical challenge is Blazor's multi-mode rendering system. Unlike JavaScript libraries that target a single runtime, Bustand must work seamlessly across Static SSR, Interactive Server, Interactive WebAssembly, and Interactive Auto modes—each with different state lifetimes, threading models, and persistence requirements. Research identifies critical pitfalls: render mode incompatibility, prerendering state mismatches, synchronization context violations, memory leaks from undisposed subscriptions, and shallow immutability illusions. These can be mitigated through mode-agnostic architecture from day one, proper use of `InvokeAsync()`, explicit subscription disposal patterns, and careful documentation of C# record limitations.

The recommended stack is deliberately minimal: .NET 10 with Blazor components, Scrutor for assembly scanning, System.Text.Json for serialization, bUnit for testing, and xUnit as the test framework. The architecture follows Zustand's unidirectional data flow with subscription-based updates, a middleware pipeline for extensibility, and selector-based optimizations to prevent render flooding. The differentiating feature—a built-in DevTools page that works without browser extensions—positions Bustand uniquely for debugging scenarios where Redux DevTools can't be used (MAUI Hybrid, restricted environments).

## Key Findings

### Recommended Stack

The stack is built on .NET 10 LTS (support until Nov 2028) with multi-targeting to .NET 8 for backward compatibility. Blazor's unified rendering model and the new `[PersistentState]` attribute for state persistence across prerendering are key .NET 10 features that inform the architecture.

**Core technologies:**
- **.NET 10 (multi-target with .NET 8):** LTS release with Blazor unified rendering, `[PersistentState]` attribute for prerendering scenarios, C# 13 record improvements
- **Microsoft.NET.Sdk.Razor:** Required for Blazor component compilation, enables `.razor` files and source generation
- **Scrutor 7.0.0:** Assembly scanning for auto-discovering stores via `IServiceCollection.Scan()`, avoiding manual registration boilerplate
- **System.Text.Json 10.0.x:** DevTools state serialization with source generators for AOT compatibility
- **bUnit 2.5.3 + xUnit 2.9.x:** Blazor component testing with semantic HTML comparison, parallel test execution

**Critical stack decisions:**
- Multi-target `net8.0;net10.0` for maximum adoption (do NOT target netstandard2.0—Blazor requires framework-specific APIs)
- Separate DevTools package (`Bustand.DevTools`) to prevent production bloat
- Use Scrutor for optional auto-discovery but provide explicit registration for source generator compatibility
- MinVer for Git-based semantic versioning
- Microsoft.SourceLink.GitHub for NuGet package debugging

### Expected Features

Research shows users expect a specific set of features based on existing state management patterns, with clear differentiation between "must have" table stakes and competitive differentiators.

**Must have (table stakes):**
- Centralized State Store — single source of truth, expected by all users
- Immutable State Updates — C# records with `with` expressions are idiomatic
- Component Subscription/Notification — automatic re-render via `StateHasChanged()` integration
- Async Action Support — modern .NET requires async operations for API calls
- Dependency Injection Integration — .NET developers expect DI; non-DI approach would feel alien
- Multi-Hosting Mode Support — must work in Server, WebAssembly, and Auto modes or adoption is limited
- Basic TypeScript-like Type Safety — generic `ZustandStore<TState>` pattern for compile-time safety
- Initial State Configuration — sensible defaults on store creation

**Should have (competitive differentiators):**
- Minimal Boilerplate API — Zustand's killer feature: no Actions/Reducers, just `Set()`
- Selective Subscriptions (Selectors) — prevent cascade re-renders by only updating when specific slices change
- Redux DevTools Integration — time-travel debugging, action logging, state inspection
- Built-in Middleware Pipeline — extensibility for logging, validation, persistence
- Persistence Middleware (LocalStorage) — state survives browser refresh
- DevTools Inspector Page — dedicated Blazor page for state inspection without browser extension (unique differentiator)
- Time-Travel Debugging — step through state history, identify when bugs were introduced
- State Diff Visualization — color-coded changes between states
- Auto-Discovery of Stores — reflection/source generators reduce registration boilerplate

**Defer (v2+):**
- Cross-Tab Synchronization — nice-to-have, adds complexity
- Undo/Redo History — specialized use case (forms, editors)
- Optimistic Updates with Rollback — advanced pattern users can implement with basic tools
- Computed/Derived State — add when users have expensive calculations

**Anti-features (explicitly avoid):**
- Mandatory Redux/Flux Pattern — massive boilerplate, defeats Zustand's purpose
- Action Classes/Records Required — boilerplate explosion
- Bidirectional Data Binding — hard to debug, unpredictable
- Global Singleton Store Only — causes cross-user data leaks in Blazor Server
- Automatic INotifyPropertyChanged — subscription overhead
- Built-in Forms Integration — scope creep, form libraries already exist

### Architecture Approach

The architecture follows a unidirectional data flow pattern (Flux-inspired) with subscription-based component updates, selector-based optimization, and a middleware pipeline for cross-cutting concerns. The design must be mode-agnostic from day one to support all Blazor rendering modes.

**Major components:**
1. **ZustandStore<TState>** — Abstract base class holding state, providing `Set()` for mutations, managing subscriptions via events
2. **ZustandScope** — CascadingValue wrapper managing store lifecycle, enabling disposal and scope isolation
3. **MiddlewarePipeline** — Intercepts `Set()` calls with delegate chain pattern, enables logging, persistence, DevTools
4. **IStoreSubscription** — Component interface for subscribing to state changes with `OnStateChanged` event
5. **DevToolsService** — Captures state snapshots, broadcasts to dev tools via abstracted transport (SignalR for Server, JS interop for WASM)
6. **StoreRegistry** — Tracks all active stores for auto-discovery, populated via Scrutor assembly scanning
7. **Selector<TState, TResult>** — Derives computed values from state with equality comparison to prevent unnecessary re-renders

**Key data flow:**
Component Action → Store.Set(mutator) → Middleware Pipeline (DevTools, Persist, Logging) → State Updated (immutable) → StateChanged Event → Subscribers call InvokeAsync(StateHasChanged) → Components Re-render

**Multi-mode considerations:**
- Static SSR: Transient stores, no events (not interactive)
- Interactive Server: Scoped stores per SignalR circuit, requires InvokeAsync for thread safety
- Interactive WebAssembly: Scoped stores in browser, JS interop for DevTools
- Interactive Auto: State serializable for prerender→hydration, uses PersistentComponentState

### Critical Pitfalls

Research identified 12+ pitfalls; these are the top 5 that require architectural prevention:

1. **Render Mode Amnesia** — Library works in one mode but fails in others. Prevention: Mode-agnostic interface design from day one, use `RendererInfo.IsInteractive` to detect mode, test all four modes (SSR, Server, WASM, Auto) explicitly. Phase to address: Phase 1 (Foundation).

2. **Prerendering State Mismatch** — Components flash to empty state after hydration because prerender state is lost. Prevention: Use .NET 10's `[PersistentState]` attribute, provide library hooks for state persistence, serialize on prerender and deserialize on hydration. Phase to address: Phase 2 (Core Store).

3. **Synchronization Context Violations** — Background updates crash with "thread not associated with Dispatcher" in Server mode. Prevention: All state change notifications must use `InvokeAsync()`, state container wraps callbacks in proper dispatching, test with actual background services in Server mode. Phase to address: Phase 2 (Core Store).

4. **Memory Leaks from Undisposed Subscriptions** — Components subscribe but don't unsubscribe, Blazor Server circuits hold "dead" component references. Prevention: Require IDisposable pattern, provide base component that auto-unsubscribes (`BustandComponent<TState>`), use weak references where appropriate, document disposal prominently. Phase to address: Phase 2 (Core Store).

5. **Shallow Immutability Illusion** — C# records provide only shallow immutability; users mutate nested reference types, corrupting time-travel. Prevention: Document limitation, recommend `ImmutableList<T>`/`ImmutableDictionary<K,V>`, provide deep-clone utilities, detect mutations in DevTools. Phase to address: Phase 2 (Core Store), Phase 4 (DevTools).

**Additional moderate pitfalls:**
- StateHasChanged Flooding (fix with selectors in Phase 2)
- Heavy Assembly Scanning at Startup (opt-in discovery in Phase 3)
- DI Service Lifetime Mismatch (validation at registration in Phase 1)
- DevTools Production Leakage (separate package in Phase 4)

## Implications for Roadmap

Based on research, suggested phase structure prioritizes architectural correctness over features, avoiding technical debt that would require rewrites. The dependency chain is: Foundation → Core Store → DX Features → DevTools.

### Phase 1: Foundation & Multi-Mode Architecture
**Rationale:** Render mode compatibility must be architected from day one; retrofitting is nearly impossible (Pitfall 1). This phase establishes the mode-agnostic core that everything else builds on.

**Delivers:**
- ZustandStore<TState> abstract base class
- Basic state container with immutable updates
- Mode detection using RendererInfo
- DI integration with correct service lifetime handling
- Initial test matrix for all four render modes (SSR, Server, WASM, Auto)

**Addresses from FEATURES.md:**
- Centralized State Store
- Basic TypeScript-like Type Safety
- DI Integration
- Multi-Hosting Mode Support (architectural foundation)

**Avoids from PITFALLS.md:**
- Pitfall 1: Render Mode Amnesia
- Pitfall 8: DI Service Lifetime Mismatch

**Stack elements used:**
- .NET 10 with multi-targeting (net8.0;net10.0)
- Microsoft.AspNetCore.Components
- bUnit + xUnit for testing

### Phase 2: Core Store with Subscriptions & Selectors
**Rationale:** Subscription mechanism is table stakes for state management. Selectors must be included in core to prevent StateHasChanged flooding (Pitfall 6), which is expensive to retrofit. Proper threading and disposal handling prevent critical pitfalls.

**Delivers:**
- Component subscription with automatic re-render
- Selector-based optimization with equality comparison
- InvokeAsync wrapper for thread-safe notifications
- IDisposable subscription pattern
- Async action support
- Prerendering state persistence hooks
- ZustandScope cascading component

**Addresses from FEATURES.md:**
- Component Subscription/Notification
- Selective Subscriptions (Selectors)
- Async Action Support
- Initial State Configuration
- Immutable State Updates (with documentation)

**Avoids from PITFALLS.md:**
- Pitfall 2: Prerendering State Mismatch
- Pitfall 3: Synchronization Context Violations
- Pitfall 4: Memory Leaks from Undisposed Subscriptions
- Pitfall 5: Shallow Immutability Illusion (documentation)
- Pitfall 6: StateHasChanged Flooding

**Implements from ARCHITECTURE.md:**
- Subscription-Based Component Updates pattern
- Selector-Based Optimization pattern
- Unidirectional Data Flow pattern

**Stack elements used:**
- C# 13 records for immutable state
- System.Collections.Immutable for deep immutability

### Phase 3: DX Features (Middleware, Auto-Discovery, Persistence)
**Rationale:** Middleware is core to extensibility but depends on stable store API. Auto-discovery improves DX but is optional (explicit registration works for MVP). This phase adds convenience without blocking core functionality.

**Delivers:**
- Middleware pipeline (IMiddleware interface, MiddlewarePipeline execution)
- Set() integration with middleware
- Logging middleware
- Persistence middleware (LocalStorage)
- StoreRegistry for tracking stores
- Scrutor integration for auto-discovery
- ServiceCollection extension methods (AddBustand)

**Addresses from FEATURES.md:**
- Built-in Middleware Pipeline
- Persistence Middleware (LocalStorage)
- Auto-Discovery of Stores
- Minimal Boilerplate API (registration convenience)

**Avoids from PITFALLS.md:**
- Pitfall 7: Heavy Assembly Scanning (make auto-discovery opt-in)
- Pitfall 9: Middleware Complexity Explosion (keep built-in middleware minimal)

**Implements from ARCHITECTURE.md:**
- Middleware Pipeline (Interceptor Chain) pattern
- StoreRegistry component

**Stack elements used:**
- Scrutor 7.0.0 for assembly scanning
- System.Text.Json for state serialization

### Phase 4: DevTools Integration & Inspector Page
**Rationale:** DevTools is the key differentiator but depends on middleware pipeline for state capture. Separate package prevents production leakage (Pitfall 10). This is complex (HIGH implementation cost) but HIGH user value.

**Delivers:**
- DevToolsService for state snapshot capture
- IDevToolsTransport abstraction
- JSInteropTransport for WASM mode
- SignalRTransport for Server mode
- DevTools middleware
- DevToolsPage.razor inspector component
- Action logging and history
- State diff visualization
- Redux DevTools browser extension integration
- Bustand.DevTools separate NuGet package

**Addresses from FEATURES.md:**
- Redux DevTools Integration
- DevTools Inspector Page (unique differentiator)
- Time-Travel Debugging (foundation)
- State Diff Visualization
- Action Logging/History

**Avoids from PITFALLS.md:**
- Pitfall 10: DevTools Production Leakage (separate package)
- Pitfall 5: Shallow Immutability Illusion (detect mutations in DevTools)

**Implements from ARCHITECTURE.md:**
- DevToolsService component
- Multi-mode transport abstraction

**Stack elements used:**
- System.Text.Json for state serialization
- SignalR for Server mode transport

### Phase 5: Polish & v1.0 Release
**Rationale:** Final validation, documentation, packaging, and release preparation. Research showed NuGet packaging best practices and Source Link for debugging support.

**Delivers:**
- Complete documentation and API reference
- Sample applications for each render mode
- NuGet package metadata and README
- Source Link configuration
- GitHub Actions CI/CD pipeline
- Performance benchmarking suite
- Migration guide from competitors

**Stack elements used:**
- Microsoft.SourceLink.GitHub for debugging
- MinVer for semantic versioning

### Phase Ordering Rationale

- **Phase 1 before Phase 2:** Mode-agnostic architecture must be foundational; retrofitting multi-mode support after building subscriptions would require complete rewrite
- **Phase 2 before Phase 3:** Middleware depends on stable Set() API; selectors must be in core to prevent performance crisis
- **Phase 3 before Phase 4:** DevTools middleware requires middleware pipeline infrastructure
- **Phase 4 separate from core:** Prevents DevTools from bloating production bundles (Pitfall 10)

This ordering prioritizes architectural correctness and avoiding pitfalls over rapid feature delivery. Each phase delivers working, testable functionality.

### Research Flags

**Phases needing deeper research during planning:**
- **Phase 4 (DevTools):** Redux DevTools protocol implementation needs API research; SignalR vs JS interop transport differences require investigation
- **Phase 2 (Prerendering):** .NET 10 `[PersistentState]` attribute is new; need to validate actual behavior vs documentation

**Phases with standard patterns (skip research-phase):**
- **Phase 1 (Foundation):** DI patterns well-documented, render mode detection is standard Blazor
- **Phase 3 (Middleware):** Middleware pattern is well-established (ASP.NET Core uses same pattern)
- **Phase 5 (Polish):** NuGet packaging and CI/CD are standard .NET practices

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | .NET 10 and Blazor official documentation verified; Scrutor, bUnit versions confirmed on NuGet |
| Features | MEDIUM-HIGH | Competitor analysis based on GitHub repositories and documentation; Zustand patterns well-documented; some features (cross-tab sync) less verified |
| Architecture | MEDIUM-HIGH | Patterns verified against Fluxor, TimeWarp.State implementations and Microsoft guidance; multi-mode considerations based on official docs |
| Pitfalls | MEDIUM-HIGH | Critical pitfalls (render mode, prerendering, sync context) verified with Microsoft documentation; community pitfalls (memory leaks, immutability) based on multiple blog posts |

**Overall confidence:** MEDIUM-HIGH

Research is strong for core architecture and stack decisions. Confidence is slightly reduced for:
1. .NET 10-specific features (recent release, limited real-world evidence)
2. Redux DevTools protocol implementation (need to research during Phase 4)
3. Cross-tab synchronization patterns (deferred to v2+, less critical)

### Gaps to Address

**Gap 1: .NET 10 PersistentState attribute actual behavior**
- Research: Single source (blog post), .NET 10 is new (Nov 2025)
- How to handle: Prototype and validate in Phase 2; fall back to manual PersistentComponentState if attribute insufficient

**Gap 2: Redux DevTools wire protocol specifics**
- Research: General understanding of DevTools, but implementation details sparse
- How to handle: Run `/gsd:research-phase` for Phase 4 to investigate protocol, message format, Chrome extension communication

**Gap 3: SignalR vs JS interop performance for DevTools**
- Research: Assume SignalR required for Server mode, but actual performance characteristics unknown
- How to handle: Benchmark during Phase 4 implementation; may need to optimize batching/throttling

**Gap 4: Shallow immutability mitigation strategies**
- Research: Problem well-documented, solutions (ImmutableCollections) known, but user adoption patterns unclear
- How to handle: Provide both documentation and optional deep-clone helpers; DevTools mutation detection validates compliance

**Gap 5: Assembly scanning performance impact in large apps**
- Research: General knowledge that reflection is slow, but specific Scrutor overhead unknown
- How to handle: Benchmark during Phase 3; make auto-discovery opt-in rather than default

## Sources

### Primary (HIGH confidence)
- [Microsoft Blazor Render Modes (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)
- [Microsoft Blazor State Management](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/?view=aspnetcore-10.0)
- [Microsoft Blazor Component Lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0)
- [Microsoft Blazor Component Disposal](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0)
- [Microsoft Blazor Synchronization Context](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0)
- [Microsoft C# Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [NuGet: Scrutor 7.0.0](https://www.nuget.org/packages/Scrutor)
- [NuGet: bUnit 2.5.3](https://www.nuget.org/packages/bunit)
- [Scrutor GitHub](https://github.com/khellang/Scrutor)

### Secondary (MEDIUM confidence)
- [Fluxor GitHub Repository](https://github.com/mrpmorris/Fluxor)
- [TimeWarp.State GitHub Repository](https://github.com/TimeWarpEngineering/timewarp-state)
- [EasyAppDev.Blazor.Store GitHub Repository](https://github.com/mashrulhaque/EasyAppDev.Blazor.Store)
- [Zustand Documentation](https://zustand.docs.pmnd.rs/)
- [Zustand GitHub Repository](https://github.com/pmndrs/zustand)
- [Code Maze: Fluxor for State Management](https://code-maze.com/fluxor-for-state-management-in-blazor/)
- [Zustand Selector Patterns](https://tkdodo.eu/blog/working-with-zustand)
- [Blazor Rehydration Patterns](https://erwinkn.com/tech/blazor-rehydration/)
- [Blazor Server Memory Management](https://amarozka.dev/blazor-server-memory-management-circuit-leaks/)
- [Blazor DI Scopes](https://www.thinktecture.com/en/blazor/dependency-injection-scopes-in-blazor/)

### Tertiary (LOW confidence)
- [.NET 10 Persistent State Feature](https://www.telerik.com/blogs/net-10-preview-release-6-tackles-blazor-server-lost-state-problem) — needs validation during Phase 2
- Various Medium articles on state management patterns — referenced for general patterns, not specific implementation details

---
*Research completed: 2026-01-24*
*Ready for roadmap: yes*
