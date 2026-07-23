# BlazorStatePlus Source Generator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace manual plumbing with a `[Slice]` attribute and incremental source generator that auto-wires persistent state in Blazor components.

**Architecture:** An `IIncrementalGenerator` in a separate `netstandard2.0` project scans for `[Slice]`-annotated fields on partial `ComponentBase` classes and emits the other partial half with DI injection, slice creation, async init, and disposal. Runtime types (`SliceAttribute`, `SliceBuilder<T>`) live in the main library. The generator project ships inside the same NuGet package via `OutputItemType="Analyzer"`.

**Tech Stack:** C# 13 / .NET 10, Roslyn `IIncrementalGenerator`, `Microsoft.CodeAnalysis.CSharp` 4.x, xUnit + `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing` for generator tests.

**Spec:** `docs/superpowers/specs/2026-04-01-source-generator-design.md`

---

## File Map

### New files

| File | Responsibility |
|---|---|
| `BlazorStatePlus/Attributes/SliceAttribute.cs` | Marker attribute with optional `TimeToLive`, `AllowUpdatesOnNavigation` |
| `BlazorStatePlus/Generators/SliceBuilder.cs` | Runtime config holder — `KeySuffix`, `KeyOverride`, `DefaultValue`, `InitializeFrom` |
| `BlazorStatePlus.Generators/BlazorStatePlus.Generators.csproj` | Generator project (netstandard2.0, analyzer) |
| `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` | Entry point — syntax filter + semantic transform + emit |
| `BlazorStatePlus.Generators/ComponentModel.cs` | Parsed representation of a component with its slice fields |
| `BlazorStatePlus.Generators/Emitter.cs` | Code generation — builds the partial class source string |
| `BlazorStatePlus.Generators/DiagnosticDescriptors.cs` | BSP001–BSP013 diagnostic definitions |
| `BlazorStatePlus.Generators.Tests/BlazorStatePlus.Generators.Tests.csproj` | Test project |
| `BlazorStatePlus.Generators.Tests/GeneratorSnapshotTests.cs` | Snapshot tests for generated output |
| `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs` | Tests for each diagnostic |
| `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs` | Unit tests for SliceBuilder<T> |
| `BlazorStatePlus.Generators.Tests/Snapshots/*.verified.cs` | Golden files for snapshot verification |

### Modified files

| File | Change |
|---|---|
| `BlazorStatePlus.sln` | Add generator + test projects |
| `BlazorStatePlus/BlazorStatePlus.csproj` | Reference generator project as analyzer |
| `BlazorStatePlus/GlobalUsings.cs` | Export `SliceAttribute` namespace |
| `BlazorStatePlus/Examples/PersistentCounter.razor.cs` | Rewrite to use `[Slice]` |
| `BlazorStatePlus/Examples/ProductDetail.razor.cs` | Rewrite to use `[Slice]` |
| `BlazorStatePlus/Examples/WeatherDashboard.razor.cs` | Rewrite to use `[Slice]` |

### Removed files

| File | Reason |
|---|---|
| `BlazorStatePlus/Components/PersistentComponentBase.cs` | Replaced by generator |
| `BlazorStatePlus/Abstractions/IStateGroup.cs` | No longer needed |

### Removed methods (in kept files)

| File | Method | Reason |
|---|---|---|
| `BlazorStatePlus/Services/StateManager.cs` | `CreateGroup<TGroup>` | No separate group concept |

---

## Task 1: Create Generator Project Skeleton

**Files:**
- Create: `BlazorStatePlus.Generators/BlazorStatePlus.Generators.csproj`
- Modify: `BlazorStatePlus.sln`
- Modify: `BlazorStatePlus/BlazorStatePlus.csproj`

- [ ] **Step 1: Create the generator csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
    <RootNamespace>BlazorStatePlus.Generators</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Reference generator from main project**

Add to `BlazorStatePlus/BlazorStatePlus.csproj` inside `<ItemGroup>`:

```xml
<ProjectReference Include="..\BlazorStatePlus.Generators\BlazorStatePlus.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

- [ ] **Step 3: Add generator project to solution**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet sln add BlazorStatePlus.Generators/BlazorStatePlus.Generators.csproj
```
Expected: `Project added to the solution.`

- [ ] **Step 4: Verify solution builds**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build
```
Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus.Generators/BlazorStatePlus.Generators.csproj BlazorStatePlus/BlazorStatePlus.csproj BlazorStatePlus.sln
git commit -m "chore: add BlazorStatePlus.Generators project skeleton"
```

---

## Task 2: Create SliceAttribute

**Files:**
- Create: `BlazorStatePlus/Attributes/SliceAttribute.cs`
- Modify: `BlazorStatePlus/GlobalUsings.cs`

- [ ] **Step 1: Create SliceAttribute**

```csharp
using System;

namespace BlazorStatePlus.Attributes;

/// <summary>
/// Marks a field of type <c>IStateSlice&lt;T&gt;</c> for automatic wiring
/// by the BlazorStatePlus source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SliceAttribute : Attribute
{
    /// <summary>
    /// How long a restored value is considered fresh.
    /// Format: TimeSpan string (e.g., "00:05:00" for 5 minutes).
    /// Null means the value never goes stale.
    /// </summary>
    public string? TimeToLive { get; set; }

    /// <summary>
    /// If true, this slice accepts updated values during enhanced navigation.
    /// Default is false.
    /// </summary>
    public bool AllowUpdatesOnNavigation { get; set; }
}
```

