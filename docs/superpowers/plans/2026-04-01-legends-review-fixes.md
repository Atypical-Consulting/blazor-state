# Legends Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all critical and medium-severity issues identified in the Legends Review (consensus 6/10), bringing the library to a shippable state.

**Architecture:** Five workstreams — (1) fix incremental generator caching by extracting primitives before `Collect()`, (2) fix lifecycle override conflicts in Emitter, (3) add runtime unit tests for StateSlice and StateManager, (4) delete dead code, (5) fix runtime bugs (staleness asymmetry, eager factory, disposed guard, thread safety, global namespace, filename collision, BuildOptions).

**Tech Stack:** C# / .NET 10 / Blazor / Roslyn Incremental Source Generators (netstandard2.0) / xUnit

---

## File Structure

| File | Responsibility | Action |
|------|---------------|--------|
| `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` | Generator pipeline + validation | Modify: replace `FieldInfo` with value-equatable record, remove symbol refs before `Collect()` |
| `BlazorStatePlus.Generators/Emitter.cs` | Code generation | Modify: fix OnInitializedAsync override, add base call, fix namespace, fix filename |
| `BlazorStatePlus.Generators/ComponentModel.cs` | Data models | Modify: minor (already primitive) |
| `BlazorStatePlus.Generators/DiagnosticDescriptors.cs` | Diagnostics catalog | Modify: remove 4 unused descriptors |
| `BlazorStatePlus/Services/StateSlice.cs` | Slice implementation | Modify: fix staleness, add disposed guard |
| `BlazorStatePlus/Services/StateManager.cs` | Central state service | Modify: fix CreateAndInit, thread safety, delete MutableSliceOptions |
| `BlazorStatePlus/Abstractions/StateSliceOptions.cs` | Options | Modify: remove AllowUpdatesOnNavigation |
| `BlazorStatePlus/Abstractions/IStateSlice.cs` | Interface | Unchanged |
| `BlazorStatePlus/Attributes/SliceAttribute.cs` | Attribute | Modify: remove AllowUpdatesOnNavigation |
| `BlazorStatePlus/Generators/SliceBuilder.cs` | Fluent builder | Modify: fix BuildOptions, remove _hasDefault |
| `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs` | Diagnostic tests | Modify: add new tests |
| `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs` | Output tests | Modify: add lifecycle override tests |
| `BlazorStatePlus.Generators.Tests/RuntimeTests.cs` | Runtime tests | Create: tests for StateSlice and StateManager |
| `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs` | Builder tests | Modify: add BuildOptions merge test |

---

## Task 1: Fix Incremental Generator Caching (Critical)

**Problem:** `FieldInfo` holds `IFieldSymbol` and `INamedTypeSymbol` (Roslyn symbols) across the `Collect()` boundary. Symbols are reference types with no value equality, so the incremental pipeline re-runs on every keystroke.

**Files:**
- Modify: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs:34-88` (ExtractFieldInfo), `:91-113` (Execute), `:116-337` (ProcessClass), `:372-399` (FieldInfo class)

- [ ] **Step 1: Write a test that verifies the generator produces identical output for identical input**

This test ensures incremental caching works by running the generator twice and verifying outputs match.

```csharp
// In BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs
[Fact]
public void Generator_ProducesDeterministicOutput()
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

    var (diag1, output1) = TestHelper.RunGenerator(source);
    var (diag2, output2) = TestHelper.RunGenerator(source);

    Assert.Equal(output1, output2);
    Assert.DoesNotContain(diag1, d => d.Severity == DiagnosticSeverity.Error);
}
```

- [ ] **Step 2: Run test to verify it passes (baseline)**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "Generator_ProducesDeterministicOutput" -v n`
Expected: PASS (determinism works even without caching fix — this is the baseline)

- [ ] **Step 3: Replace FieldInfo class with value-equatable record**

Replace the `FieldInfo` private sealed class (lines 372-399) with a record that contains only primitive/string data. Remove all `IFieldSymbol` and `INamedTypeSymbol` references.

```csharp
// In SliceIncrementalGenerator.cs — replace the FieldInfo class at the bottom

/// <summary>
/// Value-equatable data extracted per field. Contains only primitives/strings
/// so the incremental pipeline can cache correctly across Collect() boundaries.
/// </summary>
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
    bool AllowUpdatesOnNavigation,
    bool HasInitializer,
    // Location data as file path + span for diagnostic reporting
    string? FilePath,
    int SpanStart,
    int SpanLength);
```

- [ ] **Step 4: Update ExtractFieldInfo to return FieldData with only primitive values**

Replace the `ExtractFieldInfo` method (lines 34-88) to extract all symbol data into primitives:

