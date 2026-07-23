# Legends Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address the top 5 action items from the Legends Review consensus report — fix the BSP012/null! contradiction, wire up real diagnostic Locations + split FieldData, delete BuildOptions, fix the CreateAndInit TOCTOU bug, and add ILogger to StateManager.

**Architecture:** Six targeted changes. Tasks 1-2 fix the generator pipeline (BSP012 false positive, FieldData caching + diagnostic locations). Task 3 removes the dead `BuildOptions` method. Task 4 deletes the TOCTOU-buggy `CreateAndInit`/`CreateAndInitAsync`. Task 5 adds logging to `StateManager`. Task 6 cleans up the demo nullability mismatch. Each task includes TDD with existing patterns (xUnit + Shouldly).

**Tech Stack:** C# / .NET 10 / Roslyn Incremental Source Generators / xUnit / Shouldly

---

## File Structure

| File | Responsibility | Action |
|------|---------------|--------|
| `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` | Generator pipeline, FieldData, validation | Modify: detect null! initializer, split FieldData into record + location struct |
| `BlazorStatePlus.Generators/Emitter.cs` | Code generation templates | Modify: inline options arg (remove BuildOptions calls) |
| `BlazorStatePlus.Generators/ComponentModel.cs` | Data models for emitter | Modify: store real Location on models |
| `BlazorStatePlus/Generators/SliceBuilder.cs` | Fluent runtime config builder | Modify: delete BuildOptions method |
| `BlazorStatePlus/Services/StateManager.cs` | Central state management service | Modify: delete CreateAndInit, add ILogger |
| `BlazorStatePlus.Demo/Components/Pages/Weather.razor.cs` | Demo page | Modify: fix nullability mismatch |
| `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs` | Diagnostic test coverage | Modify: add BSP012 null! test, location test |
| `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs` | SliceBuilder test coverage | Modify: remove BuildOptions test |

---

### Task 1: Fix the `= null!` / BSP012 contradiction

The generator warns (BSP012) when a `[Slice]` field has an initializer, but users *must* write `= null!` to suppress CS8618. Fix: detect when the initializer is `null!` (a `SuppressNullableWarningExpression` wrapping a null literal) and skip BSP012.

**Files:**
- Modify: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs:73-74`
- Modify: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs:314-319`
- Test: `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`

- [ ] **Step 1: Write failing test — `null!` initializer should NOT produce BSP012**

Add to `DiagnosticTests.cs`:

```csharp
[Fact]
public void BSP012_NullForgivingInitializer_NoDiagnostic()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter = null!;
        }
        """;

    var diagnostics = TestHelper.GetDiagnostics(source);
    diagnostics.ShouldNotContain(d => d.Id == "BSP012");
}
```

- [ ] **Step 2: Write failing test — real initializer SHOULD produce BSP012**

Add to `DiagnosticTests.cs`:

```csharp
[Fact]
public void BSP012_RealInitializer_ReportsDiagnostic()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter = default!;
        }
        """;

    var diagnostics = TestHelper.GetDiagnostics(source);
    diagnostics.ShouldContain(d => d.Id == "BSP012");
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "BSP012" -v n`
Expected: `BSP012_NullForgivingInitializer_NoDiagnostic` FAILS (BSP012 is currently emitted for `null!`). `BSP012_RealInitializer_ReportsDiagnostic` PASSES.

- [ ] **Step 4: Update `ExtractFieldInfo` to distinguish `null!` from real initializers**

In `SliceIncrementalGenerator.cs`, replace line 74:

```csharp
// FROM:
bool hasInitializer = variableDeclarator.Initializer != null;

// TO:
bool hasInitializer = false;
if (variableDeclarator.Initializer is EqualsValueClauseSyntax initializer)
{
    var initValue = initializer.Value;
    if (initValue is PostfixUnaryExpressionSyntax { RawKind: var kind, Operand: LiteralExpressionSyntax literal }
        && kind == (int)SyntaxKind.SuppressNullableWarningExpression
        && literal.IsKind(SyntaxKind.NullLiteralExpression))
    {
        hasInitializer = false;
    }
    else
    {
        hasInitializer = true;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "BSP012" -v n`
Expected: Both tests PASS.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All tests PASS.

- [ ] **Step 7: Commit**

```bash
git add BlazorStatePlus.Generators/SliceIncrementalGenerator.cs BlazorStatePlus.Generators.Tests/DiagnosticTests.cs
git commit -m "fix: skip BSP012 for null! initializer — only warn on meaningful initializers"
```

