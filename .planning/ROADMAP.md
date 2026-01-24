# Roadmap: Bustand

## Overview

Bustand delivers Zustand-inspired state management for Blazor in six phases, starting with multi-mode architecture (the hardest part to retrofit), then building core store functionality with subscriptions and selectors, adding middleware for extensibility, implementing persistence, delivering the differentiating DevTools experience, and finally packaging for distribution. Each phase delivers working, testable functionality that builds on the previous.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation** - Project scaffolding, multi-mode architecture, DI integration
- [x] **Phase 2: Core Store** - ZustandStore base class, subscriptions, selectors, component integration
- [x] **Phase 3: Middleware & DX** - Middleware pipeline, auto-discovery, logging middleware
- [x] **Phase 4: Persistence** - Persistence middleware for LocalStorage/SessionStorage
- [ ] **Phase 5: DevTools** - DevTools page with state inspector, action log, time-travel, diff view
- [ ] **Phase 6: Distribution** - NuGet packaging, sample app, documentation

## Phase Details

### Phase 1: Foundation
**Goal**: Establish mode-agnostic architecture that works across all Blazor rendering modes from day one
**Depends on**: Nothing (first phase)
**Requirements**: MODE-01, MODE-02, MODE-03, MODE-04, MODE-05, MODE-06
**Success Criteria** (what must be TRUE):
  1. Project compiles and targets .NET 10
  2. Store can be registered in DI container with configurable lifetime
  3. Store instance can be resolved in Blazor Server mode without errors
  4. Store instance can be resolved in Blazor WebAssembly mode without errors
  5. Store instance can be resolved in Static SSR mode without errors
**Plans**: 3 plans

Plans:
- [x] 01-01-PLAN.md - Solution scaffolding and core ZustandStore base class
- [x] 01-02-PLAN.md - DI registration with attribute-based discovery and mode-aware lifetimes
- [x] 01-03-PLAN.md - Test project, core tests, and DevTools shell

### Phase 2: Core Store
**Goal**: Developers can create stores, update state immutably, and components automatically re-render on changes
**Depends on**: Phase 1
**Requirements**: CORE-01, CORE-02, CORE-03, CORE-04, CORE-05, CORE-06, CORE-07, CORE-08, COMP-01, COMP-02, COMP-03, COMP-04, COMP-05, COMP-06, COMP-07, COMP-08, MODE-07, TEST-01, TEST-02
**Success Criteria** (what must be TRUE):
  1. Developer can create a store by inheriting from ZustandStore<TState> with record state in under 10 lines
  2. Developer can update state via Set() method and component re-renders automatically
  3. Developer can subscribe to specific state slice and component only re-renders when that slice changes
  4. Component subscriptions dispose properly when component is disposed (no memory leaks)
  5. State updates in background thread do not crash in Blazor Server mode (InvokeAsync works)
  6. Library components do not specify @rendermode (mode-agnostic design)
**Plans**: 4 plans

Plans:
- [x] 02-01-PLAN.md - Enhanced store API (Set overloads, SetAsync, InitialState, render loop detection)
- [x] 02-02-PLAN.md - Subscription system with selector-based change detection
- [x] 02-03-PLAN.md - Component integration (ZustandComponent, ZustandScope, UseState)
- [x] 02-04-PLAN.md - Comprehensive test suite for Phase 2 requirements

### Phase 3: Middleware & DX
**Goal**: Developers can extend store behavior via middleware pipeline and auto-discover stores without registration boilerplate
**Depends on**: Phase 2
**Requirements**: MIDL-01, MIDL-02, MIDL-03, MIDL-04, MIDL-05, MIDL-06, DISC-01, DISC-02, DISC-03, DISC-04, DISC-05, TEST-03
**Success Criteria** (what must be TRUE):
  1. Developer can create custom middleware that intercepts Set() calls
  2. Multiple middleware can be chained and execute in configured order
  3. Logging middleware logs state changes to console with old/new state
  4. Developer can call AddBustand() and all stores in assembly are registered automatically
  5. Auto-discovery works without any manual store registration code
**Plans**: 4 plans