- [ ] **Step 2: Export namespace in GlobalUsings**

Add to `BlazorStatePlus/GlobalUsings.cs`:

```csharp
global using BlazorStatePlus.Attributes;
```

- [ ] **Step 3: Verify build**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add BlazorStatePlus/Attributes/SliceAttribute.cs BlazorStatePlus/GlobalUsings.cs
git commit -m "feat: add SliceAttribute marker for source generator"
```

---

## Task 3: Create SliceBuilder\<T\>

**Files:**
- Create: `BlazorStatePlus/Generators/SliceBuilder.cs`
- Create: `BlazorStatePlus.Generators.Tests/BlazorStatePlus.Generators.Tests.csproj`
- Create: `BlazorStatePlus.Generators.Tests/SliceBuilderTests.cs`

- [ ] **Step 1: Create the test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing" Version="1.*" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing" Version="1.*" />
    <PackageReference Include="Verify.Xunit" Version="28.*" />
    <PackageReference Include="Verify.SourceGenerators" Version="2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BlazorStatePlus\BlazorStatePlus.csproj" />
    <ProjectReference Include="..\BlazorStatePlus.Generators\BlazorStatePlus.Generators.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="true" />
  </ItemGroup>

</Project>
```

Add to solution:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet sln add BlazorStatePlus.Generators.Tests/BlazorStatePlus.Generators.Tests.csproj
```

- [ ] **Step 2: Write the failing tests for SliceBuilder**

```csharp
using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Generators;

namespace BlazorStatePlus.Generators.Tests;

public class SliceBuilderTests
{
    [Fact]
    public void ResolveKey_NoOverride_ReturnsBaseKey()
    {
        var builder = new SliceBuilder<int>();
        Assert.Equal("Component.counter", builder.ResolveKey("Component.counter"));
    }

    [Fact]
    public void ResolveKey_WithSuffix_AppendsSeparatedByColon()
    {
        var builder = new SliceBuilder<int>();
        builder.KeySuffix(42);
        Assert.Equal("Component.counter:42", builder.ResolveKey("Component.counter"));
    }

    [Fact]
    public void ResolveKey_WithMultipleSuffixes_JoinsWithColon()
    {
        var builder = new SliceBuilder<string>();
        builder.KeySuffix(1, "en");
        Assert.Equal("Component.name:1:en", builder.ResolveKey("Component.name"));
    }

    [Fact]
    public void ResolveKey_WithOverride_IgnoresBaseKey()
    {
        var builder = new SliceBuilder<int>();
        builder.KeyOverride("custom-key");
        Assert.Equal("custom-key", builder.ResolveKey("Component.counter"));
    }

    [Fact]
    public void GetDefaultValue_ReturnsDefault_WhenNotSet()
    {
        var builder = new SliceBuilder<int>();
        Assert.Equal(0, builder.GetDefaultValue());
    }

    [Fact]
    public void GetDefaultValue_ReturnsSetValue()
    {
        var builder = new SliceBuilder<int>();
        builder.DefaultValue(42);
        Assert.Equal(42, builder.GetDefaultValue());
    }

    [Fact]
    public void HasAsyncFactory_FalseByDefault()
    {
        var builder = new SliceBuilder<int>();
        Assert.False(builder.HasAsyncFactory);
    }

    [Fact]
    public void HasAsyncFactory_TrueAfterInitializeFrom()
    {
        var builder = new SliceBuilder<int>();
        builder.InitializeFrom(() => Task.FromResult(99));
        Assert.True(builder.HasAsyncFactory);
    }

    [Fact]
    public void BuildOptions_AppliesAttributeDefaults()
    {
        var builder = new SliceBuilder<int>();
        var options = new StateSliceOptions();

        var configure = builder.BuildOptions(o => o.TimeToLive = TimeSpan.FromMinutes(5));
        configure?.Invoke(options);

        Assert.Equal(TimeSpan.FromMinutes(5), options.TimeToLive);
    }