---

### Task 2: Wire up real `Location` on diagnostics and split `FieldData` for correct incremental caching

Two problems, one fix: (a) All diagnostics use `Location.None` — no IDE squiggles. (b) `FieldData.Equals` includes `SpanStart`/`SpanLength` etc., so any whitespace edit invalidates the incremental cache. Fix: convert `FieldData` to a `record` (semantic fields only), carry `Location` objects separately, and use them in diagnostics.

**Files:**
- Modify: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` (split FieldData, wire locations)
- Modify: `BlazorStatePlus.Generators/ComponentModel.cs` (store real Location on models)
- Test: `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`

- [ ] **Step 1: Write failing test — diagnostics should have source locations**

Add to `DiagnosticTests.cs`:

```csharp
[Fact]
public void BSP001_NonPartialClass_HasSourceLocation()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter;
        }
        """;

    var diagnostics = TestHelper.GetDiagnostics(source);
    var bsp001 = diagnostics.First(d => d.Id == "BSP001");
    bsp001.Location.ShouldNotBe(Location.None);
    bsp001.Location.SourceTree.ShouldNotBeNull();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "HasSourceLocation" -v n`
Expected: FAIL — `bsp001.Location` is `Location.None`.

- [ ] **Step 3: Add `FieldLocationData` struct to `SliceIncrementalGenerator.cs`**

Add inside the `SliceIncrementalGenerator` class, BEFORE the `FieldData` class:

```csharp
/// <summary>
/// Location metadata excluded from equality to avoid poisoning incremental cache.
/// </summary>
private readonly struct FieldLocationData
{
    public Location FieldLocation { get; init; }
    public Location ClassLocation { get; init; }
}
```

- [ ] **Step 4: Convert `FieldData` from manual-equality class to record**

Replace the entire `FieldData` class (lines 427-549) with:

```csharp
private sealed record FieldData(
    string FieldName,
    string ContainingClassName,
    string ContainingClassNamespace,
    string FieldTypeDisplayString,
    string? FieldTypeOriginalDefinitionDisplayString,
    string? TypeArgumentName,
    string? TypeArgumentFullyQualified,
    bool IsStatic,
    bool IsGenericType,
    string? TimeToLive,
    bool HasInitializer,
    bool IsPartialClass,
    bool InheritsFromComponentBase,
    bool UserImplementsDisposable,
    bool UserOverridesOnInitialized,
    bool UserOverridesOnInitializedAsync);
```

This removes `FilePath`, `SpanStart`, `SpanLength`, `ClassFilePath`, `ClassSpanStart`, `ClassSpanLength` from equality entirely (they live in `FieldLocationData` now), and deletes the manual `Equals`/`GetHashCode`.

- [ ] **Step 5: Update `ExtractFieldInfo` to return tuple**

Change the method signature:

```csharp
private static (FieldData Data, FieldLocationData Location)? ExtractFieldInfo(
    GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
```

At the end, build both objects:

```csharp
var fieldLocation = fieldSymbol.Locations.FirstOrDefault();
var classLocation = containingType.Locations.FirstOrDefault();

var locationData = new FieldLocationData
{
    FieldLocation = fieldLocation ?? Location.None,
    ClassLocation = classLocation ?? Location.None
};

return (new FieldData(
    FieldName: fieldSymbol.Name,
    ContainingClassName: containingClassName,
    ContainingClassNamespace: containingClassNamespace,
    FieldTypeDisplayString: fieldTypeDisplayString,
    FieldTypeOriginalDefinitionDisplayString: fieldTypeOriginalDefinitionDisplayString,
    TypeArgumentName: typeArgumentName,
    TypeArgumentFullyQualified: typeArgumentFullyQualified,
    IsStatic: fieldSymbol.IsStatic,
    IsGenericType: isGenericType,
    TimeToLive: timeToLive,
    HasInitializer: hasInitializer,
    IsPartialClass: isPartialClass,
    InheritsFromComponentBase: inheritsFromComponentBase,
    UserImplementsDisposable: userImplementsDisposable,
    UserOverridesOnInitialized: userOverridesOnInitialized,
    UserOverridesOnInitializedAsync: userOverridesOnInitializedAsync),
    locationData);
```

Remove the old `filePath`, `spanStart`, `spanLength`, `classFilePath`, `classSpanStart`, `classSpanLength` local variables and the lines that computed them from `fieldLoc?.SourceTree?.FilePath` etc. — those are replaced by the direct `Location` captures above.

- [ ] **Step 6: Update pipeline and `Execute` to carry tuples**

In `Initialize`:

```csharp
var fieldInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
    SliceAttributeFullName,
    predicate: static (node, _) => node is VariableDeclaratorSyntax,
    transform: static (ctx, ct) => ExtractFieldInfo(ctx, ct))
    .Where(static x => x != null);
```

Update `Execute`:

```csharp
private static void Execute(SourceProductionContext spc,
    ImmutableArray<(FieldData Data, FieldLocationData Location)?> fields)
{
    if (fields.IsDefaultOrEmpty) return;

    var grouped = new Dictionary<(string, string), List<(FieldData Data, FieldLocationData Location)>>();
    foreach (var field in fields)
    {
        if (field == null) continue;
        var key = (field.Value.Data.ContainingClassName, field.Value.Data.ContainingClassNamespace);
        if (!grouped.TryGetValue(key, out var list))
        {
            list = [];
            grouped[key] = list;
        }
        list.Add(field.Value);
    }

    foreach (var kvp in grouped)
        ProcessClass(spc, kvp.Value);
}
```

Update `ProcessClass` signature:

```csharp
private static void ProcessClass(SourceProductionContext spc,
    List<(FieldData Data, FieldLocationData Location)> fields)
```

- [ ] **Step 7: Replace `Location.None` with real locations in `ProcessClass`**

Access data via `fields[0].Data` for field data and `fields[0].Location` for locations. Replace every `Location.None` in diagnostic calls:

For BSP001 (non-partial class): use `field.Location.FieldLocation`
For BSP003 (not ComponentBase): use `first.Location.ClassLocation`
For BSP008 (static field): use `field.Location.FieldLocation`
For BSP002 (invalid field type): use `field.Location.FieldLocation`
For BSP005 (invalid TTL): use `field.Location.FieldLocation`
For BSP012 (has initializer): use `field.Location.FieldLocation`
For BSP007 (duplicate key): use the second field's `FieldLocation`
For BSP006 (existing disposable): use `first.Location.ClassLocation`
For BSP011 (existing OnInitialized): use `first.Location.ClassLocation`

Also update `SliceFieldModel` and `ComponentModel` to store real `Location` values instead of `Location.None`:

```csharp
validFields.Add(new SliceFieldModel
{
    FieldName = field.Data.FieldName,
    PropertyName = propertyName,
    TypeArgument = typeArgument,
    FullTypeArgument = fullTypeArgument!,
    TimeToLive = field.Data.TimeToLive,
    BaseKey = baseKey,
    FieldLocation = field.Location.FieldLocation
});
```

And:

```csharp
var model = new ComponentModel
{
    Namespace = ns,
    ClassName = className,
    Fields = validFields,
    UserImplementsDisposable = userImplementsDisposable,
    UserOverridesOnInitialized = userOverridesOnInitialized,
    UserOverridesOnInitializedAsync = userOverridesOnInitializedAsync,
    ClassLocation = first.Location.ClassLocation
};
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All tests PASS, including the new `BSP001_NonPartialClass_HasSourceLocation`.

- [ ] **Step 9: Commit**

```bash
git add BlazorStatePlus.Generators/SliceIncrementalGenerator.cs BlazorStatePlus.Generators/ComponentModel.cs BlazorStatePlus.Generators.Tests/DiagnosticTests.cs
git commit -m "fix: wire up real Location on diagnostics, split FieldData into record + location struct"
```

---

### Task 3: Delete `BuildOptions` from `SliceBuilder` and inline in Emitter

`BuildOptions` is `return attributeDefaults;` — a pass-through adding zero value. Delete it and emit the options argument directly.

**Files:**
- Modify: `BlazorStatePlus/Generators/SliceBuilder.cs:59-64` (delete method)
- Modify: `BlazorStatePlus.Generators/Emitter.cs:193-206` (inline options arg)
- Test: `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs` (remove test)

- [ ] **Step 1: Delete the `BuildOptions` test from `SliceBuilderTests.cs`**

Remove the entire `BuildOptions_AppliesAttributeDefaults` test method.

- [ ] **Step 2: Delete `BuildOptions` method from `SliceBuilder.cs`**

Remove lines 59-64:

```csharp
[EditorBrowsable(EditorBrowsableState.Never)]
public Action<StateSliceOptions>? BuildOptions(Action<StateSliceOptions>? attributeDefaults = null)
{
    return attributeDefaults;
}
```

- [ ] **Step 3: Update `Emitter.BuildOptionsArg` to emit lambda directly**

Replace the `BuildOptionsArg` method in `Emitter.cs`:

```csharp
private static string BuildOptionsArg(SliceFieldModel field)
{
    if (string.IsNullOrEmpty(field.TimeToLive))
        return "null";

    return $"__o => {{ __o.TimeToLive = global::System.TimeSpan.Parse(\"{EscapeString(field.TimeToLive!)}\"); }}";
}
```

- [ ] **Step 4: Run full test suite**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All tests PASS. If any generated output test asserts on `BuildOptions`, update the assertion.

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus/Generators/SliceBuilder.cs BlazorStatePlus.Generators/Emitter.cs BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs
git commit -m "refactor: delete BuildOptions pass-through, inline options lambda in emitter"
```

---

### Task 4: Delete `CreateAndInit` and `CreateAndInitAsync` from `StateManager`

TOCTOU bug: outer `IsStale` at T1, inner check at T2 can diverge — factory called but value discarded. Generator doesn't use these methods. Also remove dead `options.Key ??= key` fallback.

**Files:**
- Modify: `BlazorStatePlus/Services/StateManager.cs`

- [ ] **Step 1: Delete `CreateAndInit` method (lines 81-92)**

- [ ] **Step 2: Delete `CreateAndInitAsync` method (lines 99-107)**

- [ ] **Step 3: Delete dead `Key` fallback (line 131)**

Change `BuildOptions`:

```csharp
private static StateSliceOptions BuildOptions(string key, Action<StateSliceOptions>? configure)
{
    var options = new StateSliceOptions { Key = key };
    configure?.Invoke(options);
    return options;
}
```

- [ ] **Step 4: Run tests and build demo**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n && dotnet build BlazorStatePlus.Demo -v n`
Expected: All pass — demo uses only `CreateSlice` via the generator.

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus/Services/StateManager.cs
git commit -m "fix: delete CreateAndInit/CreateAndInitAsync (TOCTOU bug) and dead Key fallback"
```

---

### Task 5: Add `ILogger` to `StateManager`

Minimum observability: `Debug`-level logging for restore hits/misses and TTL staleness.

**Files:**
- Modify: `BlazorStatePlus/Services/StateManager.cs`

- [ ] **Step 1: Add `ILogger` to constructor and using**

```csharp
using Microsoft.Extensions.Logging;

public sealed class StateManager(
    PersistentComponentState persistence,
    ILogger<StateManager> logger) : IDisposable
```

- [ ] **Step 2: Add log statements in `CreateSlice` after the restore block**

After the if/else block that sets `restoredValue` and `effectivelyRestored`, add:

```csharp
if (effectivelyRestored)
    logger.LogDebug("Slice '{Key}': restored from prerender", options.Key);
else if (wasRestored && envelope is not null)
    logger.LogDebug("Slice '{Key}': restored value discarded (TTL expired)", options.Key);
else
    logger.LogDebug("Slice '{Key}': no persisted value, using default", options.Key);
```

- [ ] **Step 3: Build and test**

Run: `dotnet build BlazorStatePlus -v n && dotnet build BlazorStatePlus.Demo -v n && dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All succeed. If any test constructs `StateManager` directly, pass `NullLogger<StateManager>.Instance`.

- [ ] **Step 4: Commit**

```bash
git add BlazorStatePlus/Services/StateManager.cs
git commit -m "feat: add ILogger to StateManager for restore/staleness observability"
```

---

### Task 6: Fix demo `WeatherForecast[]?` nullability mismatch

The diff tightened `WeatherService.GetForecastAsync` to return `WeatherForecast[]` (non-nullable), but the slice type in `Weather.razor.cs` is still `IStateSlice<WeatherForecast[]?>`.

**Files:**
- Modify: `BlazorStatePlus.Demo/Components/Pages/Weather.razor.cs:14`

- [ ] **Step 1: Fix the nullability mismatch**

```csharp
// FROM:
private IStateSlice<WeatherForecast[]?> _forecasts = null!;

// TO:
private IStateSlice<WeatherForecast[]> _forecasts = null!;
```

- [ ] **Step 2: Build demo**

Run: `dotnet build BlazorStatePlus.Demo -v n`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus.Demo/Components/Pages/Weather.razor.cs
git commit -m "fix: align Weather slice type with non-nullable WeatherService return type"
```