Plans:
- [x] 03-01-PLAN.md - Middleware infrastructure (IMiddleware, MiddlewareContext, MiddlewarePipeline)
- [x] 03-02-PLAN.md - Store integration and registration API (ZustandStore hooks, BustandOptions.UseMiddleware)
- [x] 03-03-PLAN.md - Logging middleware with CompareNETObjects diffing
- [x] 03-04-PLAN.md - Comprehensive test suite for middleware pipeline (TEST-03)

### Phase 4: Persistence
**Goal**: Store state persists across page reloads and circuit reconnects
**Depends on**: Phase 3
**Requirements**: MIDL-07, MIDL-08, PERS-01, PERS-02, PERS-03, PERS-04, PERS-05, PERS-06
**Success Criteria** (what must be TRUE):
  1. Developer can enable persistence for a store via configuration
  2. Store state persists to LocalStorage and survives page reload (WASM)
  3. Store state persists to SessionStorage and survives page reload (WASM)
  4. Store state restores on Blazor Server circuit reconnect
  5. Developer can configure storage key prefix for namespacing
**Plans**: 5 plans

Plans:
- [x] 04-01-PLAN.md - Storage abstraction (PersistAttribute, StorageType, IBrowserStorage, BrowserStorageService)
- [x] 04-02-PLAN.md - PersistenceMiddleware with debounced writes (DebouncedWriter)
- [x] 04-03-PLAN.md - DI integration and state restoration (ServiceCollectionExtensions, ZustandStore hooks, BustandInitializer)
- [x] 04-05-PLAN.md - Circuit reconnect handling (BustandCircuitHandler for Blazor Server)
- [x] 04-04-PLAN.md - Comprehensive test suite for persistence functionality

### Phase 5: DevTools
**Goal**: Developers can inspect, debug, and time-travel through state changes via built-in DevTools page
**Depends on**: Phase 4
**Requirements**: MIDL-09, DEVO-01, DEVO-02, DEVO-03, DEVO-04, DEVO-05, DEVO-06, DEVO-07, DEVO-08, DEVO-09, DEVO-10, DEVO-11, DEVO-12, DEVO-13, DEVO-14
**Success Criteria** (what must be TRUE):
  1. Developer can navigate to /bustand-devtools and see list of all registered stores
  2. DevTools page shows current state of selected store in real-time (updates as state changes)
  3. DevTools page shows history of state changes with timestamps
  4. Developer can click a previous state to rewind (time-travel) and see app update
  5. DevTools page shows diff between consecutive states highlighting what changed
**Plans**: 8 plans

Plans:
- [ ] 05-01-PLAN.md - DevToolsStore core, StateSnapshot model, IDevToolsStore interface
- [ ] 05-02-PLAN.md - DevToolsMiddleware and DI integration with environment protection
- [ ] 05-03-PLAN.md - DevTools page layout, sidebar, tab bar, dark theme CSS
- [ ] 05-04-PLAN.md - StateTreeView recursive component and export features
- [ ] 05-05-PLAN.md - ActionHistoryPanel with time-travel implementation
- [ ] 05-06-PLAN.md - DiffViewerPanel with side-by-side comparison
- [ ] 05-07-PLAN.md - Complete middleware wiring and store registration
- [ ] 05-08-PLAN.md - Tests and human verification of complete DevTools

### Phase 6: Distribution
**Goal**: Bustand is installable from NuGet with documentation and working sample app
**Depends on**: Phase 5
**Requirements**: DIST-01, DIST-02, DIST-03, DIST-04, DIST-05, DIST-06, DIST-07, DIST-08, DIST-09, TEST-04, TEST-05
**Success Criteria** (what must be TRUE):
  1. Developer can install Bustand and Bustand.DevTools from NuGet
  2. Sample Blazor app runs and demonstrates stores in Server, WASM, and Auto modes
  3. Sample app demonstrates DevTools usage at /bustand-devtools
  4. README includes getting started guide that works from zero to running in under 5 minutes
  5. Test coverage for core library is >= 80%
**Plans**: TBD

Plans:
- [ ] 06-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 3/3 | Complete | 2026-01-24 |
| 2. Core Store | 4/4 | Complete | 2026-01-24 |
| 3. Middleware & DX | 4/4 | Complete | 2026-01-24 |
| 4. Persistence | 5/5 | Complete | 2026-01-24 |
| 5. DevTools | 0/8 | Planned | - |
| 6. Distribution | 0/TBD | Not started | - |
