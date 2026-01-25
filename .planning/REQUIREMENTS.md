# Requirements: Bustand

**Defined:** 2026-01-24
**Core Value:** Minimal boilerplate state management with exceptional debugging experience

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Core Store

- [ ] **CORE-01**: Developer can create store by inheriting from ZustandStore<TState>
- [ ] **CORE-02**: Developer can define state as C# record
- [ ] **CORE-03**: Developer can update state via Set() method with immutable with expression
- [ ] **CORE-04**: Developer can update state asynchronously
- [ ] **CORE-05**: Developer can define initial state in constructor
- [ ] **CORE-06**: Developer can define derived/computed state as properties
- [ ] **CORE-07**: Store notifies subscribers when state changes
- [ ] **CORE-08**: State updates are type-safe (compile-time checked)

### Component Integration

- [ ] **COMP-01**: Component can subscribe to store state changes
- [ ] **COMP-02**: Component re-renders automatically when subscribed state changes
- [ ] **COMP-03**: Store can be injected via dependency injection
- [ ] **COMP-04**: Component can use ZustandScope for scoped store instances
- [ ] **COMP-05**: Component can access store via CascadingParameter
- [ ] **COMP-06**: Component can subscribe to specific state slice (selector)
- [ ] **COMP-07**: Component only re-renders when selected state slice changes
- [ ] **COMP-08**: Component subscriptions dispose properly (no memory leaks)

### Multi-Mode Support

- [ ] **MODE-01**: Store works in Blazor Server mode
- [ ] **MODE-02**: Store works in Blazor WebAssembly mode
- [ ] **MODE-03**: Store works in Static SSR mode (with limitations)
- [ ] **MODE-04**: Store works in Interactive Auto mode (Server -> WASM transition)
- [ ] **MODE-05**: State updates respect Blazor synchronization context (no threading errors)
- [ ] **MODE-06**: Store handles prerendering without state mismatch
- [ ] **MODE-07**: Library components never specify @rendermode (mode-agnostic)

### Middleware

- [ ] **MIDL-01**: Developer can create custom middleware by implementing interface
- [ ] **MIDL-02**: Middleware intercepts Set() calls before state update
- [ ] **MIDL-03**: Middleware can access old state, new state, and store type
- [ ] **MIDL-04**: Multiple middleware can be chained in pipeline
- [ ] **MIDL-05**: Middleware execution order is configurable
- [ ] **MIDL-06**: Logging middleware logs state changes to console
- [ ] **MIDL-07**: Persistence middleware saves state to LocalStorage
- [ ] **MIDL-08**: Persistence middleware restores state on initialization
- [x] **MIDL-09**: DevTools middleware captures state changes for DevTools page

### Auto-Discovery

- [ ] **DISC-01**: Developer can enable auto-discovery via AddBustand()
- [ ] **DISC-02**: Scrutor scans assemblies for ZustandStore<T> descendants
- [ ] **DISC-03**: Discovered stores register automatically in DI container
- [ ] **DISC-04**: Developer can configure service lifetime (scoped/singleton)
- [ ] **DISC-05**: Auto-discovery works without manual registration code

### Persistence

- [ ] **PERS-01**: Store state can persist to LocalStorage
- [ ] **PERS-02**: Store state can persist to SessionStorage
- [ ] **PERS-03**: Persisted state restores on page reload (WASM)
- [ ] **PERS-04**: Persisted state restores on circuit reconnect (Server)
- [ ] **PERS-05**: Developer can configure which stores persist
- [ ] **PERS-06**: Developer can configure storage key prefix

### DevTools

- [x] **DEVO-01**: Developer can enable DevTools via AddBustandDevTools() in dev environment
- [x] **DEVO-02**: DevTools page accessible at /bustand-devtools route
- [x] **DEVO-03**: DevTools page shows list of all registered stores
- [x] **DEVO-04**: DevTools page shows current state of selected store (state inspector)
- [x] **DEVO-05**: DevTools page shows history of state changes (action log)
- [x] **DEVO-06**: DevTools page shows timestamp for each state change
- [x] **DEVO-07**: DevTools page supports time-travel (rewind to previous state)
- [x] **DEVO-08**: DevTools page supports replay (forward through state history)
- [x] **DEVO-09**: DevTools page shows diff between consecutive states
- [x] **DEVO-10**: DevTools page highlights what changed in state (diff view)
- [x] **DEVO-11**: DevTools page updates in real-time when state changes
- [x] **DEVO-12**: DevTools UI built with plain HTML/CSS (no framework dependencies)
- [x] **DEVO-13**: DevTools packaged as separate NuGet (Bustand.DevTools)
- [x] **DEVO-14**: DevTools registration fails/warns in production environment

### Distribution

- [ ] **DIST-01**: Bustand core packaged as NuGet package
- [ ] **DIST-02**: Bustand.DevTools packaged as separate NuGet package
- [ ] **DIST-03**: NuGet packages target .NET 8 and .NET 10
- [ ] **DIST-04**: Sample Blazor app demonstrates core features
- [ ] **DIST-05**: Sample app shows all rendering modes
- [ ] **DIST-06**: Sample app demonstrates DevTools usage
- [ ] **DIST-07**: README includes getting started guide
- [ ] **DIST-08**: README includes API reference
- [ ] **DIST-09**: README includes code examples

### Testing

