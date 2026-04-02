# Legends Review v1 Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address the top 5 action items from the Legends Review consensus report (7.7/10) to bring BlazorStatePlus to a shippable v1.0 state.

**Architecture:** Five independent tasks. Task 1 wires `OnChanged` to `StateHasChanged` in generated code (the #1 consensus finding). Task 2 fixes the `InitializeIfNeeded` return value bug. Task 3 adds runtime duplicate-key detection in `StateManager`. Task 4 adds missing diagnostic tests for BSP006/BSP007/BSP011 and renames `__DisposeSlices` to `DisposeSlices`. Task 5 is cleanup: remove dead `ClassLocation` from `ComponentModel`, move `SliceBuilder` to correct namespace.

**Tech Stack:** C# / .NET 10 / Roslyn Incremental Source Generators (netstandard2.0) / xUnit / Shouldly / bUnit

---

## File Map

| Task | File | Action |
|------|------|--------|
| 1 | `BlazorStatePlus.Generators/Emitter.cs` | Modify: emit `OnChanged += () => InvokeAsync(StateHasChanged)` per slice + unsubscribe in Dispose |
| 1 | `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs` | Modify: add test verifying StateHasChanged wiring in generated output |
| 2 | `BlazorStatePlus/Services/StateSlice.cs` | Modify: fix `InitializeIfNeeded` / `InitializeIfNeededAsync` return value |
| 2 | `BlazorStatePlus.Generators.Tests/RuntimeTests.cs` | Modify: add test for equality-no-op case |
| 3 | `BlazorStatePlus/Services/StateManager.cs` | Modify: track registered keys, throw on duplicates |
| 3 | `BlazorStatePlus.Tests/StateManagerTests.cs` | Modify: add duplicate-key test |
| 4 | `BlazorStatePlus.Generators/DiagnosticDescriptors.cs` | Modify: rename `__DisposeSlices` to `DisposeSlices` in BSP006 message |
| 4 | `BlazorStatePlus.Generators/Emitter.cs` | Modify: rename emitted method from `__DisposeSlices` to `DisposeSlices` |
| 4 | `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs` | Modify: add BSP006, BSP007, BSP011 tests |
| 5 | `BlazorStatePlus.Generators/ComponentModel.cs` | Modify: remove dead `ClassLocation`, convert to records |
| 5 | `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` | Modify: stop setting `ClassLocation` on `ComponentModel` |
| 5 | `BlazorStatePlus/Generators/SliceBuilder.cs` | Move to `BlazorStatePlus/Builders/SliceBuilder.cs`, change namespace |
| 5 | `BlazorStatePlus.Generators/Emitter.cs` | Modify: update fully-qualified `SliceBuilder` reference |

---

### Task 1: Wire OnChanged to StateHasChanged in generated code

**Files:**
- Modify: `BlazorStatePlus.Generators/Emitter.cs:54-61` (OnInitialized slice creation loop)
- Modify: `BlazorStatePlus.Generators/Emitter.cs:119-143` (Dispose methods)
- Modify: `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs`:

```csharp
[Fact]
public void GeneratedCode_WiresOnChangedToStateHasChanged()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter;
        }
        """;

    var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

    diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
    generatedSource.ShouldNotBeNull();
    generatedSource.ShouldContain("OnChanged");
    generatedSource.ShouldContain("InvokeAsync");
    generatedSource.ShouldContain("StateHasChanged");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "GeneratedCode_WiresOnChangedToStateHasChanged" -v minimal`
Expected: FAIL — generated code does not contain `InvokeAsync` or `StateHasChanged`

- [ ] **Step 3: Modify Emitter to wire OnChanged in OnInitialized**

In `BlazorStatePlus.Generators/Emitter.cs`, after each slice creation in the `OnInitialized` method (after line 60 `{optionsArg});`), add the subscription:

```csharp
sb.AppendLine($"        {field.FieldName}.OnChanged += () => InvokeAsync(StateHasChanged);");
```

The full loop in the `OnInitialized` section becomes:

```csharp
foreach (var field in model.Fields)
{
    var optionsArg = BuildOptionsArg(field);
    sb.AppendLine($"        {field.FieldName} = __sm.CreateSlice<{field.FullTypeArgument}>(");
    sb.AppendLine($"            __ctxLocal.{field.PropertyName}.ResolveKey(\"{EscapeString(field.BaseKey)}\"),");
    sb.AppendLine($"            __ctxLocal.{field.PropertyName}.GetDefaultValue(),");
    sb.AppendLine($"            {optionsArg});");
    sb.AppendLine($"        {field.FieldName}.OnChanged += () => InvokeAsync(StateHasChanged);");
}
```

No unsubscribe is needed in Dispose — `StateSlice.Dispose()` already sets `OnChanged = null`, clearing all subscribers.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "GeneratedCode_WiresOnChangedToStateHasChanged" -v minimal`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test -v minimal`
Expected: All tests pass (existing + new)

- [ ] **Step 6: Commit**

```bash
git add BlazorStatePlus.Generators/Emitter.cs BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs
git commit -m "feat: auto-wire OnChanged to StateHasChanged in generated code"
```

---

### Task 2: Fix InitializeIfNeeded return value bug

**Files:**
- Modify: `BlazorStatePlus/Services/StateSlice.cs:55-71`
- Modify: `BlazorStatePlus.Generators.Tests/RuntimeTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `BlazorStatePlus.Generators.Tests/RuntimeTests.cs`:

```csharp
[Fact]
public void InitializeIfNeeded_WhenNotRestored_SameValue_ReturnsFalse()
{
    var slice = CreateSlice(42, wasRestored: false);
    var result = slice.InitializeIfNeeded(42);
    result.ShouldBeFalse();
}

[Fact]
public async Task InitializeIfNeededAsync_WhenNotRestored_SameValue_ReturnsFalse()
{
    var slice = CreateSlice(42, wasRestored: false);
    var result = await slice.InitializeIfNeededAsync(() => Task.FromResult(42));
    result.ShouldBeFalse();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "SameValue_ReturnsFalse" -v minimal`
Expected: FAIL — both return `true` because the method doesn't check whether the setter actually changed the value

- [ ] **Step 3: Fix InitializeIfNeeded in StateSlice.cs**

Replace the two methods in `BlazorStatePlus/Services/StateSlice.cs`:

```csharp
public bool InitializeIfNeeded(T value)
{
    if (WasRestored && !IsStale)
        return false;

    if (EqualityComparer<T>.Default.Equals(_value, value))
        return false;

    Value = value;
    return true;
}

public async Task<bool> InitializeIfNeededAsync(Func<Task<T>> factory)
{
    if (WasRestored && !IsStale)
        return false;

    var value = await factory();

    if (EqualityComparer<T>.Default.Equals(_value, value))
        return false;

    Value = value;
    return true;
}
```

Note: we check equality against `_value` (the backing field) before calling the `Value` setter, so we avoid the duplicate equality check in the setter. The factory is still called in the async version (we can't know the result without calling it), but we avoid the assignment and change notification if the value is equal.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "SameValue_ReturnsFalse" -v minimal`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test -v minimal`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add BlazorStatePlus/Services/StateSlice.cs BlazorStatePlus.Generators.Tests/RuntimeTests.cs
git commit -m "fix: InitializeIfNeeded returns false when value equals current (BUG-1)"
```

---

### Task 3: Add runtime duplicate-key detection in StateManager

**Files:**
- Modify: `BlazorStatePlus/Services/StateManager.cs`
- Modify: `BlazorStatePlus.Tests/StateManagerTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `BlazorStatePlus.Tests/StateManagerTests.cs`:

```csharp
[Fact]
public void CreateSlice_DuplicateKey_ThrowsInvalidOperationException()
{
    using var manager = CreateManager();
    manager.CreateSlice<int>("counter");

    Should.Throw<InvalidOperationException>(() => manager.CreateSlice<int>("counter"));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Tests --filter "DuplicateKey_ThrowsInvalidOperationException" -v minimal`
Expected: FAIL — no exception thrown, second slice silently created

- [ ] **Step 3: Add key tracking to StateManager**

In `BlazorStatePlus/Services/StateManager.cs`, add a `HashSet<string>` field and check in `CreateSlice`:

Add field after the existing fields:

```csharp
private readonly HashSet<string> _registeredKeys = [];
```

Add check in `CreateSlice` after `var options = BuildOptions(key, configure);`:

```csharp
if (!_registeredKeys.Add(options.Key!))
    throw new InvalidOperationException(
        $"A slice with key '{options.Key}' has already been registered. Each slice key must be unique.");
```

In `Dispose`, clear the set:

```csharp
_registeredKeys.Clear();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BlazorStatePlus.Tests --filter "DuplicateKey_ThrowsInvalidOperationException" -v minimal`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test -v minimal`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add BlazorStatePlus/Services/StateManager.cs BlazorStatePlus.Tests/StateManagerTests.cs
git commit -m "fix: throw on duplicate slice keys in StateManager.CreateSlice"
```

---

### Task 4: Add missing diagnostic tests + rename DisposeSlices

**Files:**
- Modify: `BlazorStatePlus.Generators/DiagnosticDescriptors.cs:36`
- Modify: `BlazorStatePlus.Generators/Emitter.cs:123`
- Modify: `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`

- [ ] **Step 1: Write BSP006 test (existing IDisposable)**

Add to `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`:

```csharp
[Fact]
public void BSP006_ExistingDisposable_ReportsWarning()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;
        using System;

        namespace TestApp;

        public partial class MyComponent : ComponentBase, IDisposable
        {
            [Slice]
            private IStateSlice<int> _counter;

            public void Dispose() { }
        }
        """;

    var diagnostics = TestHelper.GetDiagnostics(source);
    diagnostics.ShouldContain(d => d.Id == "BSP006");
}
```

- [ ] **Step 2: Write BSP007 test (duplicate keys)**

Add to `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`:

```csharp
[Fact]
public void BSP007_DuplicateKey_ReportsError()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter;

            [Slice]
            private IStateSlice<string> _Counter;
        }
        """;

    var diagnostics = TestHelper.GetDiagnostics(source);
    diagnostics.ShouldContain(d => d.Id == "BSP007");
}
```

Note: `_counter` and `_Counter` both resolve to base key `MyComponent.counter` (field name minus underscore), so they collide.

- [ ] **Step 3: Write BSP011 test (existing OnInitialized)**

Add to `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`:

```csharp
[Fact]
public void BSP011_ExistingOnInitialized_ReportsWarning()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter;

            protected override void OnInitialized()
            {
                base.OnInitialized();
            }
        }
        """;

    var diagnostics = TestHelper.GetDiagnostics(source);
    diagnostics.ShouldContain(d => d.Id == "BSP011");
}
```

- [ ] **Step 4: Run new diagnostic tests to verify they pass**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "BSP006|BSP007|BSP011" -v minimal`
Expected: All 3 PASS (the diagnostics already exist and fire — we're just adding missing test coverage)

- [ ] **Step 5: Write test for DisposeSlices rename**

Add to `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs`:

```csharp
[Fact]
public void UserImplementsDisposable_EmitsDisposeSlicesHelper()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;
        using System;

        namespace TestApp;

        public partial class MyComponent : ComponentBase, IDisposable
        {
            [Slice]
            private IStateSlice<int> _counter;

            public void Dispose() { }
        }
        """;

    var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

    generatedSource.ShouldNotBeNull();
    generatedSource.ShouldContain("private void DisposeSlices()");
    generatedSource.ShouldNotContain("__DisposeSlices");
}
```

- [ ] **Step 6: Run rename test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "UserImplementsDisposable_EmitsDisposeSlicesHelper" -v minimal`
Expected: FAIL — generated code still contains `__DisposeSlices`