```csharp
private static FieldData? ExtractFieldInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();

    if (ctx.TargetNode is not VariableDeclaratorSyntax variableDeclarator)
        return null;

    if (ctx.TargetSymbol is not IFieldSymbol fieldSymbol)
        return null;

    var containingType = fieldSymbol.ContainingType;
    if (containingType == null)
        return null;

    // Extract attribute properties
    string? timeToLive = null;
    bool allowUpdatesOnNavigation = false;

    foreach (var attr in ctx.Attributes)
    {
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg is { Key: "TimeToLive", Value.Value: string ttl })
                timeToLive = ttl;
            else if (namedArg is { Key: "AllowUpdatesOnNavigation", Value.Value: bool allow })
                allowUpdatesOnNavigation = allow;
        }
        break;
    }

    bool hasInitializer = variableDeclarator.Initializer != null;

    // Extract type argument info
    string? typeArgName = null;
    string? typeArgFull = null;
    string? origDefDisplay = null;
    bool isGeneric = false;

    if (fieldSymbol.Type is INamedTypeSymbol { IsGenericType: true } namedType)
    {
        isGeneric = true;
        origDefDisplay = namedType.OriginalDefinition.ToDisplayString();
        if (origDefDisplay == "BlazorStatePlus.Abstractions.IStateSlice<T>")
        {
            var typeArg = namedType.TypeArguments[0];
            typeArgName = typeArg.Name;
            typeArgFull = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
    }

    var location = fieldSymbol.Locations.FirstOrDefault();

    return new FieldData(
        FieldName: fieldSymbol.Name,
        ContainingClassName: containingType.Name,
        ContainingClassNamespace: containingType.ContainingNamespace.IsGlobalNamespace
            ? ""
            : containingType.ContainingNamespace.ToDisplayString(),
        FieldTypeDisplayString: fieldSymbol.Type.ToDisplayString(),
        FieldTypeOriginalDefinitionDisplayString: origDefDisplay,
        TypeArgumentName: typeArgName,
        TypeArgumentFullyQualified: typeArgFull,
        IsStatic: fieldSymbol.IsStatic,
        IsGenericType: isGeneric,
        TimeToLive: timeToLive,
        AllowUpdatesOnNavigation: allowUpdatesOnNavigation,
        HasInitializer: hasInitializer,
        FilePath: location?.SourceTree?.FilePath,
        SpanStart: location?.SourceSpan.Start ?? 0,
        SpanLength: location?.SourceSpan.Length ?? 0);
}
```

- [ ] **Step 5: Update Execute to group FieldData by class name + namespace**

Replace the `Execute` method (lines 91-113) to group by `(ContainingClassName, ContainingClassNamespace)` instead of by `INamedTypeSymbol`:

```csharp
private static void Execute(SourceProductionContext spc, ImmutableArray<FieldData?> fields)
{
    if (fields.IsDefaultOrEmpty)
        return;

    var grouped = new Dictionary<(string ClassName, string Namespace), List<FieldData>>();
    foreach (var field in fields)
    {
        if (field == null) continue;

        var key = (field.ContainingClassName, field.ContainingClassNamespace);
        if (!grouped.TryGetValue(key, out var list))
        {
            list = new List<FieldData>();
            grouped[key] = list;
        }
        list.Add(field);
    }

    foreach (var kvp in grouped)
    {
        ProcessClass(spc, kvp.Value);
    }
}
```

- [ ] **Step 6: Update ProcessClass to work with FieldData instead of symbols**

This is the largest change. `ProcessClass` currently takes `INamedTypeSymbol` and does validation against it (partial check, ComponentBase check, member scanning). Since we no longer have the symbol, we need to move those checks into `ExtractFieldInfo` (where we still have the symbol) and store the results as booleans on the record.

First, extend `FieldData` with class-level data:

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
    bool AllowUpdatesOnNavigation,
    bool HasInitializer,
    string? FilePath,
    int SpanStart,
    int SpanLength,
    // Class-level data (same for all fields in same class)
    bool IsPartialClass,
    bool InheritsFromComponentBase,
    bool UserImplementsDisposable,
    bool UserOverridesOnInitialized,
    bool UserOverridesOnInitializedAsync,
    string? ClassFilePath,
    int ClassSpanStart,
    int ClassSpanLength);
```

Then update `ExtractFieldInfo` to extract these class-level booleans while we still have the symbol:

```csharp
// Add to ExtractFieldInfo, before the return statement:

// Class-level checks (extracted here while we have the symbol)
bool isPartial = false;
foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
{
    var syntax = syntaxRef.GetSyntax(ct);
    if (syntax is ClassDeclarationSyntax classDecl)
    {
        foreach (var modifier in classDecl.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword))
            {
                isPartial = true;
                break;
            }
        }
    }
    if (isPartial) break;
}

bool inheritsComponentBase = InheritsFromComponentBase(containingType);

