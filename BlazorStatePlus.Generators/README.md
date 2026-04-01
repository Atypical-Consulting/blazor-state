# BlazorStatePlus.Generators

Roslyn incremental source generator that eliminates boilerplate for Blazor persistent state management.

> **Scope:** This generator supports `PersistentComponentState` — the prerender-to-interactive handoff mechanism. State managed by this generator does **not** survive page refreshes, navigation, or browser sessions.

## What it does

Finds `[Slice]`-annotated `IStateSlice<T>` fields on partial `ComponentBase` classes and generates the other partial half with:

- `StateManager` injection
- `OnInitialized` override that creates slices with auto-derived keys
- `OnInitializedAsync` override that runs async initialization factories
- `SliceInitContext` nested class for runtime configuration (dynamic keys, async init)
- `IDisposable` implementation that cleans up all slices

## User-facing API

```csharp
public partial class ProductDetail : ComponentBase
{
    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<ProductPageState> _page;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Page
           .KeySuffix(ProductId)
           .InitializeFrom(async () => new ProductPageState { ... });
    }
}
```

No base class, no string keys, no manual wiring.

## Architecture

- **Target:** `netstandard2.0` (Roslyn requirement)
- **Ships inside** the main `BlazorStatePlus` NuGet package via `OutputItemType="Analyzer"`
- **Pipeline:** `ForAttributeWithMetadataName` → validate → build `ComponentModel` → `Emitter.Emit()`

## Diagnostics

| ID | Severity | Description |
|---|---|---|
| BSP001 | Error | `[Slice]` on non-partial class |
| BSP002 | Error | Field type is not `IStateSlice<T>` |
| BSP003 | Error | Class doesn't inherit `ComponentBase` |
| BSP005 | Error | Invalid `TimeToLive` format |
| BSP006 | Warning | Class already implements `IDisposable` |
| BSP007 | Error | Duplicate slice keys |
| BSP008 | Error | `[Slice]` on static field |
| BSP011 | Warning | Class overrides `OnInitialized` |
| BSP012 | Warning | Field has initializer (will be overwritten) |