- [ ] **Step 7: Rename __DisposeSlices to DisposeSlices**

In `BlazorStatePlus.Generators/Emitter.cs`, change line 123:

```csharp
// Change from:
sb.AppendLine("    private void __DisposeSlices()");
// Change to:
sb.AppendLine("    private void DisposeSlices()");
```

In `BlazorStatePlus.Generators/DiagnosticDescriptors.cs`, update BSP006 message (line 36):

```csharp
// Change from:
"Class '{0}' implements IDisposable; call __DisposeSlices() from your Dispose method",
// Change to:
"Class '{0}' implements IDisposable; call DisposeSlices() from your Dispose method",
```

- [ ] **Step 8: Run all tests to verify they pass**

Run: `dotnet test -v minimal`
Expected: All tests pass

- [ ] **Step 9: Commit**

```bash
git add BlazorStatePlus.Generators/DiagnosticDescriptors.cs BlazorStatePlus.Generators/Emitter.cs BlazorStatePlus.Generators.Tests/DiagnosticTests.cs BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs
git commit -m "test: add BSP006/BSP007/BSP011 diagnostic tests, rename DisposeSlices"
```

---

### Task 5: Cleanup — remove dead ClassLocation, move SliceBuilder namespace

**Files:**
- Modify: `BlazorStatePlus.Generators/ComponentModel.cs`
- Modify: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs:394-403`
- Rename: `BlazorStatePlus/Generators/SliceBuilder.cs` → `BlazorStatePlus/Builders/SliceBuilder.cs`
- Modify: `BlazorStatePlus.Generators/Emitter.cs:154` (update fully-qualified SliceBuilder reference)

- [ ] **Step 1: Remove ClassLocation from ComponentModel**

In `BlazorStatePlus.Generators/ComponentModel.cs`, remove the `ClassLocation` property and convert both classes to records:

```csharp
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators;

