# Phase 4: Persistence - Context

**Gathered:** 2026-01-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Store state persists across page reloads and circuit reconnects via LocalStorage/SessionStorage persistence middleware. Covers WASM page refreshes and Server circuit reconnections.

</domain>

<decisions>
## Implementation Decisions

### Storage Strategy
- Opt-in persistence via `[Persist(StorageType.Local)]` or `[Persist(StorageType.Session)]` attribute on store class
- No persistence by default - stores must explicitly declare persistence
- Storage key auto-generated from store type name (e.g., "Bustand.CounterStore")
- Custom storage key via optional attribute parameter: `[Persist(StorageType.Local, Key = "my-counter")]`
- Global prefix configured in AddBustand (e.g., `AddBustand(prefix: "MyApp")`) - all keys get "MyApp." prefix
- Prefix prevents collisions when multiple apps share same origin

### Serialization Approach
- System.Text.Json (built-in) - no additional dependencies
- JsonSerializerOptions configurable globally via AddBustand options
- Non-serializable state members (functions, events, delegates) cause serialization to fail with clear error message
- Developer must ensure state types are fully serializable

### Restore Timing & Behavior
- State restored during store construction (DI resolution time)
- Store is ready with persisted state immediately when injected
- Corrupted/invalid stored data falls back to InitialState with logged warning (graceful degradation)
- Restored state merges with InitialState - stored values win, InitialState fills gaps for new fields
- State changes persist with debouncing - write to storage after quiet period (e.g., 500ms of no changes)
- Debouncing reduces storage I/O while maintaining data safety

### Claude's Discretion
- Exact debounce timing (balance between performance and data safety)
- Schema versioning strategy when stored state doesn't match current type
- Server mode storage location (client-side via JS interop vs ProtectedSessionStorage)
- Circuit reconnection handling in Server mode
- Persistence behavior for Scoped vs Singleton stores in Server mode
- Auto mode (Server -> WASM) persistence migration strategy

</decisions>

<specifics>
## Specific Ideas

No specific requirements - open to standard approaches for areas under Claude's discretion.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 04-persistence*
*Context gathered: 2026-01-24*
