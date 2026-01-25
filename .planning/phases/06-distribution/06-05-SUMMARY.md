---
phase: 06-distribution
plan: 05
subsystem: samples
tags: [counter, render-modes, server, wasm, auto, blazor]
requires:
  - "06-04 (Sample Stores)"
provides:
  - "Counter pages for all Blazor render modes"
  - "Tutorial comments explaining render mode differences"
  - "Mode indicator for Auto render mode"
affects:
  - "06-06 (Sample Pages)"
tech-stack:
  added: []
  patterns:
    - "UseState subscription pattern"
    - "ZustandComponent inheritance"
    - "OperatingSystem.IsBrowser detection"
key-files:
  created:
    - samples/Bustand.Sample/Components/Pages/CounterServer.razor
    - samples/Bustand.Sample.Client/Pages/CounterWasm.razor
    - samples/Bustand.Sample.Client/Pages/CounterAuto.razor
  modified:
    - samples/Bustand.Sample/Components/_Imports.razor
decisions:
  - id: "06-05-01"
    choice: "UseState pattern with sliced state"
    rationale: "Follows ZustandComponent API - components use UseState() for subscriptions"
  - id: "06-05-02"
    choice: "RenderMode static using in _Imports.razor"
    rationale: "Enables clean @rendermode InteractiveServer syntax without namespace prefix"
metrics:
  duration: "4.6 min"
  completed: "2026-01-25"
---

# Phase 06 Plan 05: Counter Pages Summary

**One-liner:** Three Counter pages demonstrating Server, WASM, and Auto render modes with tutorial comments explaining each mode's characteristics.

## What Was Built

### 1. CounterServer.razor (Server project)
- Located in `samples/Bustand.Sample/Components/Pages/`
- Uses `@rendermode InteractiveServer`
- Demonstrates SignalR-based UI interactions
- Explains server-side state processing and persistence via JS interop

### 2. CounterWasm.razor (Client project)
- Located in `samples/Bustand.Sample.Client/Pages/`
- Uses `@rendermode InteractiveWebAssembly`
- Demonstrates pure browser-side execution
- Explains offline capability and local processing

### 3. CounterAuto.razor (Client project)
- Located in `samples/Bustand.Sample.Client/Pages/`
- Uses `@rendermode InteractiveAuto`
- Includes `OperatingSystem.IsBrowser()` mode indicator
- Explains Server-to-WASM transition and state persistence

## Key Patterns Demonstrated

### UseState Subscription Pattern
```csharp
@code {
    private UseStateResult<int> count;

    protected override void OnInitialized()
    {
        count = UseState(state => state.Count);
    }
}
```

### Mode Detection
```razor
@if (OperatingSystem.IsBrowser())
{
    <span class="badge bg-success">Running in WebAssembly</span>
}
else
{
    <span class="badge bg-primary">Running on Server</span>
}
```

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added RenderMode static using**
- **Found during:** Task 1
- **Issue:** `@rendermode InteractiveServer` failed to resolve
- **Fix:** Added `@using static Microsoft.AspNetCore.Components.Web.RenderMode` to _Imports.razor files
- **Files modified:** samples/Bustand.Sample/Components/_Imports.razor
- **Commit:** 28ae95e

**2. [Rule 3 - Blocking] Added Bustand.Components using**
- **Found during:** Task 1
- **Issue:** `ZustandComponent<,>` not found
- **Fix:** Added `@using Bustand.Components` to Server _Imports.razor
- **Files modified:** samples/Bustand.Sample/Components/_Imports.razor
- **Commit:** 28ae95e

## Architecture Notes

### Project Placement
- **Server pages** (CounterServer) can live in either project but placed in Server for clarity
- **WASM/Auto pages** MUST be in Client project (compiled to WebAssembly)
- **Stores** live in Client project (accessible from all render modes)

### Why Same Store Works Everywhere
1. Bustand stores are render-mode agnostic
2. Server project references Client project
3. Persistence uses browser LocalStorage (works in all modes)
4. UseState pattern is identical across all render modes

## Verification Checklist

- [x] CounterServer.razor exists with InteractiveServer
- [x] CounterWasm.razor exists with InteractiveWebAssembly
- [x] CounterAuto.razor exists with InteractiveAuto
- [x] CounterAuto.razor has OperatingSystem.IsBrowser() check
- [x] All pages use ZustandComponent<CounterStore, CounterState>
- [x] All pages reference CounterStore from Client project
- [x] dotnet build Bustand.sln succeeds

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 28ae95e | feat | CounterServer page with InteractiveServer |
| 8899c0e | feat | CounterWasm page with InteractiveWebAssembly |
| 2aa415d | feat | CounterAuto page with InteractiveAuto |

## Next Phase Readiness

Plan 06-05 complete. All three Blazor render modes are now demonstrated with:
- Identical Bustand patterns across all modes
- Tutorial-quality comments for developers
- Mode indicator showing actual execution context

Ready for: 06-06 (Sample Pages with additional patterns)
