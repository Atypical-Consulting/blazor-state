# Phase 1: Foundation - Context

**Gathered:** 2026-01-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Establish mode-agnostic architecture that works across all Blazor rendering modes (Server, WebAssembly, Static SSR, Auto). This phase delivers the project scaffolding, DI integration, and foundational patterns that all subsequent phases build upon. Features like subscriptions, middleware, and persistence are separate phases.

</domain>

<decisions>
## Implementation Decisions

### DI registration API
- Single method: `AddBustand()` with auto-discovery of all stores
- Fluent configuration: `services.AddBustand(opt => opt...)` for optional customization
- Available config options:
  - Assembly scanning control (which assemblies to scan for stores)
  - Default store lifetime override
  - Middleware registration (global middleware)
- Discovery mechanism: `[BustandStore]` attribute for explicit opt-in
  - Only classes with `[BustandStore]` attribute are auto-registered
  - Inheriting from `ZustandStore<T>` alone is not enough

### Store lifetime behavior
- **Default lifetime is mode-aware:**
  - Singleton in WebAssembly (global state makes sense)
  - Scoped in Server mode (isolated per circuit/user for safety)
- **Per-store override:** `[BustandStore(Lifetime.Singleton)]` attribute on store class
  - Attribute declares lifetime explicitly
  - Overrides mode-aware default
- **Circuit reconnect in Server mode:**
  - State restores from persistence if enabled (handled in Phase 4)
  - Not fresh state on reconnect
- **Safety warning:** Log console warning if singleton store detected in Server mode
  - Alerts developers to potential data leak across users
  - Logged at startup

### Mode detection & adaptation
- **Detection approach:** Hybrid global default + per-component override
  - .NET 10 allows mixed rendering modes per component
  - Global mode detected at startup as default assumption
  - Components can hint their mode when needed (per-component override)
- **What adapts based on mode:** Claude's discretion
  - Identify what actually needs mode awareness in Phase 1 (e.g., StateHasChanged invocation)
  - StateHasChanged likely needs InvokeAsync wrapper in Server, not WASM
- **Timing:** Claude's discretion
  - Choose between startup detection, lazy initialization, or dynamic per-operation
  - Balance performance vs flexibility for mixed-mode scenarios

### Project structure & multi-targeting
- **Package split:** Two packages
  - `Bustand` (core library)
  - `Bustand.DevTools` (separate DevTools package to avoid production bloat)
- **Target framework:** `net10.0` only
  - Latest .NET, forward-looking
  - Does not target .NET 8
- **Root namespace:** `Bustand`
  - Simple, matches package name
  - Usage: `using Bustand;`
- **Solution organization:** Categorized folders
  - `src/` — source projects (Bustand, Bustand.DevTools)
  - `tests/` — test projects
  - `samples/` — sample applications

### Claude's Discretion
- Mode detection timing (startup vs lazy vs dynamic)
- Specific behaviors that need mode adaptation in Phase 1
- Internal architecture patterns and abstractions
- Error handling and validation details

</decisions>

<specifics>
## Specific Ideas

- DI registration should feel minimal — developer adds one line and it "just works"
- Mode-aware lifetime defaults prevent the common mistake of singleton in Server mode
- Per-component mode override is essential for .NET 10's mixed-mode capability
- DevTools as separate package follows best practice (like EF Core's design-time tools)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-01-24*