internal sealed record ComponentModel
{
    public required string Namespace { get; init; }
    public required string ClassName { get; init; }
    public required List<SliceFieldModel> Fields { get; init; }
    public required bool UserImplementsDisposable { get; init; }
    public required bool UserOverridesOnInitialized { get; init; }
    public required bool UserOverridesOnInitializedAsync { get; init; }
}

internal sealed record SliceFieldModel
{
    public required string FieldName { get; init; }
    public required string PropertyName { get; init; }
    public required string TypeArgument { get; init; }
    public required string FullTypeArgument { get; init; }
    public string? TimeToLive { get; init; }
    public required string BaseKey { get; init; }
    public required Location FieldLocation { get; init; }
}
```

- [ ] **Step 2: Update SliceIncrementalGenerator to match**

In `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs`, update the `ComponentModel` construction (around line 394). Remove `ClassLocation`:

```csharp
var model = new ComponentModel
{
    Namespace = ns,
    ClassName = className,
    Fields = validFields,
    UserImplementsDisposable = userImplementsDisposable,
    UserOverridesOnInitialized = userOverridesOnInitialized,
    UserOverridesOnInitializedAsync = userOverridesOnInitializedAsync,
};
```

Also update all `SliceFieldModel` constructions (around line 337) to use `init` syntax:

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

(This already uses object initializer syntax, so no change needed to the initializer — just confirm it compiles with `init` setters.)

- [ ] **Step 3: Run tests to verify nothing broke**

Run: `dotnet test -v minimal`
Expected: All tests pass

- [ ] **Step 4: Move SliceBuilder to correct namespace**

Rename file: `BlazorStatePlus/Generators/SliceBuilder.cs` → `BlazorStatePlus/Builders/SliceBuilder.cs`

Change namespace in the file:

```csharp
// Change from:
namespace BlazorStatePlus.Generators;
// Change to:
namespace BlazorStatePlus.Builders;
```

- [ ] **Step 5: Update Emitter fully-qualified reference**

In `BlazorStatePlus.Generators/Emitter.cs`, line 154, update the `SliceBuilder` reference:

```csharp
// Change from:
sb.AppendLine($"        public global::BlazorStatePlus.Generators.SliceBuilder<{field.FullTypeArgument}> {field.PropertyName} {{ get; }} = new global::BlazorStatePlus.Generators.SliceBuilder<{field.FullTypeArgument}>();");
// Change to:
sb.AppendLine($"        public global::BlazorStatePlus.Builders.SliceBuilder<{field.FullTypeArgument}> {field.PropertyName} {{ get; }} = new global::BlazorStatePlus.Builders.SliceBuilder<{field.FullTypeArgument}>();");
```

- [ ] **Step 6: Update SliceBuilderTests using**

In `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs`, the `using` statement should already resolve via project reference, but verify it compiles. If needed, update:

```csharp
// Change from:
using BlazorStatePlus.Generators;
// (if present) Change to:
using BlazorStatePlus.Builders;
```

- [ ] **Step 7: Run full test suite**

Run: `dotnet test -v minimal`
Expected: All tests pass

- [ ] **Step 8: Delete old directory if empty**

```bash
rmdir BlazorStatePlus/Generators 2>/dev/null || true
```

(The `Generators` folder under `BlazorStatePlus/` should be empty after moving `SliceBuilder.cs`.)

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor: convert models to records, move SliceBuilder to Builders namespace"
```

---

## Self-Review Checklist

1. **Spec coverage:** All 5 consensus actions are covered: StateHasChanged wiring (Task 1), InitializeIfNeeded BUG-1 (Task 2), duplicate-key detection (Task 3), missing diagnostic tests + DisposeSlices rename (Task 4), cleanup (Task 5).
2. **Placeholder scan:** All steps have complete code. No TBD/TODO.
3. **Type consistency:** `SliceBuilder` namespace change in Task 5 updates both the Emitter reference and the test imports. `ComponentModel` record conversion uses `required` + `init` which is compatible with the existing object-initializer construction sites.