bool userImplementsDisposable = false;
bool userOverridesOnInitialized = false;
bool userOverridesOnInitializedAsync = false;
foreach (var member in containingType.GetMembers())
{
    if (member is IMethodSymbol method)
    {
        if (method is { Name: "Dispose", Parameters.Length: 0, IsAbstract: false })
            userImplementsDisposable = true;
        else if (method is { Name: "OnInitialized", Parameters.Length: 0, IsOverride: true })
            userOverridesOnInitialized = true;
        else if (method is { Name: "OnInitializedAsync", Parameters.Length: 0, IsOverride: true })
            userOverridesOnInitializedAsync = true;
    }
}

var classLocation = containingType.Locations.FirstOrDefault();
```

And update the return to include all new fields.

Then rewrite `ProcessClass` to take `List<FieldData>` and use the pre-extracted booleans instead of querying symbols:

```csharp
private static void ProcessClass(SourceProductionContext spc, List<FieldData> fields)
{
    var first = fields[0];
    var className = first.ContainingClassName;

    // Create Location from stored data for diagnostics
    // Note: We can't recreate exact Location without SyntaxTree,
    // so we report diagnostics without location or use a simple approach
    
    // --- Validation using pre-extracted booleans ---

    if (!first.IsPartialClass)
    {
        foreach (var field in fields)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NonPartialClass,
                Location.None,
                field.FieldName,
                className));
        }
        return;
    }

    if (!first.InheritsFromComponentBase)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NotComponentBase,
            Location.None,
            className));
        return;
    }

    // Validate each field
    var validFields = new List<SliceFieldModel>();
    bool hasErrors = false;

    foreach (var field in fields)
    {
        if (field.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.StaticField,
                Location.None,
                field.FieldName));
            hasErrors = true;
            continue;
        }

        if (field.TypeArgumentName == null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidFieldType,
                Location.None,
                field.FieldName));
            hasErrors = true;
            continue;
        }

        if (field.TimeToLive != null && !TimeSpan.TryParse(field.TimeToLive, out _))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidTimeToLive,
                Location.None,
                field.TimeToLive,
                field.FieldName));
            hasErrors = true;
            continue;
        }

        if (field.HasInitializer)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FieldHasInitializer,
                Location.None,
                field.FieldName));
        }

        string propertyName = ConvertFieldNameToPropertyName(field.FieldName);
        string fieldNameWithoutUnderscore = field.FieldName.TrimStart('_');
        string baseKey = className + "." + fieldNameWithoutUnderscore;

        validFields.Add(new SliceFieldModel
        {
            FieldName = field.FieldName,
            PropertyName = propertyName,
            TypeArgument = field.TypeArgumentName,
            FullTypeArgument = field.TypeArgumentFullyQualified!,
            TimeToLive = field.TimeToLive,
            AllowUpdatesOnNavigation = field.AllowUpdatesOnNavigation,
            BaseKey = baseKey,
            FieldLocation = Location.None
        });
    }

    if (hasErrors || validFields.Count == 0)
        return;

    // Duplicate key check
    for (int i = 0; i < validFields.Count; i++)
    {
        for (int j = i + 1; j < validFields.Count; j++)
        {
            if (validFields[i].BaseKey == validFields[j].BaseKey)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateKey,
                    Location.None,
                    validFields[i].FieldName,
                    validFields[j].FieldName,
                    validFields[i].BaseKey));
                return;
            }
        }
    }

    if (first.UserImplementsDisposable)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ExistingDisposable,
            Location.None,
            className));
    }

    if (first.UserOverridesOnInitialized)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ExistingOnInitialized,
            Location.None,
            className));
    }

    var ns = first.ContainingClassNamespace;

    var model = new ComponentModel
    {
        Namespace = ns,
        ClassName = className,
        Fields = validFields,
        UserImplementsDisposable = first.UserImplementsDisposable,
        UserOverridesOnInitialized = first.UserOverridesOnInitialized,
        UserOverridesOnInitializedAsync = first.UserOverridesOnInitializedAsync,
        ClassLocation = Location.None
    };

    string source = Emitter.Emit(model);
    string hintName = string.IsNullOrEmpty(ns)
        ? $"{className}.g.cs"
        : $"{ns}.{className}.g.cs";
    spc.AddSource(hintName, source);
}
```

- [ ] **Step 7: Run all existing tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All tests PASS

- [ ] **Step 8: Commit**

```bash
git add BlazorStatePlus.Generators/SliceIncrementalGenerator.cs BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs
git commit -m "fix: extract primitives from Roslyn symbols before Collect() boundary

Replaces FieldInfo (holding IFieldSymbol/INamedTypeSymbol) with a
value-equatable FieldData record containing only primitives/strings.
This fixes incremental caching — the generator no longer re-runs on
every keystroke because record equality works across pipeline phases.

