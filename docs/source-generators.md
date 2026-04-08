# Source Generators

TheBlazorState uses three Roslyn incremental source generators to eliminate boilerplate. This page explains what gets generated, how to troubleshoot issues, and what diagnostics to look for.

## The Three Generators

### 1. PersistIncrementalGenerator

**Trigger:** Properties with `[Persist]` attribute

**Generates:**
- `[Inject] private StateManager __stateManager` — auto-injected dependency
- Backing field (`__PropertyName_backing`)
- `StateMeta` field (`__PropertyName_meta`)
- Property implementation with change detection
- `{PropertyName}Meta` public accessor
- Nested `__StateContext` class with `PropertyConfigurator<T>` per property
- `OnInitialized()` override — sync restoration from PrerenderHtml / ServerMemoryCache
- `OnInitializedAsync()` override — async restoration from browser strategies + factory invocation
- `Dispose()` override — cleanup of meta handlers and subscriptions
- Partial method declarations: `ConfigureState()`, `__SubscribeToSharedState()`, `__UnsubscribeFromSharedState()`

### 2. SharedIncrementalGenerator

**Trigger:** Properties with `[Shared]` attribute

**Generates:**
- Backing field (`__PropertyName_backing`)
- `StateMeta` field (`__PropertyName_meta`)
- Property implementation with equality check, `MarkDirty()`, `RaiseChanged()`, and `StateChanged?.Invoke()`
- `{PropertyName}Meta` public accessor
- `INotifyStateChanged` interface implementation
- `public event Action? StateChanged` on the class

### 3. InjectSubscriptionGenerator

**Trigger:** `[Inject]` properties where the injected type has `[Shared]` properties

**Generates (in components with [Persist]):**
```csharp
partial void __SubscribeToSharedState()
{
    PropertyName.StateChanged += __OnSharedStateChanged;
}

partial void __UnsubscribeFromSharedState()
{
    PropertyName.StateChanged -= __OnSharedStateChanged;
}
```

**Generates (in components without [Persist]):**
```csharp
protected override void OnInitialized()
{
    base.OnInitialized();
    PropertyName.StateChanged += __OnSharedStateChanged;
}

private void __OnSharedStateChanged() => InvokeAsync(StateHasChanged);

public void Dispose()
{
    PropertyName.StateChanged -= __OnSharedStateChanged;
}
```

## Diagnostics

The generators emit diagnostics when they detect misuse:

| ID | Severity | Description | Fix |
|---|---|---|---|
| **TBS001** | Error | Property has `[Persist]` or `[Shared]` but is not `partial` | Add `partial` to the property declaration |
| **TBS002** | Error | Property has `[Persist]` or `[Shared]` but the containing class is not `partial` | Add `partial` to the class declaration |
| **TBS004** | Error | `TimeToLive` value is not a valid `TimeSpan` string | Use format `"hh:mm:ss"` or `"d.hh:mm:ss"` |

### Example: TBS001

```csharp
// ERROR TBS001: Property 'Count' has [Persist] but is not declared as partial
public partial class Counter : ComponentBase
{
    [Persist]
    public int Count { get; set; }  // ← Missing 'partial'
}
```

Fix:
```csharp
[Persist]
public partial int Count { get; set; }  // ✓
```

### Example: TBS002

```csharp
// ERROR TBS002: Property 'Count' has [Persist] but class 'Counter' is not partial
public class Counter : ComponentBase  // ← Missing 'partial'
{
    [Persist]
    public partial int Count { get; set; }
}
```

Fix:
```csharp
public partial class Counter : ComponentBase  // ✓
{
    [Persist]
    public partial int Count { get; set; }
}
```

## Viewing Generated Code

### In Visual Studio / Rider

Navigate to: **Dependencies → Analyzers → TheBlazorState.Generators** → expand to see generated `.cs` files.

### On disk

After building, generated files are in:
```
obj/Debug/net10.0/generated/TheBlazorState.Generators/
```

### With MSBuild

Add to your `.csproj` to emit generated source to disk:
```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)/GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## Troubleshooting

### "Property is not partial" error

Both the property and its containing class must be `partial`:

```csharp
public partial class MyComponent : ComponentBase  // ✓ partial class
{
    [Persist]
    public partial int Count { get; set; }         // ✓ partial property
}
```

### ConfigureState is not called

Make sure the method signature matches exactly:

```csharp
partial void ConfigureState(__StateContext ctx);  // ← declared by generator
```

Your implementation:
```csharp
partial void ConfigureState(__StateContext ctx)    // ← must match
{
    ctx.MyProperty.LoadFrom(async () => await ...);
}
```

The `__StateContext` type is a nested class generated inside your component. If IntelliSense doesn't resolve it, build once to trigger the generator.

### Component doesn't re-render on shared state changes

Check that:
1. The state class has `[Shared]` on its properties (not just `[Persist]`)
2. The component is `partial`
3. The component uses `[Inject]` (not manual DI)
4. You've built the solution (generators run at compile time)

### Cross-tab sync not working

1. Verify the property uses `StorageStrategy.LocalStorage()` — other strategies don't sync
2. For the receiving component, inherit `StateComponentBase`
3. Check browser console for JS errors (the `theblazorstate.js` module must load)

### Generator not running

- Ensure `TheBlazorState` NuGet package is referenced (it includes the generator)
- Clean and rebuild the solution
- Check that your .NET SDK version supports incremental generators (requires .NET 6+ SDK)
