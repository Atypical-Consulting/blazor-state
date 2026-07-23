# TheBlazorState.Generators

Roslyn incremental source generator for the [TheBlazorState](https://www.nuget.org/packages/TheBlazorState) library.

## Installation

This package is included automatically when you reference the main `TheBlazorState` NuGet package. You do not need to install it separately.

## What It Generates

The generator finds `[Persist]` and `[Shared]` attributes on `partial` properties and emits the other half of each partial class:

**For `[Persist]` properties on components:**
- Backing field and property implementation with persistence hooks
- `{Name}Meta` companion property (WasRestored, IsDirty, IsStale, LastUpdated, OnChanged)
- `OnInitialized` / `OnInitializedAsync` / `Dispose` lifecycle wiring
- Integration with `[Parameter]` when both attributes are present
- `ConfigureState(StateContext)` partial method discovery

**For `[Shared]` properties on state classes:**
- Backing field and property implementation with change notification
- `{Name}Meta` companion property
- `INotifyStateChanged` interface implementation for component subscriptions
- Combined persistence and notification logic when `[Shared, Persist]` are used together

## Architecture

- **Target framework:** `netstandard2.0` (Roslyn analyzer requirement)
- **Pipeline:** `ForAttributeWithMetadataName` -> validate -> build model -> emit

## Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| `TBS001` | Error | `[Persist]` or `[Shared]` on non-partial property |
| `TBS002` | Error | `[Persist]` or `[Shared]` on non-partial class |
| `TBS003` | Warning | `[Persist]` on a shared state class property without `[Shared]` |
| `TBS004` | Error | Invalid `TimeToLive` format |
| `TBS005` | Warning | `ConfigureState` references a property without `[Persist]` or `[Shared]` |