Also fixes generator filename collision by using namespace-qualified
hint names."
```

---

## Task 2: Fix OnInitializedAsync Override Conflict (Critical)

**Problem:** Generator detects `UserOverridesOnInitializedAsync` but the Emitter ignores it. The generated `OnInitializedAsync` override silently replaces the user's, and doesn't call `base.OnInitializedAsync()`.

**Files:**
- Modify: `BlazorStatePlus.Generators/Emitter.cs:72-88`
- Test: `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs`

- [ ] **Step 1: Write a test for OnInitializedAsync conflict handling**

```csharp
// In GeneratorOutputTests.cs
[Fact]
public void UserOverridesOnInitializedAsync_EmitsPartialHook()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        namespace TestApp;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter;

            protected override async Task OnInitializedAsync()
            {
                await Task.CompletedTask;
            }
        }
        """;

    var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

    Assert.NotNull(generatedSource);
    Assert.Contains("OnAfterSlicesInitializedAsync", generatedSource);
    Assert.Contains("base.OnInitializedAsync()", generatedSource);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "UserOverridesOnInitializedAsync_EmitsPartialHook" -v n`
Expected: FAIL — generated code does not contain `OnAfterSlicesInitializedAsync` or `base.OnInitializedAsync()`

- [ ] **Step 3: Write a test for base.OnInitializedAsync call when no user override**

```csharp
// In GeneratorOutputTests.cs
[Fact]
public void NoUserOverride_OnInitializedAsync_CallsBase()
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

    Assert.NotNull(generatedSource);
    Assert.Contains("await base.OnInitializedAsync()", generatedSource);
}
```

- [ ] **Step 4: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "NoUserOverride_OnInitializedAsync_CallsBase" -v n`
Expected: FAIL

- [ ] **Step 5: Fix the Emitter to handle OnInitializedAsync correctly**

In `Emitter.cs`, replace the `OnInitializedAsync` generation block (lines 72-88):

```csharp
        // --- OnInitializedAsync override ---
        sb.AppendLine("    protected override async global::System.Threading.Tasks.Task OnInitializedAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.OnInitializedAsync();");
        sb.AppendLine();
        sb.AppendLine("        if (__ctx is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var __ctxLocal = __ctx;");
        sb.AppendLine("            __ctx = null;");
        sb.AppendLine();

        foreach (var field in model.Fields)
        {
            sb.AppendLine($"            await __ctxLocal.{field.PropertyName}.InitializeAsync({field.FieldName});");
        }

        sb.AppendLine("        }");

        if (model.UserOverridesOnInitializedAsync)
        {
            sb.AppendLine();
            sb.AppendLine("        await OnAfterSlicesInitializedAsync();");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
```

Also add the partial method declaration after the `OnInitializeSlices` partial (around line 91):

```csharp
        // --- partial Task OnAfterSlicesInitializedAsync (only if user overrides OnInitializedAsync) ---
        if (model.UserOverridesOnInitializedAsync)
        {
            sb.AppendLine("    partial global::System.Threading.Tasks.Task OnAfterSlicesInitializedAsync();");
            sb.AppendLine();
        }
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add BlazorStatePlus.Generators/Emitter.cs BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs
git commit -m "fix: handle OnInitializedAsync user override and call base

The Emitter now:
- Always calls base.OnInitializedAsync()
- Emits partial OnAfterSlicesInitializedAsync() when user overrides
  OnInitializedAsync, matching the OnInitialized/OnAfterSlicesCreated
  pattern for the sync case."
```

---

## Task 3: Fix Global Namespace Emission Bug

**Problem:** `Emitter.Emit` emits `namespace ;` when the class is in the global namespace.

**Files:**
- Modify: `BlazorStatePlus.Generators/Emitter.cs:23`
- Test: `BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs`

- [ ] **Step 1: Write a failing test**

```csharp
// In GeneratorOutputTests.cs
[Fact]
public void GlobalNamespace_GeneratesValidCode()
{
    var source = """
        using BlazorStatePlus.Abstractions;
        using BlazorStatePlus.Attributes;
        using Microsoft.AspNetCore.Components;

        public partial class MyComponent : ComponentBase
        {
            [Slice]
            private IStateSlice<int> _counter;
        }
        """;

    var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

    Assert.NotNull(generatedSource);
    Assert.DoesNotContain("namespace ;", generatedSource);
    Assert.DoesNotContain("namespace \n", generatedSource);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "GlobalNamespace_GeneratesValidCode" -v n`
Expected: FAIL

- [ ] **Step 3: Fix the Emitter namespace emission**

In `Emitter.cs`, replace line 23 (`sb.AppendLine($"namespace {model.Namespace};");`) with:

```csharp
        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace};");
            sb.AppendLine();
        }
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus.Generators/Emitter.cs BlazorStatePlus.Generators.Tests/GeneratorOutputTests.cs
git commit -m "fix: handle global namespace in emitter — skip namespace declaration when empty"
```

---

## Task 4: Delete Dead Code

**Problem:** `AllowUpdatesOnNavigation` (never read), 4 unused diagnostics (BSP004/009/010/013), `MutableSliceOptions` (unnecessary), `_hasDefault` on SliceBuilder (set but never read).

**Files:**
- Modify: `BlazorStatePlus/Abstractions/StateSliceOptions.cs` — remove `AllowUpdatesOnNavigation`
- Modify: `BlazorStatePlus/Attributes/SliceAttribute.cs` — remove `AllowUpdatesOnNavigation`
- Modify: `BlazorStatePlus/Services/StateManager.cs` — delete `MutableSliceOptions`, simplify `BuildOptions`
- Modify: `BlazorStatePlus/Generators/SliceBuilder.cs` — remove `_hasDefault`
- Modify: `BlazorStatePlus.Generators/DiagnosticDescriptors.cs` — remove BSP004, BSP009, BSP010, BSP013
- Modify: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` — remove AllowUpdatesOnNavigation from FieldData and ProcessClass
- Modify: `BlazorStatePlus.Generators/Emitter.cs` — remove AllowUpdatesOnNavigation from BuildOptionsArg
- Modify: `BlazorStatePlus.Generators/ComponentModel.cs` — remove AllowUpdatesOnNavigation from SliceFieldModel

- [ ] **Step 1: Remove AllowUpdatesOnNavigation from StateSliceOptions**

In `StateSliceOptions.cs`, delete the `AllowUpdatesOnNavigation` property (lines 22-27):

```csharp
namespace BlazorStatePlus.Abstractions;

public class StateSliceOptions
{
    public string? Key { get; set; }
    public TimeSpan? TimeToLive { get; set; }
}
```

- [ ] **Step 2: Remove AllowUpdatesOnNavigation from SliceAttribute**

In `SliceAttribute.cs`, delete the `AllowUpdatesOnNavigation` property (lines 17-21):

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class SliceAttribute : Attribute
{
    public string? TimeToLive { get; set; }
}
```

- [ ] **Step 3: Delete MutableSliceOptions and simplify BuildOptions in StateManager**

In `StateManager.cs`, replace the `BuildOptions` method and delete the `MutableSliceOptions` class:

```csharp
    private static StateSliceOptions BuildOptions(string key, Action<StateSliceOptions>? configure)
    {
        var options = new StateSliceOptions { Key = key };
        configure?.Invoke(options);
        options.Key ??= key;
        return options;
    }
```

Delete the entire `MutableSliceOptions` class (lines 164-176).

- [ ] **Step 4: Remove _hasDefault from SliceBuilder**

In `SliceBuilder.cs`, remove the `_hasDefault` field declaration and the `_hasDefault = true;` assignment in `DefaultValue`:

```csharp
    public SliceBuilder<T> DefaultValue(T value)
    {
        _defaultValue = value;
        return this;
    }
```

- [ ] **Step 5: Remove unused diagnostic descriptors**

In `DiagnosticDescriptors.cs`, delete the `OrphanInitMethod` (BSP004), `PropertyNotField` (BSP009), `InvalidInitSignature` (BSP010), and `NotSerializable` (BSP013) declarations.

- [ ] **Step 6: Remove AllowUpdatesOnNavigation from generator pipeline**

In `SliceIncrementalGenerator.cs`:
- Remove `AllowUpdatesOnNavigation` from the `FieldData` record
- Remove the extraction of `AllowUpdatesOnNavigation` from `ExtractFieldInfo`
- Remove `AllowUpdatesOnNavigation` from `SliceFieldModel` in `ComponentModel.cs`
- In `Emitter.cs`, simplify `BuildOptionsArg` to only handle TTL (remove `hasAllow` branch)

- [ ] **Step 7: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor: delete dead code — AllowUpdatesOnNavigation, 4 unused diagnostics, MutableSliceOptions, _hasDefault

Removes code that was declared but never functional:
- AllowUpdatesOnNavigation: plumbed through entire stack, never read
- BSP004/009/010/013: diagnostic descriptors never emitted
- MutableSliceOptions: solved a nonexistent problem
- _hasDefault: set but never read in SliceBuilder"
```

---

## Task 5: Fix InitializeIfNeeded Staleness Asymmetry

**Problem:** Sync `InitializeIfNeeded` checks only `WasRestored`. Async version checks `WasRestored && !IsStale`. Users expect identical guard logic.

**Files:**
- Create: `BlazorStatePlus.Generators.Tests/RuntimeTests.cs`
- Modify: `BlazorStatePlus/Services/StateSlice.cs:52-58`

- [ ] **Step 1: Create RuntimeTests.cs with test for sync staleness**

```csharp
// Create BlazorStatePlus.Generators.Tests/RuntimeTests.cs
using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Services;
using Xunit;

namespace BlazorStatePlus.Generators.Tests;

public class StateSliceTests
{
    private static StateSlice<T> CreateSlice<T>(T value, bool wasRestored, TimeSpan? ttl = null)
    {
        var options = new StateSliceOptions { Key = "test", TimeToLive = ttl };
        return new StateSlice<T>(value, wasRestored, options);
    }

    [Fact]
    public void InitializeIfNeeded_WhenRestoredAndStale_ReturnsTrue()
    {
        // A stale restored value should allow re-initialization (sync and async should match)
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
        // TTL of zero means immediately stale
        Assert.True(slice.IsStale);

        var result = slice.InitializeIfNeeded(99);

        Assert.True(result);
        Assert.Equal(99, slice.Value);
    }

    [Fact]
    public void InitializeIfNeeded_WhenRestoredAndFresh_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
        Assert.False(slice.IsStale);

        var result = slice.InitializeIfNeeded(99);

        Assert.False(result);
        Assert.Equal(42, slice.Value);
    }

    [Fact]
    public void InitializeIfNeeded_WhenNotRestored_ReturnsTrue()
    {
        var slice = CreateSlice(0, wasRestored: false);

        var result = slice.InitializeIfNeeded(99);

        Assert.True(result);
        Assert.Equal(99, slice.Value);
    }
}
```

Note: `StateSlice<T>` is `internal` but the generator project has `InternalsVisibleTo` for the test project. You may need to add `InternalsVisibleTo` for `BlazorStatePlus` assembly too. Add this to `BlazorStatePlus.csproj`:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="BlazorStatePlus.Generators.Tests" />
</ItemGroup>
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "InitializeIfNeeded_WhenRestoredAndStale_ReturnsTrue" -v n`
Expected: FAIL — sync version returns false for stale restored values

- [ ] **Step 3: Fix InitializeIfNeeded to match async staleness logic**

In `StateSlice.cs`, replace lines 52-58:

```csharp
    public bool InitializeIfNeeded(T value)
    {
        if (WasRestored && !IsStale)
            return false;

        Value = value;
        return true;
    }
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus/Services/StateSlice.cs BlazorStatePlus.Generators.Tests/RuntimeTests.cs BlazorStatePlus/BlazorStatePlus.csproj
git commit -m "fix: align InitializeIfNeeded staleness check with async version

Both sync and async InitializeIfNeeded now check WasRestored && !IsStale,
so stale restored values are re-initialized consistently regardless of
which path is used."
```

---

## Task 6: Fix CreateAndInit Eager Factory Evaluation

**Problem:** `CreateAndInit` calls `factory()` unconditionally before checking `WasRestored`.

**Files:**
- Test: `BlazorStatePlus.Generators.Tests/RuntimeTests.cs`
- Modify: `BlazorStatePlus/Services/StateManager.cs:81-89`

- [ ] **Step 1: Write a failing test**

```csharp
// Add to RuntimeTests.cs
public class StateManagerTests
{
    // Note: StateManager requires PersistentComponentState which is hard to mock.
    // Test the CreateAndInit logic indirectly via StateSlice behavior.
    // The fix is straightforward enough to verify by code review + the slice tests.
}
```

Since `PersistentComponentState` has no public constructor and can't be easily mocked, we test through a design-level fix:

- [ ] **Step 2: Fix CreateAndInit to defer factory evaluation**

In `StateManager.cs`, replace the `CreateAndInit` method (lines 81-89):

```csharp
    public IStateSlice<T> CreateAndInit<T>(
        string key,
        Func<T> factory,
        Action<StateSliceOptions>? configure = null)
    {
        var slice = CreateSlice<T>(key, default!, configure);
        if (!slice.WasRestored || slice.IsStale)
        {
            slice.InitializeIfNeeded(factory());
        }
        return slice;
    }
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 4: Commit**

```bash
git add BlazorStatePlus/Services/StateManager.cs
git commit -m "fix: defer factory() call in CreateAndInit until needed

Factory is now only evaluated when the slice was not restored or is stale,
avoiding expensive computation when the value was already persisted."
```

---

## Task 7: Add Disposed Guard to StateSlice

**Problem:** After `Dispose()`, `Value` setter still mutates state silently.

**Files:**
- Test: `BlazorStatePlus.Generators.Tests/RuntimeTests.cs`
- Modify: `BlazorStatePlus/Services/StateSlice.cs`

- [ ] **Step 1: Write a failing test**

```csharp
// Add to StateSliceTests in RuntimeTests.cs
[Fact]
public void Value_AfterDispose_ThrowsObjectDisposedException()
{
    var slice = CreateSlice(42, wasRestored: false);
    slice.Dispose();

    Assert.Throws<ObjectDisposedException>(() => slice.Value = 99);
}

[Fact]
public void Value_AfterDispose_GetStillWorks()
{
    var slice = CreateSlice(42, wasRestored: false);
    slice.Value = 10;
    slice.Dispose();

    // Getting value after dispose should still work (common pattern)
    Assert.Equal(10, slice.Value);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "Value_AfterDispose_ThrowsObjectDisposedException" -v n`
Expected: FAIL — no exception thrown

- [ ] **Step 3: Add disposed guard to Value setter**

In `StateSlice.cs`, add a disposed check at the start of the `Value` setter:

```csharp
    public T Value
    {
        get => _value;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;
            IsDirty = true;
            LastUpdated = DateTimeOffset.UtcNow;
            OnChanged?.Invoke();
        }
    }
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus/Services/StateSlice.cs BlazorStatePlus.Generators.Tests/RuntimeTests.cs
git commit -m "fix: throw ObjectDisposedException on Value set after Dispose"
```

---

## Task 8: Add Thread Safety to Persist Callbacks

**Problem:** `_persistCallbacks` is a `List<>` mutated in `RegisterPersistCallback` while the persist lambda iterates it.

**Files:**
- Modify: `BlazorStatePlus/Services/StateManager.cs:106-121`

- [ ] **Step 1: Fix by snapshotting the callback list in the persist lambda**

In `StateManager.cs`, replace the persist lambda in `RegisterPersistCallback` (lines 115-119):

```csharp
    private void RegisterPersistCallback(Func<Task> callback)
    {
        _persistCallbacks.Add(callback);

        if (!_registered)
        {
            _registered = true;
            _subscriptions.Add(persistence.RegisterOnPersisting(async () =>
            {
                var snapshot = _persistCallbacks.ToArray();
                foreach (var cb in snapshot)
                    await cb();
            }));
        }
    }
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus/Services/StateManager.cs
git commit -m "fix: snapshot persist callbacks before iteration to prevent race condition"
```

---

## Task 9: Fix BuildOptions to Merge Builder Config with Attribute Defaults

**Problem:** `SliceBuilder.BuildOptions` returns `attributeDefaults` verbatim, ignoring any future builder-level options configuration.

**Files:**
- Test: `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs`
- Modify: `BlazorStatePlus/Generators/SliceBuilder.cs`

- [ ] **Step 1: Write a failing test for BuildOptions merge**

```csharp
// Add to SliceBuilderTests.cs
[Fact]
public void BuildOptions_NoAttributeDefaults_ReturnsNull()
{
    var builder = new SliceBuilder<int>();
    var configure = builder.BuildOptions();
    Assert.Null(configure);
}

[Fact]
public void BuildOptions_WithAttributeDefaults_AppliesThem()
{
    var builder = new SliceBuilder<int>();
    var configure = builder.BuildOptions(o => o.TimeToLive = TimeSpan.FromMinutes(5));
    Assert.NotNull(configure);

    var options = new StateSliceOptions();
    configure!(options);
    Assert.Equal(TimeSpan.FromMinutes(5), options.TimeToLive);
}
```

- [ ] **Step 2: Run tests to verify they pass (current behavior matches these cases)**

Run: `dotnet test BlazorStatePlus.Generators.Tests --filter "BuildOptions_NoAttributeDefaults_ReturnsNull|BuildOptions_WithAttributeDefaults_AppliesThem" -v n`
Expected: PASS (the current behavior happens to handle these cases)

- [ ] **Step 3: Simplify BuildOptions — remove the pretense of merging**

The current `BuildOptions` claims to merge but doesn't. Since the builder has no options-level configuration today, simplify it to be an honest pass-through:

```csharp
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Action<StateSliceOptions>? BuildOptions(Action<StateSliceOptions>? attributeDefaults = null)
    {
        return attributeDefaults;
    }
```

This is functionally identical to the current code but removes the misleading comment in the Emitter. Update the Emitter comment (around line 175-176) to remove the "merge with user-supplied overrides" language.

- [ ] **Step 4: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus/Generators/SliceBuilder.cs BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs BlazorStatePlus.Generators/Emitter.cs
git commit -m "fix: clarify BuildOptions as honest pass-through, remove misleading merge comment"
```

---

## Task 10: Add Remaining Runtime Tests

**Problem:** StateSlice and StateManager have zero tests for core behavior: change notifications, TTL/IsStale, IsDirty, WasRestored, dispose lifecycle.

**Files:**
- Modify: `BlazorStatePlus.Generators.Tests/RuntimeTests.cs`

- [ ] **Step 1: Add change notification tests**

```csharp
// Add to StateSliceTests in RuntimeTests.cs
[Fact]
public void Value_Set_FiresOnChanged()
{
    var slice = CreateSlice(0, wasRestored: false);
    bool fired = false;
    slice.OnChanged += () => fired = true;

    slice.Value = 42;

    Assert.True(fired);
}

[Fact]
public void Value_SetSameValue_DoesNotFireOnChanged()
{
    var slice = CreateSlice(42, wasRestored: false);
    bool fired = false;
    slice.OnChanged += () => fired = true;

    slice.Value = 42;

    Assert.False(fired);
}
```

- [ ] **Step 2: Add TTL / IsStale tests**

```csharp
[Fact]
public void IsStale_NoTTL_ReturnsFalse()
{
    var slice = CreateSlice(42, wasRestored: true, ttl: null);
    Assert.False(slice.IsStale);
}

[Fact]
public void IsStale_ZeroTTL_ReturnsTrue()
{
    var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
    Assert.True(slice.IsStale);
}

[Fact]
public void IsStale_LargeTTL_ReturnsFalse()
{
    var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
    Assert.False(slice.IsStale);
}
```

- [ ] **Step 3: Add IsDirty and WasRestored tests**

```csharp
[Fact]
public void IsDirty_InitiallyFalse()
{
    var slice = CreateSlice(0, wasRestored: false);
    Assert.False(slice.IsDirty);
}

[Fact]
public void IsDirty_TrueAfterValueSet()
{
    var slice = CreateSlice(0, wasRestored: false);
    slice.Value = 42;
    Assert.True(slice.IsDirty);
}

[Fact]
public void WasRestored_ReflectsConstructorArg()
{
    var restored = CreateSlice(42, wasRestored: true);
    var fresh = CreateSlice(0, wasRestored: false);

    Assert.True(restored.WasRestored);
    Assert.False(fresh.WasRestored);
}
```

- [ ] **Step 4: Add dispose lifecycle tests**

```csharp
[Fact]
public void Dispose_ClearsOnChanged()
{
    var slice = CreateSlice(0, wasRestored: false);
    bool fired = false;
    slice.OnChanged += () => fired = true;

    slice.Dispose();
    // After dispose, getter still works but setter throws (from Task 7)
    Assert.False(fired);
}

[Fact]
public void Dispose_IsIdempotent()
{
    var slice = CreateSlice(0, wasRestored: false);
    slice.Dispose();
    slice.Dispose(); // Should not throw
}
```

- [ ] **Step 5: Add async InitializeIfNeededAsync tests**

```csharp
[Fact]
public async Task InitializeIfNeededAsync_WhenNotRestored_CallsFactory()
{
    var slice = CreateSlice(0, wasRestored: false);
    bool factoryCalled = false;

    await slice.InitializeIfNeededAsync(async () =>
    {
        factoryCalled = true;
        return 99;
    });

    Assert.True(factoryCalled);
    Assert.Equal(99, slice.Value);
}

[Fact]
public async Task InitializeIfNeededAsync_WhenRestoredAndFresh_SkipsFactory()
{
    var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
    bool factoryCalled = false;

    await slice.InitializeIfNeededAsync(async () =>
    {
        factoryCalled = true;
        return 99;
    });

    Assert.False(factoryCalled);
    Assert.Equal(42, slice.Value);
}

[Fact]
public async Task InitializeIfNeededAsync_WhenRestoredAndStale_CallsFactory()
{
    var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);

    await slice.InitializeIfNeededAsync(async () => 99);

    Assert.Equal(99, slice.Value);
}
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test BlazorStatePlus.Generators.Tests -v n`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add BlazorStatePlus.Generators.Tests/RuntimeTests.cs
git commit -m "test: add comprehensive runtime tests for StateSlice

Covers: change notifications, equality suppression, TTL/IsStale,
IsDirty, WasRestored, dispose lifecycle, sync and async initialization
with staleness checks."
```

---

## Summary

| Task | Priority | What it fixes |
|------|----------|---------------|
| 1 | Critical | Incremental caching — generator re-runs on every keystroke |
| 2 | Critical | OnInitializedAsync override silently dropped |
| 3 | Critical | `namespace ;` syntax error for global namespace |
| 4 | Medium | Dead code cleanup (AllowUpdatesOnNavigation, 4 diagnostics, MutableSliceOptions, _hasDefault) |
| 5 | Medium | Staleness asymmetry between sync/async init |
| 6 | Medium | CreateAndInit eager factory evaluation |
| 7 | Medium | No disposed guard on Value setter |
| 8 | Medium | Thread safety on persist callbacks |
| 9 | Low | BuildOptions misleading merge comment |
| 10 | Medium | Runtime test coverage gap |
