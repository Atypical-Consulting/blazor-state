# Bustand

## What This Is

A Zustand-inspired state management library for Blazor .NET 10 that brings Zustand's simplicity and exceptional developer experience to the .NET ecosystem. Developers create stores by inheriting from `ZustandStore<TState>`, use immutable state updates via records, and get a Redux DevTools-like experience built in.

## Core Value

Minimal boilerplate state management with exceptional debugging experience - if developers can't easily see and understand state changes, the library fails.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Core store functionality (ZustandStore base class, Set() method, immutable state updates)
- [ ] Component integration (ZustandScope cascading component, CascadingParameter pattern)
- [ ] Auto-discovery with Scrutor (scan and register stores automatically)
- [ ] Middleware architecture (interceptor pattern for state changes)
- [ ] DevTools middleware (capture state changes and send to DevTools page)
- [ ] DevTools page with state inspector (view current state of all stores in real-time)
- [ ] DevTools page with action log (history of state mutations)
- [ ] DevTools page with time-travel debugging (rewind/replay state changes)
- [ ] DevTools page with diff view (see what changed between states)
- [ ] DevTools registration (AddBustandDevTools() for dev environment only)
- [ ] Persistence middleware (save/restore state to localStorage/sessionStorage)
- [ ] Logging middleware (log state changes to console/telemetry)
- [ ] NuGet package (published and installable)
- [ ] Sample Blazor app (demonstrates all core features)
- [ ] README documentation (getting started, API reference, examples)

### Out of Scope

- Other UI frameworks (WPF, WinForms, MAUI) — Blazor-only for v1, can expand later
- Complex built-in middleware — provide architecture and basic examples, community can extend
- Production DevTools — DevTools are dev-only, not for production use
- TypeScript definitions — .NET library, no JS interop needed
- Async state initialization — defer to v2, keep v1 simple

## Context

**Inspiration:**
- Zustand (React state management) - loved for minimal API surface and no boilerplate
- Redux DevTools - exceptional debugging experience that Blazor state management lacks

**Technical environment:**
- .NET 10 Blazor with unified rendering model
- Support for Server, WebAssembly, and Static SSR modes
- C# records for immutable state (with expressions)
- Scrutor for assembly scanning and auto-registration

**Developer experience goals:**
- Store definition in ~10 lines of code
- Zero boilerplate (no actions, reducers, dispatchers)
- DevTools accessible at `/bustand-devtools` when enabled
- State changes automatically trigger component re-renders

## Constraints

- **Tech stack**: .NET 10, Blazor (Server + WASM + SSR support required) — must work across all rendering modes
- **DevTools UI**: Plain HTML/CSS — no UI framework dependencies to avoid forcing choices on consumers
- **DevTools availability**: Dev environment only — registered via `if (env.IsDevelopment()) builder.Services.AddBustandDevTools()`
- **State immutability**: C# records with `with` expressions — enforced by API design, not runtime validation
- **Performance**: State updates must not block UI thread — consider batching for high-frequency updates

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| CascadingParameter pattern over DI injection | Allows multiple instances of same store in component tree, better scoping | — Pending |
| Auto-discovery with Scrutor | Reduces registration ceremony, follows library philosophy | — Pending |
| DevTools as separate page (not browser extension) | Simpler implementation, works across all browsers, no extension install needed | — Pending |
| Plain HTML/CSS for DevTools | Avoids UI framework lock-in, keeps library dependencies minimal | — Pending |
| Middleware intercepts Set() calls | Clean extension point for logging, persistence, DevTools without polluting store API | — Pending |

---
*Last updated: 2026-01-24 after initialization*