    [Fact]
    public void FluentApi_ReturnsSameInstance()
    {
        var builder = new SliceBuilder<int>();
        var same = builder.KeySuffix(1).DefaultValue(0).KeyOverride("x");
        Assert.Same(builder, same);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test BlazorStatePlus.Generators.Tests --filter "SliceBuilderTests" -v minimal
```
Expected: FAIL — `SliceBuilder` type does not exist.

- [ ] **Step 4: Implement SliceBuilder\<T\>**

Create `BlazorStatePlus/Generators/SliceBuilder.cs`:

```csharp
using System.ComponentModel;
using BlazorStatePlus.Abstractions;

namespace BlazorStatePlus.Generators;

/// <summary>
/// Fluent configuration builder for a single <c>[Slice]</c> field.
/// Used by the generated <c>SliceInitContext</c> to collect runtime configuration
/// before slices are created.
/// </summary>
public sealed class SliceBuilder<T>
{
    private object[]? _suffixParts;
    private string? _keyOverride;
    private T _defaultValue = default!;
    private bool _hasDefault;
    private Func<Task<T>>? _asyncFactory;

    public SliceBuilder<T> KeySuffix(params object[] parts)
    {
        _suffixParts = parts;
        return this;
    }

    public SliceBuilder<T> KeyOverride(string key)
    {
        _keyOverride = key;
        return this;
    }

    public SliceBuilder<T> DefaultValue(T value)
    {
        _defaultValue = value;
        _hasDefault = true;
        return this;
    }

    public SliceBuilder<T> InitializeFrom(Func<Task<T>> factory)
    {
        _asyncFactory = factory;
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string ResolveKey(string baseKey)
    {
        if (_keyOverride is not null)
            return _keyOverride;

        if (_suffixParts is null or { Length: 0 })
            return baseKey;

        return baseKey + ":" + string.Join(":", _suffixParts);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public T GetDefaultValue() => _defaultValue;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool HasAsyncFactory => _asyncFactory is not null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Action<StateSliceOptions>? BuildOptions(Action<StateSliceOptions>? attributeDefaults = null)
    {
        if (attributeDefaults is null)
            return null;

        return attributeDefaults;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task InitializeAsync(IStateSlice<T> slice)
    {
        if (_asyncFactory is not null)
            await slice.InitializeIfNeededAsync(_asyncFactory);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test BlazorStatePlus.Generators.Tests --filter "SliceBuilderTests" -v minimal
```
Expected: All 10 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add BlazorStatePlus/Generators/SliceBuilder.cs BlazorStatePlus.Generators.Tests/ BlazorStatePlus.sln
git commit -m "feat: add SliceBuilder<T> runtime config holder with tests"
```

---

## Task 4: Create DiagnosticDescriptors

**Files:**
- Create: `BlazorStatePlus.Generators/DiagnosticDescriptors.cs`

- [ ] **Step 1: Create all diagnostic descriptors**

```csharp
using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "BlazorStatePlus";

    public static readonly DiagnosticDescriptor NonPartialClass = new(
        "BSP001",
        "[Slice] requires partial class",
        "Field '{0}' has [Slice] but class '{1}' is not partial",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFieldType = new(
        "BSP002",
        "[Slice] field must be IStateSlice<T>",
        "Field '{0}' has [Slice] but its type is not IStateSlice<T>",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotComponentBase = new(
        "BSP003",
        "[Slice] class must inherit ComponentBase",
        "Class '{0}' has [Slice] fields but does not inherit from ComponentBase",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OrphanInitMethod = new(
        "BSP004",
        "OnInitializeSlices without [Slice] fields",
        "Class '{0}' defines OnInitializeSlices but has no [Slice] fields",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTimeToLive = new(
        "BSP005",
        "Invalid TimeToLive format",
        "TimeToLive '{0}' on field '{1}' is not a valid TimeSpan string",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExistingDisposable = new(
        "BSP006",
        "Component already implements IDisposable",
        "Class '{0}' implements IDisposable; call __DisposeSlices() from your Dispose method",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateKey = new(
        "BSP007",
        "Duplicate slice key",
        "Fields '{0}' and '{1}' resolve to the same key '{2}'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticField = new(
        "BSP008",
        "[Slice] on static field",
        "Field '{0}' is static; [Slice] fields must be instance fields",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNotField = new(
        "BSP009",
        "[Slice] on property instead of field",
        "'{0}' is a property; [Slice] must be applied to fields",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidInitSignature = new(
        "BSP010",
        "OnInitializeSlices has wrong signature",
        "OnInitializeSlices in '{0}' must be 'partial void OnInitializeSlices(SliceInitContext ctx)'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExistingOnInitialized = new(
        "BSP011",
        "Component overrides OnInitialized",
        "Class '{0}' overrides OnInitialized; use OnAfterSlicesCreated() instead",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FieldHasInitializer = new(
        "BSP012",
        "[Slice] field has initializer",
        "Field '{0}' has an initializer that will be overwritten by the generator",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotSerializable = new(
        "BSP013",
        "Slice type not JSON-serializable",
        "Type '{0}' on field '{1}' may not be JSON-serializable",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
```

- [ ] **Step 2: Verify build**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build BlazorStatePlus.Generators
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus.Generators/DiagnosticDescriptors.cs
git commit -m "feat: add diagnostic descriptors BSP001-BSP013"
```

---

## Task 5: Create ComponentModel

**Files:**
- Create: `BlazorStatePlus.Generators/ComponentModel.cs`

- [ ] **Step 1: Create ComponentModel types**

```csharp
using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators;

/// <summary>
/// Parsed representation of a component class with [Slice] fields.
/// Built during the semantic transform phase, consumed by the emitter.
/// </summary>
internal sealed class ComponentModel
{
    public required string Namespace { get; init; }
    public required string ClassName { get; init; }
    public required List<SliceFieldModel> Fields { get; init; }
    public required bool UserImplementsDisposable { get; init; }
    public required bool UserOverridesOnInitialized { get; init; }
    public required bool UserOverridesOnInitializedAsync { get; init; }
    public required Location ClassLocation { get; init; }
}

internal sealed class SliceFieldModel
{
    public required string FieldName { get; init; }
    public required string PropertyName { get; init; }
    public required string TypeArgument { get; init; }
    public required string FullTypeArgument { get; init; }
    public required string? TimeToLive { get; init; }
    public required bool AllowUpdatesOnNavigation { get; init; }
    public required string BaseKey { get; init; }
    public required Location FieldLocation { get; init; }
}
```

`PropertyName` is the PascalCase name for the `SliceInitContext` property (e.g., `_page` -> `Page`, `_viewCount` -> `ViewCount`).

`BaseKey` is `"{ClassName}.{fieldNameWithoutUnderscore}"` (e.g., `"ProductDetail.page"`).

`TypeArgument` is the short name (e.g., `int`). `FullTypeArgument` is the fully qualified name (e.g., `BlazorStatePlus.Examples.ProductPageState`).

- [ ] **Step 2: Verify build**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build BlazorStatePlus.Generators
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus.Generators/ComponentModel.cs
git commit -m "feat: add ComponentModel for generator semantic model"
```

---

## Task 6: Create Emitter

**Files:**
- Create: `BlazorStatePlus.Generators/Emitter.cs`

- [ ] **Step 1: Implement the code emitter**

```csharp
using System.Text;

namespace BlazorStatePlus.Generators;

internal static class Emitter
{
    public static string Emit(ComponentModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.ComponentModel;");
        sb.AppendLine("using BlazorStatePlus.Abstractions;");
        sb.AppendLine("using BlazorStatePlus.Generators;");
        sb.AppendLine("using BlazorStatePlus.Services;");
        sb.AppendLine("using Microsoft.AspNetCore.Components;");
        sb.AppendLine();
        sb.AppendLine($"namespace {model.Namespace};");
        sb.AppendLine();

        // Open partial class — conditionally implement IDisposable
        if (model.UserImplementsDisposable)
            sb.AppendLine($"partial class {model.ClassName}");
        else
            sb.AppendLine($"partial class {model.ClassName} : global::System.IDisposable");

        sb.AppendLine("{");

        // Injected StateManager
        sb.AppendLine("    [global::Microsoft.AspNetCore.Components.InjectAttribute]");
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("    private global::BlazorStatePlus.Services.StateManager __sm { get; set; } = null!;");
        sb.AppendLine();

        // Stashed context for async init
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("    private SliceInitContext? __ctx;");
        sb.AppendLine();

        // OnInitialized override or OnAfterSlicesCreated hook
        EmitOnInitialized(sb, model);
        sb.AppendLine();

        // OnInitializedAsync override
        EmitOnInitializedAsync(sb, model);
        sb.AppendLine();

        // Partial method declaration
        sb.AppendLine("    partial void OnInitializeSlices(SliceInitContext ctx);");
        sb.AppendLine();

        // OnAfterSlicesCreated hook if user overrides OnInitialized
        if (model.UserOverridesOnInitialized)
        {
            sb.AppendLine("    partial void OnAfterSlicesCreated();");
            sb.AppendLine();
        }

        // Dispose or __DisposeSlices
        EmitDispose(sb, model);
        sb.AppendLine();

        // SliceInitContext nested class
        EmitSliceInitContext(sb, model);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void EmitOnInitialized(StringBuilder sb, ComponentModel model)
    {
        sb.AppendLine("    protected override void OnInitialized()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnInitialized();");
        sb.AppendLine();
        sb.AppendLine("        var ctx = new SliceInitContext();");
        sb.AppendLine("        OnInitializeSlices(ctx);");
        sb.AppendLine();

        foreach (var field in model.Fields)
        {
            sb.Append($"        {field.FieldName} = __sm.CreateSlice<{field.FullTypeArgument}>(");
            sb.AppendLine();
            sb.Append($"            ctx.{field.PropertyName}.ResolveKey(\"{field.BaseKey}\"),");
            sb.AppendLine();
            sb.Append($"            ctx.{field.PropertyName}.GetDefaultValue(),");
            sb.AppendLine();

            // Build options lambda if attribute has config
            if (field.TimeToLive is not null || field.AllowUpdatesOnNavigation)
            {
                sb.Append($"            ctx.{field.PropertyName}.BuildOptions(o =>");
                sb.AppendLine();
                sb.AppendLine("            {");

                if (field.TimeToLive is not null)
                    sb.AppendLine($"                o.TimeToLive = global::System.TimeSpan.Parse(\"{field.TimeToLive}\");");

                if (field.AllowUpdatesOnNavigation)
                    sb.AppendLine("                o.AllowUpdatesOnNavigation = true;");

                sb.AppendLine("            }));");
            }
            else
            {
                sb.AppendLine($"            ctx.{field.PropertyName}.BuildOptions());");
            }

            sb.AppendLine();
        }

        // Stash context if any async init might be needed
        sb.Append("        __ctx = ctx.HasAsyncInit ? ctx : null;");
        sb.AppendLine();

        if (model.UserOverridesOnInitialized)
        {
            sb.AppendLine();
            sb.AppendLine("        OnAfterSlicesCreated();");
        }

        sb.AppendLine("    }");
    }

    private static void EmitOnInitializedAsync(StringBuilder sb, ComponentModel model)
    {
        sb.AppendLine("    protected override async global::System.Threading.Tasks.Task OnInitializedAsync()");
        sb.AppendLine("    {");

        if (model.UserOverridesOnInitializedAsync)
            sb.AppendLine("        await base.OnInitializedAsync();");

        sb.AppendLine("        if (__ctx is null) return;");
        sb.AppendLine();

        foreach (var field in model.Fields)
        {
            sb.AppendLine($"        await __ctx.{field.PropertyName}.InitializeAsync({field.FieldName});");
        }

        sb.AppendLine();
        sb.AppendLine("        __ctx = null;");
        sb.AppendLine("    }");
    }

    private static void EmitDispose(StringBuilder sb, ComponentModel model)
    {
        var methodName = model.UserImplementsDisposable ? "__DisposeSlices" : "Dispose";
        var visibility = model.UserImplementsDisposable ? "private" : "public";

        sb.AppendLine($"    {visibility} void {methodName}()");
        sb.AppendLine("    {");

        foreach (var field in model.Fields)
        {
            sb.AppendLine($"        {field.FieldName}?.Dispose();");
        }

        sb.AppendLine("    }");
    }

    private static void EmitSliceInitContext(StringBuilder sb, ComponentModel model)
    {
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("    public sealed class SliceInitContext");
        sb.AppendLine("    {");

        foreach (var field in model.Fields)
        {
            sb.AppendLine($"        public global::BlazorStatePlus.Generators.SliceBuilder<{field.FullTypeArgument}> {field.PropertyName} {{ get; }} = new();");
        }

        sb.AppendLine();
        sb.Append("        public bool HasAsyncInit => ");

        var conditions = model.Fields
            .Select(f => $"{f.PropertyName}.HasAsyncFactory")
            .ToList();

        sb.Append(string.Join(" || ", conditions));
        sb.AppendLine(";");

        sb.AppendLine("    }");
    }
}
```

- [ ] **Step 2: Verify build**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build BlazorStatePlus.Generators
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus.Generators/Emitter.cs
git commit -m "feat: add Emitter for source generator code output"
```

---

## Task 7: Create SliceIncrementalGenerator

**Files:**
- Create: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs`

- [ ] **Step 1: Implement the incremental generator**

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazorStatePlus.Generators;

[Generator]
public class SliceIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Syntax filter — find fields with [Slice]
        var sliceFields = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "BlazorStatePlus.Attributes.SliceAttribute",
                predicate: static (node, _) => node is VariableDeclaratorSyntax,
                transform: static (ctx, ct) => GetFieldInfo(ctx, ct))
            .Where(static f => f is not null)
            .Select(static (f, _) => f!);

        // Step 2: Group fields by containing class
        var grouped = sliceFields.Collect();

        // Step 3: Combine with compilation for semantic analysis
        var compilationAndFields = context.CompilationProvider.Combine(grouped);

        // Step 4: Generate source
        context.RegisterSourceOutput(compilationAndFields,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static FieldCandidate? GetFieldInfo(
        GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IFieldSymbol fieldSymbol)
            return null;

        var attribute = ctx.Attributes.FirstOrDefault();
        if (attribute is null)
            return null;

        return new FieldCandidate
        {
            FieldSymbol = fieldSymbol,
            AttributeData = attribute,
            Location = ctx.TargetNode.GetLocation()
        };
    }

    private static void Execute(
        Compilation compilation,
        ImmutableArray<FieldCandidate> candidates,
        SourceProductionContext context)
    {
        if (candidates.IsDefaultOrEmpty)
            return;

        var componentBaseType = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Components.ComponentBase");
        var stateSliceType = compilation.GetTypeByMetadataName(
            "BlazorStatePlus.Abstractions.IStateSlice`1");
        var disposableType = compilation.GetTypeByMetadataName(
            "System.IDisposable");

        if (componentBaseType is null || stateSliceType is null)
            return;

        // Group by containing class
        var byClass = candidates.GroupBy(
            c => c.FieldSymbol.ContainingType,
            SymbolEqualityComparer.Default);

        foreach (var group in byClass)
        {
            var classSymbol = (INamedTypeSymbol)group.Key!;
            var fields = group.ToList();

            // Validate class-level constraints
            if (!ValidateClass(classSymbol, fields, componentBaseType, disposableType, context))
                continue;

            // Validate and build field models
            var fieldModels = new List<SliceFieldModel>();
            var hasErrors = false;

            foreach (var candidate in fields)
            {
                var fieldModel = ValidateAndBuildField(
                    candidate, stateSliceType, classSymbol, context);

                if (fieldModel is null)
                {
                    hasErrors = true;
                    continue;
                }

                fieldModels.Add(fieldModel);
            }

            if (hasErrors || fieldModels.Count == 0)
                continue;

            // Check for duplicate keys
            if (HasDuplicateKeys(fieldModels, context))
                continue;

            // Check if user overrides OnInitialized
            var userOverridesOnInit = classSymbol.GetMembers("OnInitialized")
                .OfType<IMethodSymbol>()
                .Any(m => m.IsOverride && m.Parameters.Length == 0);

            var userOverridesOnInitAsync = classSymbol.GetMembers("OnInitializedAsync")
                .OfType<IMethodSymbol>()
                .Any(m => m.IsOverride && m.Parameters.Length == 0);

            if (userOverridesOnInit)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ExistingOnInitialized,
                    classSymbol.Locations.FirstOrDefault(),
                    classSymbol.Name));
            }

            var userImplementsDisposable = classSymbol.AllInterfaces
                .Any(i => SymbolEqualityComparer.Default.Equals(i, disposableType));

            if (userImplementsDisposable)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ExistingDisposable,
                    classSymbol.Locations.FirstOrDefault(),
                    classSymbol.Name));
            }

            var model = new ComponentModel
            {
                Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
                ClassName = classSymbol.Name,
                Fields = fieldModels,
                UserImplementsDisposable = userImplementsDisposable,
                UserOverridesOnInitialized = userOverridesOnInit,
                UserOverridesOnInitializedAsync = userOverridesOnInitAsync,
                ClassLocation = classSymbol.Locations.FirstOrDefault()!
            };

            var source = Emitter.Emit(model);
            context.AddSource($"{model.ClassName}.g.cs", source);
        }
    }

    private static bool ValidateClass(
        INamedTypeSymbol classSymbol,
        List<FieldCandidate> fields,
        INamedTypeSymbol componentBaseType,
        INamedTypeSymbol? disposableType,
        SourceProductionContext context)
    {
        var valid = true;

        // BSP001: non-partial class
        if (!classSymbol.DeclaringSyntaxReferences
            .Any(r => r.GetSyntax() is ClassDeclarationSyntax cds
                && cds.Modifiers.Any(SyntaxKind.PartialKeyword)))
        {
            foreach (var f in fields)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonPartialClass,
                    f.Location,
                    f.FieldSymbol.Name,
                    classSymbol.Name));
            }
            valid = false;
        }

        // BSP003: not ComponentBase
        var inherits = InheritsFrom(classSymbol, componentBaseType);
        if (!inherits)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NotComponentBase,
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name));
            valid = false;
        }

        return valid;
    }

    private static SliceFieldModel? ValidateAndBuildField(
        FieldCandidate candidate,
        INamedTypeSymbol stateSliceType,
        INamedTypeSymbol classSymbol,
        SourceProductionContext context)
    {
        var field = candidate.FieldSymbol;

        // BSP008: static field
        if (field.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.StaticField,
                candidate.Location,
                field.Name));
            return null;
        }

        // BSP002: field type must be IStateSlice<T>
        if (field.Type is not INamedTypeSymbol namedType
            || !namedType.IsGenericType
            || namedType.OriginalDefinition.ToDisplayString() != "BlazorStatePlus.Abstractions.IStateSlice<T>")
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidFieldType,
                candidate.Location,
                field.Name));
            return null;
        }

        var typeArg = namedType.TypeArguments[0];

        // BSP005: validate TimeToLive format
        string? ttl = null;
        foreach (var kvp in candidate.AttributeData.NamedArguments)
        {
            if (kvp.Key == "TimeToLive" && kvp.Value.Value is string ttlStr)
            {
                if (!TimeSpan.TryParse(ttlStr, out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidTimeToLive,
                        candidate.Location,
                        ttlStr,
                        field.Name));
                    return null;
                }
                ttl = ttlStr;
            }
        }

        var allowUpdates = candidate.AttributeData.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "AllowUpdatesOnNavigation")
            .Value.Value is true;

        // BSP012: field has initializer
        var declarator = candidate.Location.SourceTree?.GetRoot()
            .FindNode(candidate.Location.SourceSpan);
        if (declarator is VariableDeclaratorSyntax vds && vds.Initializer is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FieldHasInitializer,
                candidate.Location,
                field.Name));
        }

        // Derive property name: _page -> Page, _viewCount -> ViewCount
        var rawName = field.Name.TrimStart('_');
        var propertyName = char.ToUpperInvariant(rawName[0]) + rawName.Substring(1);

        // Derive base key: "ClassName.fieldWithoutUnderscore"
        var baseKey = $"{classSymbol.Name}.{rawName}";

        return new SliceFieldModel
        {
            FieldName = field.Name,
            PropertyName = propertyName,
            TypeArgument = typeArg.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            FullTypeArgument = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            TimeToLive = ttl,
            AllowUpdatesOnNavigation = allowUpdates,
            BaseKey = baseKey,
            FieldLocation = candidate.Location
        };
    }

    private static bool HasDuplicateKeys(
        List<SliceFieldModel> fields, SourceProductionContext context)
    {
        var seen = new Dictionary<string, SliceFieldModel>();
        var hasDuplicates = false;

        foreach (var field in fields)
        {
            if (seen.TryGetValue(field.BaseKey, out var existing))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateKey,
                    field.FieldLocation,
                    existing.FieldName,
                    field.FieldName,
                    field.BaseKey));
                hasDuplicates = true;
            }
            else
            {
                seen[field.BaseKey] = field;
            }
        }

        return hasDuplicates;
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }
}