- [ ] **TEST-01**: Core store functionality has unit tests (xUnit)
- [ ] **TEST-02**: Component integration has bUnit tests
- [ ] **TEST-03**: Middleware pipeline has unit tests
- [ ] **TEST-04**: Multi-mode scenarios have integration tests
- [ ] **TEST-05**: Test coverage >= 80% for core library

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Advanced DX

- **DX-01**: Undo/redo functionality
- **DX-02**: Cross-tab state synchronization
- **DX-03**: Source generator for store discovery (alternative to Scrutor)

### Advanced DevTools

- **DEVO-ADV-01**: Redux DevTools browser extension integration
- **DEVO-ADV-02**: Network-based DevTools (remote debugging)
- **DEVO-ADV-03**: Performance profiling (re-render counts, update frequency)

### Enterprise Features

- **ENT-01**: Middleware for validation
- **ENT-02**: Optimistic updates with rollback
- **ENT-03**: State migration utilities (version upgrades)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| WPF/WinForms/MAUI support | Blazor-only for v1, can expand later to other .NET UI frameworks |
| Bidirectional data binding | Against Zustand philosophy; use one-way flow |
| Action/Reducer classes | Redux boilerplate we're explicitly avoiding |
| Global singleton-only stores | CascadingParameter pattern allows multiple instances |
| Automatic INotifyPropertyChanged | Not needed; immutable state via records |
| Built-in form validation | Orthogonal concern; use FluentValidation separately |
| TypeScript definitions | .NET library, no JS interop needed |
| Production DevTools | DevTools are dev-only for security and performance |
| Real-time collaboration | Too complex for v1; defer to v2+ or community |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CORE-01 | Phase 2 | Complete |
| CORE-02 | Phase 2 | Complete |
| CORE-03 | Phase 2 | Complete |
| CORE-04 | Phase 2 | Complete |
| CORE-05 | Phase 2 | Complete |
| CORE-06 | Phase 2 | Complete |
| CORE-07 | Phase 2 | Complete |
| CORE-08 | Phase 2 | Complete |
| COMP-01 | Phase 2 | Complete |
| COMP-02 | Phase 2 | Complete |
| COMP-03 | Phase 2 | Complete |
| COMP-04 | Phase 2 | Complete |
| COMP-05 | Phase 2 | Complete |
| COMP-06 | Phase 2 | Complete |
| COMP-07 | Phase 2 | Complete |
| COMP-08 | Phase 2 | Complete |
| MODE-01 | Phase 1 | Complete |
| MODE-02 | Phase 1 | Complete |
| MODE-03 | Phase 1 | Complete |
| MODE-04 | Phase 1 | Complete |
| MODE-05 | Phase 1 | Complete |
| MODE-06 | Phase 1 | Complete |
| MODE-07 | Phase 2 | Complete |
| MIDL-01 | Phase 3 | Pending |
| MIDL-02 | Phase 3 | Pending |
| MIDL-03 | Phase 3 | Pending |
| MIDL-04 | Phase 3 | Pending |
| MIDL-05 | Phase 3 | Pending |
| MIDL-06 | Phase 3 | Pending |
| MIDL-07 | Complete | 4 |
| MIDL-08 | Complete | 4 |
| MIDL-09 | Phase 5 | Complete |
| DISC-01 | Phase 3 | Pending |
| DISC-02 | Phase 3 | Pending |
| DISC-03 | Phase 3 | Pending |
| DISC-04 | Phase 3 | Pending |
| DISC-05 | Phase 3 | Pending |
| PERS-01 | Complete | 4 |
| PERS-02 | Complete | 4 |
| PERS-03 | Complete | 4 |
| PERS-04 | Complete | 4 |
| PERS-05 | Complete | 4 |
| PERS-06 | Complete | 4 |
| DEVO-01 | Phase 5 | Complete |
| DEVO-02 | Phase 5 | Complete |
| DEVO-03 | Phase 5 | Complete |
| DEVO-04 | Phase 5 | Complete |
| DEVO-05 | Phase 5 | Complete |
| DEVO-06 | Phase 5 | Complete |
| DEVO-07 | Phase 5 | Complete |
| DEVO-08 | Phase 5 | Complete |
| DEVO-09 | Phase 5 | Complete |
| DEVO-10 | Phase 5 | Complete |
| DEVO-11 | Phase 5 | Complete |
| DEVO-12 | Phase 5 | Complete |
| DEVO-13 | Phase 5 | Complete |
| DEVO-14 | Phase 5 | Complete |
| DIST-01 | Phase 6 | Pending |
| DIST-02 | Phase 6 | Pending |
| DIST-03 | Phase 6 | Pending |
| DIST-04 | Phase 6 | Pending |
| DIST-05 | Phase 6 | Pending |
| DIST-06 | Phase 6 | Pending |
| DIST-07 | Phase 6 | Pending |
| DIST-08 | Phase 6 | Pending |
| DIST-09 | Phase 6 | Pending |
| TEST-01 | Phase 2 | Complete |
| TEST-02 | Phase 2 | Complete |
| TEST-03 | Phase 3 | Pending |
| TEST-04 | Phase 6 | Pending |
| TEST-05 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 71 total
- Mapped to phases: 71
- Unmapped: 0

---
*Requirements defined: 2026-01-24*
*Last updated: 2026-01-24 after roadmap creation*