internal sealed class FieldCandidate
{
    public required IFieldSymbol FieldSymbol { get; init; }
    public required AttributeData AttributeData { get; init; }
    public required Location Location { get; init; }
}
```

- [ ] **Step 2: Verify build**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build BlazorStatePlus.Generators
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus.Generators/SliceIncrementalGenerator.cs
git commit -m "feat: add SliceIncrementalGenerator with validation and emission"
```

---

## Task 8: Snapshot Tests for Generated Output

**Files:**
- Create: `BlazorStatePlus.Generators.Tests/GeneratorSnapshotTests.cs`
- Create: `BlazorStatePlus.Generators.Tests/TestHelper.cs`

- [ ] **Step 1: Create test helper for running the generator**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BlazorStatePlus.Generators.Tests;

internal static class TestHelper
{
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Add BlazorStatePlus assembly reference
        references.Add(MetadataReference.CreateFromFile(
            typeof(BlazorStatePlus.Abstractions.IStateSlice<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(
            typeof(BlazorStatePlus.Attributes.SliceAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SliceIncrementalGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }

    public static ImmutableArray<Diagnostic> GetDiagnostics(string source)
    {
        var result = RunGenerator(source);
        return result.Diagnostics;
    }
}
```

- [ ] **Step 2: Write snapshot tests**

```csharp
using VerifyXunit;

namespace BlazorStatePlus.Generators.Tests;

[UsesVerify]
public class GeneratorSnapshotTests
{
    [Fact]
    public Task SimpleSlice_GeneratesCorrectOutput()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class Counter : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var result = TestHelper.RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task MultipleSlices_WithTTL_GeneratesCorrectOutput()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class Dashboard : ComponentBase
            {
                [Slice(TimeToLive = "00:05:00")]
                private IStateSlice<string[]> _items;

                [Slice(AllowUpdatesOnNavigation = true)]
                private IStateSlice<int> _selectedIndex;
            }
            """;

        var result = TestHelper.RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task UserImplementsDisposable_EmitsDisposeSlicesHelper()
    {
        var source = """
            using System;
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase, IDisposable
            {
                [Slice]
                private IStateSlice<int> _value;

                public void Dispose() { }
            }
            """;

        var result = TestHelper.RunGenerator(source);
        return Verify(result);
    }
}
```

- [ ] **Step 3: Run tests to generate initial snapshots**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test BlazorStatePlus.Generators.Tests --filter "GeneratorSnapshotTests" -v minimal
```

First run will fail because no `.verified.` files exist. Accept the snapshots:

```bash
cd /c/repo/POC/BlazorStatePlus && dotnet tool install --global verify.tool 2>/dev/null; verify accept --directory BlazorStatePlus.Generators.Tests
```

- [ ] **Step 4: Re-run tests to confirm snapshots pass**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test BlazorStatePlus.Generators.Tests --filter "GeneratorSnapshotTests" -v minimal
```
Expected: All 3 tests PASS.

- [ ] **Step 5: Review generated snapshots manually**

Read each `.verified.` file and confirm the output matches the expected generated code shape from the spec.

- [ ] **Step 6: Commit**

```bash
git add BlazorStatePlus.Generators.Tests/
git commit -m "test: add snapshot tests for source generator output"
```

---

## Task 9: Diagnostic Tests

**Files:**
- Create: `BlazorStatePlus.Generators.Tests/DiagnosticTests.cs`

- [ ] **Step 1: Write diagnostic tests**

```csharp
using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators.Tests;

public class DiagnosticTests
{
    [Fact]
    public void BSP001_NonPartialClass_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public class NotPartial : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP001");
    }

    [Fact]
    public void BSP002_WrongFieldType_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class BadType : ComponentBase
            {
                [Slice]
                private int _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP002");
    }

    [Fact]
    public void BSP003_NotComponentBase_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;

            namespace TestApp;

            public partial class NotAComponent
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP003");
    }

    [Fact]
    public void BSP005_InvalidTTL_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class BadTTL : ComponentBase
            {
                [Slice(TimeToLive = "not-a-timespan")]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP005");
    }

    [Fact]
    public void BSP008_StaticField_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class StaticSlice : ComponentBase
            {
                [Slice]
                private static IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP008");
    }

    [Fact]
    public void ValidComponent_NoDiagnostics()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class ValidComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
```

- [ ] **Step 2: Run diagnostic tests**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test BlazorStatePlus.Generators.Tests --filter "DiagnosticTests" -v minimal
```
Expected: All 6 tests PASS (the generator and diagnostics were already implemented in Task 7).

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus.Generators.Tests/DiagnosticTests.cs
git commit -m "test: add diagnostic tests for BSP001-BSP008"
```

---

## Task 10: Verify End-to-End with Main Project Build

**Files:**
- Modify: `BlazorStatePlus/Examples/PersistentCounter.razor.cs`
- Modify: `BlazorStatePlus/Examples/ProductDetail.razor.cs`
- Modify: `BlazorStatePlus/Examples/WeatherDashboard.razor.cs`

- [ ] **Step 1: Rewrite PersistentCounter to use [Slice]**

Replace the contents of `BlazorStatePlus/Examples/PersistentCounter.razor.cs`:

```csharp
using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;

namespace BlazorStatePlus.Examples;

public partial class PersistentCounter : ComponentBase
{
    [Slice]
    private IStateSlice<int> _counter;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Counter.DefaultValue(Random.Shared.Next(100));
    }

    private void Increment() => _counter.Value++;
}
```

- [ ] **Step 2: Rewrite ProductDetail to use [Slice]**

Replace the contents of `BlazorStatePlus/Examples/ProductDetail.razor.cs`:

```csharp
using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;

namespace BlazorStatePlus.Examples;

public partial class ProductDetail : ComponentBase
{
    [Inject] private ProductService Products { get; set; } = null!;
    [Inject] private ReviewService Reviews { get; set; } = null!;

    [Parameter]
    public int ProductId { get; set; }

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<ProductPageState> _page;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Page
           .KeySuffix(ProductId)
           .InitializeFrom(async () => new ProductPageState
           {
               Product = await Products.GetAsync(ProductId),
               Reviews = await Reviews.GetSummaryAsync(ProductId),
               IsInWishlist = await Products.IsInWishlistAsync(ProductId)
           });
    }

    private void ToggleWishlist()
    {
        var state = _page.Value;
        _page.Value = state with { IsInWishlist = !state.IsInWishlist };
    }

    public record ProductPageState
    {
        public ProductDetailDto? Product { get; init; }
        public ReviewSummary? Reviews { get; init; }
        public bool IsInWishlist { get; init; }
    }

    public record ProductDetailDto(int Id, string Name, string Description, decimal Price);
    public record ReviewSummary(int TotalCount, double AverageRating);
}
```

- [ ] **Step 3: Rewrite WeatherDashboard to use [Slice]**

Replace the contents of `BlazorStatePlus/Examples/WeatherDashboard.razor.cs`:

```csharp
using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;

namespace BlazorStatePlus.Examples;

public partial class WeatherDashboard : ComponentBase
{
    [Inject] private WeatherService Weather { get; set; } = null!;
    [Inject] private ILogger<WeatherDashboard> Logger { get; set; } = null!;

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<WeatherForecast[]?> _forecasts;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Forecasts
           .InitializeFrom(() => Weather.GetForecastAsync());
    }

    private async Task Refresh()
    {
        _forecasts.Value = await Weather.GetForecastAsync();
        Logger.LogInformation("Forecasts refreshed at {Time}", DateTimeOffset.UtcNow);
    }
}
```

- [ ] **Step 4: Build to verify generator works end-to-end**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build
```
Expected: Build succeeded. The generator produces `PersistentCounter.g.cs`, `ProductDetail.g.cs`, `WeatherDashboard.g.cs`.

- [ ] **Step 5: Commit**

```bash
git add BlazorStatePlus/Examples/
git commit -m "feat: rewrite examples to use [Slice] source generator"
```

---

## Task 11: Remove Old API

**Files:**
- Delete: `BlazorStatePlus/Components/PersistentComponentBase.cs`
- Delete: `BlazorStatePlus/Abstractions/IStateGroup.cs`
- Modify: `BlazorStatePlus/Services/StateManager.cs:80-87` (remove `CreateGroup`)

- [ ] **Step 1: Delete PersistentComponentBase**

Run:
```bash
rm "C:/repo/POC/BlazorStatePlus/BlazorStatePlus/Components/PersistentComponentBase.cs"
```

- [ ] **Step 2: Delete IStateGroup**

Run:
```bash
rm "C:/repo/POC/BlazorStatePlus/BlazorStatePlus/Abstractions/IStateGroup.cs"
```

- [ ] **Step 3: Remove CreateGroup from StateManager**

Remove lines 76–88 from `BlazorStatePlus/Services/StateManager.cs` (the `CreateGroup` method):

```csharp
    /// <summary>
    /// Persists and restores an entire <see cref="IStateGroup"/> as a single unit.
    /// Reduces the number of serialization keys and avoids partial-state issues.
    /// </summary>
    public IStateSlice<TGroup> CreateGroup<TGroup>(
        string key,
        TGroup? defaultValue = null,
        Action<StateSliceOptions>? configure = null)
        where TGroup : class, IStateGroup, new()
    {
        return CreateSlice(key, defaultValue ?? new TGroup(), configure);
    }
```

- [ ] **Step 4: Remove IStateGroup using from StateManager**

Remove the `using BlazorStatePlus.Abstractions;` line at the top of `StateManager.cs` only if no other types from that namespace are used. (Check: `StateSliceOptions` and `IStateSlice<T>` are in that namespace — keep the using.)

Actually `IStateGroup` is referenced in the `CreateGroup` constraint. After removing `CreateGroup`, the using is still needed for `IStateSlice<T>` and `StateSliceOptions`. No change needed.

- [ ] **Step 5: Remove Components directory if empty**

Run:
```bash
rmdir "C:/repo/POC/BlazorStatePlus/BlazorStatePlus/Components" 2>/dev/null; echo "done"
```

- [ ] **Step 6: Verify build**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 7: Run all tests**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test -v minimal
```
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor: remove PersistentComponentBase, IStateGroup, and CreateGroup"
```

---

## Task 12: Final Verification

- [ ] **Step 1: Clean build from scratch**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet clean && dotnet build
```
Expected: Build succeeded, 0 warnings (except intentional BSP006/011/012 on examples if applicable).

- [ ] **Step 2: Run all tests**

Run:
```bash
cd /c/repo/POC/BlazorStatePlus && dotnet test -v minimal
```
Expected: All tests pass.

- [ ] **Step 3: Verify generated files exist**

Run:
```bash
find "C:/repo/POC/BlazorStatePlus/BlazorStatePlus/obj" -name "*.g.cs" | head -20
```
Expected: See `PersistentCounter.g.cs`, `ProductDetail.g.cs`, `WeatherDashboard.g.cs` in the obj directory.

- [ ] **Step 4: Commit any remaining changes**

```bash
cd /c/repo/POC/BlazorStatePlus && git status
```

If clean, no commit needed. If any unstaged changes, investigate and commit.
