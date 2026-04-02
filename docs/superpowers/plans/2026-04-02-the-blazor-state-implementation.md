# TheBlazorState Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform BlazorStatePlus into TheBlazorState with property-based `[Persist]`/`[Shared]` API, pluggable storage strategies, and cross-component reactive state.

**Architecture:** Roslyn incremental source generator targeting `partial` properties (C# 13) instead of `IStateSlice<T>` fields. Two generators: one for `[Persist]` on component properties, one for `[Shared]` on state class properties. Storage abstraction layer with cascade resolution. Meta companion properties generated for all annotated properties.

**Tech Stack:** .NET 10, C# 13 (partial properties), Roslyn 4.12.0, ASP.NET Core Blazor, xUnit, Shouldly, bUnit

**Spec:** `docs/superpowers/specs/2026-04-02-the-blazor-state-design.md`

**Existing code:** The current BlazorStatePlus codebase in this repo will be transformed. Key files to understand before starting:
- Generator: `BlazorStatePlus.Generators/SliceIncrementalGenerator.cs` (456 lines)
- Emitter: `BlazorStatePlus.Generators/Emitter.cs` (200 lines)
- Runtime: `BlazorStatePlus/Services/StateManager.cs` (193 lines), `StateSlice.cs` (87 lines)
- Builder: `BlazorStatePlus/Builders/SliceBuilder.cs` (66 lines)

---

## Phase 1: Project Rename & Foundation

### Task 1: Rename Solution and Projects

Rename all project directories, namespaces, and references from `BlazorStatePlus` to `TheBlazorState`.

**Files:**
- Rename: `BlazorStatePlus/` → `TheBlazorState/`
- Rename: `BlazorStatePlus.Generators/` → `TheBlazorState.Generators/`
- Rename: `BlazorStatePlus.Tests/` → `TheBlazorState.Tests/`
- Rename: `BlazorStatePlus.Generators.Tests/` → `TheBlazorState.Generators.Tests/`
- Rename: `BlazorStatePlus.Demo/` → `TheBlazorState.Demo/`
- Modify: `BlazorStatePlus.slnx` → `TheBlazorState.slnx`
- Modify: All `.csproj` files (project references, RootNamespace)
- Modify: All `.cs` files (namespace declarations, using directives)
- Modify: All `.razor` files (using directives)

- [ ] **Step 1: Rename project directories**

```bash
cd C:/repo/POC/BlazorStatePlus
git mv BlazorStatePlus.slnx TheBlazorState.slnx
git mv BlazorStatePlus TheBlazorState
git mv BlazorStatePlus.Generators TheBlazorState.Generators
git mv BlazorStatePlus.Tests TheBlazorState.Tests
git mv BlazorStatePlus.Generators.Tests TheBlazorState.Generators.Tests
git mv BlazorStatePlus.Demo TheBlazorState.Demo
```

- [ ] **Step 2: Update solution file**

Replace content of `TheBlazorState.slnx`:

```xml
<Solution>
  <Configurations>
    <Platform Name="Any CPU" />
    <Platform Name="x64" />
    <Platform Name="x86" />
  </Configurations>
  <Folder Name="/src/">
    <Project Path="TheBlazorState/TheBlazorState.csproj" />
    <Project Path="TheBlazorState.Generators/TheBlazorState.Generators.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="TheBlazorState.Generators.Tests/TheBlazorState.Generators.Tests.csproj" />
    <Project Path="TheBlazorState.Tests/TheBlazorState.Tests.csproj" />
  </Folder>
  <Folder Name="/samples/">
    <Project Path="TheBlazorState.Demo/TheBlazorState.Demo.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 3: Update all .csproj files**

Rename the `.csproj` files themselves and update their content. In each file, replace:
- `BlazorStatePlus` → `TheBlazorState` in all `<RootNamespace>`, `<ProjectReference>`, `<InternalsVisibleTo>`, and `<Description>` elements.
- Update project reference paths to use new directory names.

Key changes per file:

`TheBlazorState/TheBlazorState.csproj`:
```xml
<RootNamespace>TheBlazorState</RootNamespace>
<Description>Ergonomic state management for Blazor with [Persist] and [Shared] attributes (.NET 10+)</Description>
...
<ProjectReference Include="..\TheBlazorState.Generators\TheBlazorState.Generators.csproj" ... />
...
<InternalsVisibleTo Include="TheBlazorState.Generators.Tests" />
<InternalsVisibleTo Include="TheBlazorState.Tests" />
```

`TheBlazorState.Generators/TheBlazorState.Generators.csproj`:
```xml
<RootNamespace>TheBlazorState.Generators</RootNamespace>
...
<InternalsVisibleTo Include="TheBlazorState.Generators.Tests" />
```

`TheBlazorState.Demo/TheBlazorState.Demo.csproj`:
```xml
<ProjectReference Include="..\TheBlazorState\TheBlazorState.csproj" />
<ProjectReference Include="..\TheBlazorState.Generators\TheBlazorState.Generators.csproj" ... />
```

`TheBlazorState.Tests/TheBlazorState.Tests.csproj`:
```xml
<ProjectReference Include="..\TheBlazorState\TheBlazorState.csproj" />
```

`TheBlazorState.Generators.Tests/TheBlazorState.Generators.Tests.csproj`:
```xml
<ProjectReference Include="..\TheBlazorState.Generators\TheBlazorState.Generators.csproj" />
<ProjectReference Include="..\TheBlazorState\TheBlazorState.csproj" />
```

- [ ] **Step 4: Find-and-replace namespaces across all .cs files**

In every `.cs` file across the solution, replace:
- `namespace BlazorStatePlus` → `namespace TheBlazorState`
- `using BlazorStatePlus` → `using TheBlazorState`
- `BlazorStatePlus.` → `TheBlazorState.` (in string literals, fully-qualified names)
- `global::BlazorStatePlus.` → `global::TheBlazorState.`

In every `.razor` file, replace:
- `@using BlazorStatePlus` → `@using TheBlazorState`

In `GlobalUsings.cs`:
- Update using directives to `TheBlazorState.*`

- [ ] **Step 5: Rename the .csproj files on disk**

```bash
cd C:/repo/POC/BlazorStatePlus
git mv TheBlazorState/BlazorStatePlus.csproj TheBlazorState/TheBlazorState.csproj
git mv TheBlazorState.Generators/BlazorStatePlus.Generators.csproj TheBlazorState.Generators/TheBlazorState.Generators.csproj
git mv TheBlazorState.Tests/BlazorStatePlus.Tests.csproj TheBlazorState.Tests/TheBlazorState.Tests.csproj
git mv TheBlazorState.Generators.Tests/BlazorStatePlus.Generators.Tests.csproj TheBlazorState.Generators.Tests/TheBlazorState.Generators.Tests.csproj
git mv TheBlazorState.Demo/BlazorStatePlus.Demo.csproj TheBlazorState.Demo/TheBlazorState.Demo.csproj
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build TheBlazorState.slnx
```

Expected: Build succeeds with the old API still intact, just renamed.

- [ ] **Step 7: Run tests**

```bash
dotnet test TheBlazorState.slnx
```

Expected: All existing tests pass.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor: rename BlazorStatePlus to TheBlazorState"
```

---

### Task 2: Create New Attributes

Replace `[Slice]` with `[Persist]` and add new `[Shared]` attribute. Keep `[Slice]` temporarily for migration (removed in Task 8).

**Files:**
- Create: `TheBlazorState/Attributes/PersistAttribute.cs`
- Create: `TheBlazorState/Attributes/SharedAttribute.cs`
- Test: `TheBlazorState.Generators.Tests/DiagnosticTests.cs` (update references)

- [ ] **Step 1: Write test for [Persist] attribute existence**

Add to `TheBlazorState.Generators.Tests/DiagnosticTests.cs` (or a new file `AttributeTests.cs`):

```csharp
using TheBlazorState.Attributes;
using Shouldly;

namespace TheBlazorState.Generators.Tests;

public class AttributeTests
{
    [Fact]
    public void PersistAttribute_Can_Be_Constructed()
    {
        var attr = new PersistAttribute();
        attr.TimeToLive.ShouldBeNull();
    }

    [Fact]
    public void PersistAttribute_Accepts_TimeToLive()
    {
        var attr = new PersistAttribute { TimeToLive = "00:05:00" };
        attr.TimeToLive.ShouldBe("00:05:00");
    }

    [Fact]
    public void SharedAttribute_Can_Be_Constructed()
    {
        var attr = new SharedAttribute();
        attr.ShouldNotBeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test TheBlazorState.Generators.Tests
```

Expected: Compilation error — `PersistAttribute` and `SharedAttribute` don't exist.

- [ ] **Step 3: Create PersistAttribute**

Create `TheBlazorState/Attributes/PersistAttribute.cs`:

```csharp
namespace TheBlazorState.Attributes;

/// <summary>
/// Marks a partial property for state persistence across prerender-to-interactive transitions.
/// The source generator emits the property backing implementation, a Meta companion,
/// and lifecycle wiring.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PersistAttribute : Attribute
{
    /// <summary>
    /// Optional time-to-live. If the persisted value is older than this duration,
    /// it is considered stale and the async factory (if any) will be re-invoked.
    /// Format: TimeSpan string (e.g., "00:05:00" for 5 minutes).
    /// </summary>
    public string? TimeToLive { get; set; }
}
```

- [ ] **Step 4: Create SharedAttribute**

Create `TheBlazorState/Attributes/SharedAttribute.cs`:

```csharp
namespace TheBlazorState.Attributes;

/// <summary>
/// Marks a partial property in a state class as reactive across components.
/// Any component injecting the state class re-renders when this property changes.
/// Can be combined with <see cref="PersistAttribute"/> for shared + persisted state.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SharedAttribute : Attribute
{
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test TheBlazorState.Generators.Tests
```

Expected: All 3 new attribute tests pass.

- [ ] **Step 6: Commit**

```bash
git add TheBlazorState/Attributes/PersistAttribute.cs TheBlazorState/Attributes/SharedAttribute.cs TheBlazorState.Generators.Tests/AttributeTests.cs
git commit -m "feat: add [Persist] and [Shared] attributes"
```

---

### Task 3: Create Storage Strategy Abstractions

Define `IStorageStrategy`, `StorageResult<T>`, `StorageMetadata`, and the `StorageStrategy` factory.

**Files:**
- Create: `TheBlazorState/Storage/IStorageStrategy.cs`
- Create: `TheBlazorState/Storage/StorageResult.cs`
- Create: `TheBlazorState/Storage/StorageMetadata.cs`
- Create: `TheBlazorState/Storage/StorageStrategy.cs`
- Test: `TheBlazorState.Tests/StorageStrategyTests.cs`

- [ ] **Step 1: Write tests for storage abstractions**

Create `TheBlazorState.Tests/StorageStrategyTests.cs`:

```csharp
using Shouldly;
using TheBlazorState.Storage;

namespace TheBlazorState.Tests;

public class StorageStrategyTests
{
    [Fact]
    public void StorageResult_Found_Contains_Value()
    {
        var result = new StorageResult<string>(true, "hello", DateTimeOffset.UtcNow);
        result.Found.ShouldBeTrue();
        result.Value.ShouldBe("hello");
        result.PersistedAt.ShouldNotBeNull();
    }

    [Fact]
    public void StorageResult_NotFound()
    {
        var result = new StorageResult<string>(false, null, null);
        result.Found.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void StorageMetadata_Captures_Key_And_TTL()
    {
        var meta = new StorageMetadata("MyComponent.Product", TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow);
        meta.Key.ShouldBe("MyComponent.Product");
        meta.TimeToLive.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void StorageStrategy_PrerenderHtml_Returns_Instance()
    {
        var strategy = StorageStrategy.PrerenderHtml();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_SessionStorage_Returns_Instance()
    {
        var strategy = StorageStrategy.SessionStorage();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_LocalStorage_Returns_Instance()
    {
        var strategy = StorageStrategy.LocalStorage();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_IndexedDb_Returns_Instance()
    {
        var strategy = StorageStrategy.IndexedDb();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_ServerMemoryCache_Returns_Instance()
    {
        var strategy = StorageStrategy.ServerMemoryCache();
        strategy.ShouldNotBeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test TheBlazorState.Tests
```

Expected: Compilation errors — storage types don't exist.

- [ ] **Step 3: Create IStorageStrategy**

Create `TheBlazorState/Storage/IStorageStrategy.cs`:

```csharp
namespace TheBlazorState.Storage;

/// <summary>
/// Abstraction for persisting and restoring state values.
/// Implement this interface to add custom storage backends (Redis, SQLite, etc.).
/// </summary>
public interface IStorageStrategy
{
    Task<StorageResult<T>> RestoreAsync<T>(string key);
    Task PersistAsync<T>(string key, T value, StorageMetadata metadata);
    Task RemoveAsync(string key);
}
```

- [ ] **Step 4: Create StorageResult and StorageMetadata**

Create `TheBlazorState/Storage/StorageResult.cs`:

```csharp
namespace TheBlazorState.Storage;

public record StorageResult<T>(bool Found, T? Value, DateTimeOffset? PersistedAt);
```

Create `TheBlazorState/Storage/StorageMetadata.cs`:

```csharp
namespace TheBlazorState.Storage;

public record StorageMetadata(string Key, TimeSpan? TimeToLive, DateTimeOffset Timestamp);
```

- [ ] **Step 5: Create StorageStrategy factory with stub implementations**

Create `TheBlazorState/Storage/StorageStrategy.cs`:

```csharp
namespace TheBlazorState.Storage;

/// <summary>
/// Factory for built-in storage strategies.
/// </summary>
public static class StorageStrategy
{
    public static IStorageStrategy PrerenderHtml() => PrerenderHtmlStrategy.Instance;
    public static IStorageStrategy ServerMemoryCache() => ServerMemoryCacheStrategy.Instance;
    public static IStorageStrategy SessionStorage() => SessionStorageStrategy.Instance;
    public static IStorageStrategy LocalStorage() => LocalStorageStrategy.Instance;
    public static IStorageStrategy IndexedDb() => IndexedDbStrategy.Instance;

    /// <summary>
    /// Returns a custom named strategy registered via AddTheBlazorState options.
    /// </summary>
    public static IStorageStrategy Custom(string name)
        => new CustomStrategyReference(name);
}

/// <summary>Marker for deferred resolution of custom strategies via DI.</summary>
internal sealed record CustomStrategyReference(string Name) : IStorageStrategy
{
    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => throw new InvalidOperationException($"Custom strategy '{Name}' must be resolved through DI.");
    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => throw new InvalidOperationException($"Custom strategy '{Name}' must be resolved through DI.");
    public Task RemoveAsync(string key)
        => throw new InvalidOperationException($"Custom strategy '{Name}' must be resolved through DI.");
}
```

- [ ] **Step 6: Create PrerenderHtmlStrategy**

Create `TheBlazorState/Storage/Strategies/PrerenderHtmlStrategy.cs`:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Storage;

/// <summary>
/// Persists state into the prerendered HTML via PersistentComponentState.
/// This is the default strategy. Restore/persist calls are wired by the generator
/// through StateManager, so this class delegates to PersistentComponentState directly.
/// </summary>
internal sealed class PrerenderHtmlStrategy : IStorageStrategy
{
    internal static readonly PrerenderHtmlStrategy Instance = new();

    public Task<StorageResult<T>> RestoreAsync<T>(string key)
    {
        // PrerenderHtml restore is handled by StateManager directly
        // because it needs access to PersistentComponentState which is scoped.
        // This strategy acts as a marker; actual I/O is in StateManager.
        return Task.FromResult(new StorageResult<T>(false, default, null));
    }

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
    {
        // Actual persistence handled by StateManager
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key) => Task.CompletedTask;
}
```

- [ ] **Step 7: Create stub strategies for browser storage**

These are stubs that will be implemented when JSInterop wiring is added. For now they throw `NotImplementedException` with a clear message.

Create `TheBlazorState/Storage/Strategies/SessionStorageStrategy.cs`:

```csharp
namespace TheBlazorState.Storage;

internal sealed class SessionStorageStrategy : IStorageStrategy
{
    internal static readonly SessionStorageStrategy Instance = new();

    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => throw new NotImplementedException("SessionStorage strategy requires browser JSInterop. Coming in a future release.");

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => throw new NotImplementedException("SessionStorage strategy requires browser JSInterop. Coming in a future release.");

    public Task RemoveAsync(string key)
        => throw new NotImplementedException("SessionStorage strategy requires browser JSInterop. Coming in a future release.");
}
```

Create `TheBlazorState/Storage/Strategies/LocalStorageStrategy.cs`:

```csharp
namespace TheBlazorState.Storage;

internal sealed class LocalStorageStrategy : IStorageStrategy
{
    internal static readonly LocalStorageStrategy Instance = new();

    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => throw new NotImplementedException("LocalStorage strategy requires browser JSInterop. Coming in a future release.");

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => throw new NotImplementedException("LocalStorage strategy requires browser JSInterop. Coming in a future release.");

    public Task RemoveAsync(string key)
        => throw new NotImplementedException("LocalStorage strategy requires browser JSInterop. Coming in a future release.");
}
```

Create `TheBlazorState/Storage/Strategies/IndexedDbStrategy.cs`:

```csharp
namespace TheBlazorState.Storage;

internal sealed class IndexedDbStrategy : IStorageStrategy
{
    internal static readonly IndexedDbStrategy Instance = new();

    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => throw new NotImplementedException("IndexedDb strategy requires browser JSInterop. Coming in a future release.");

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => throw new NotImplementedException("IndexedDb strategy requires browser JSInterop. Coming in a future release.");

    public Task RemoveAsync(string key)
        => throw new NotImplementedException("IndexedDb strategy requires browser JSInterop. Coming in a future release.");
}
```

Create `TheBlazorState/Storage/Strategies/ServerMemoryCacheStrategy.cs`:

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace TheBlazorState.Storage;

/// <summary>
/// Persists state in server-side IMemoryCache. Survives page reloads on Blazor Server.
/// </summary>
internal sealed class ServerMemoryCacheStrategy : IStorageStrategy
{
    internal static readonly ServerMemoryCacheStrategy Instance = new();

    // Actual cache is injected via StateManager at runtime.
    // This acts as a marker; real I/O is in StateManager.

    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => Task.FromResult(new StorageResult<T>(false, default, null));

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => Task.CompletedTask;

    public Task RemoveAsync(string key) => Task.CompletedTask;
}
```

- [ ] **Step 8: Run tests**

```bash
dotnet test TheBlazorState.Tests
```

Expected: All storage strategy tests pass.

- [ ] **Step 9: Commit**

```bash
git add TheBlazorState/Storage/ TheBlazorState.Tests/StorageStrategyTests.cs
git commit -m "feat: add IStorageStrategy abstraction and built-in strategies"
```

---

### Task 4: Create StateMeta and INotifyStateChanged

The Meta companion type and the interface for shared state change notification.

**Files:**
- Create: `TheBlazorState/Abstractions/StateMeta.cs`
- Create: `TheBlazorState/Abstractions/INotifyStateChanged.cs`
- Test: `TheBlazorState.Generators.Tests/StateMetaTests.cs`

- [ ] **Step 1: Write tests for StateMeta**

Create `TheBlazorState.Generators.Tests/StateMetaTests.cs`:

```csharp
using Shouldly;
using TheBlazorState.Abstractions;

namespace TheBlazorState.Generators.Tests;

public class StateMetaTests
{
    [Fact]
    public void StateMeta_Defaults()
    {
        var meta = new StateMeta(ttl: null);
        meta.WasRestored.ShouldBeFalse();
        meta.IsDirty.ShouldBeFalse();
        meta.IsStale.ShouldBeFalse();
        meta.LastUpdated.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    [Fact]
    public void StateMeta_WasRestored_When_Set()
    {
        var meta = new StateMeta(ttl: null);
        meta.MarkRestored(DateTimeOffset.UtcNow.AddMinutes(-1));
        meta.WasRestored.ShouldBeTrue();
    }

    [Fact]
    public void StateMeta_IsDirty_After_MarkDirty()
    {
        var meta = new StateMeta(ttl: null);
        meta.MarkDirty();
        meta.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void StateMeta_IsStale_When_TTL_Exceeded()
    {
        var meta = new StateMeta(ttl: TimeSpan.FromMinutes(5));
        meta.MarkRestored(DateTimeOffset.UtcNow.AddMinutes(-10));
        meta.IsStale.ShouldBeTrue();
    }

    [Fact]
    public void StateMeta_Not_Stale_When_TTL_Fresh()
    {
        var meta = new StateMeta(ttl: TimeSpan.FromMinutes(5));
        meta.MarkRestored(DateTimeOffset.UtcNow.AddMinutes(-1));
        meta.IsStale.ShouldBeFalse();
    }

    [Fact]
    public void StateMeta_Not_Stale_Without_TTL()
    {
        var meta = new StateMeta(ttl: null);
        meta.MarkRestored(DateTimeOffset.UtcNow.AddHours(-24));
        meta.IsStale.ShouldBeFalse();
    }

    [Fact]
    public void StateMeta_OnChanged_Fires()
    {
        var meta = new StateMeta(ttl: null);
        bool fired = false;
        meta.OnChanged += () => fired = true;
        meta.RaiseChanged();
        fired.ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test TheBlazorState.Generators.Tests
```

Expected: Compilation error — `StateMeta` doesn't exist.

- [ ] **Step 3: Create StateMeta**

Create `TheBlazorState/Abstractions/StateMeta.cs`:

```csharp
namespace TheBlazorState.Abstractions;

/// <summary>
/// Metadata companion for a [Persist] or [Shared] property.
/// Generated code creates one instance per annotated property.
/// </summary>
public sealed class StateMeta
{
    private readonly TimeSpan? _ttl;

    public StateMeta(TimeSpan? ttl)
    {
        _ttl = ttl;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>True if the value was restored from persistence.</summary>
    public bool WasRestored { get; private set; }

    /// <summary>True if the value has been modified since initialization.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>True if the value has exceeded its configured TTL.</summary>
    public bool IsStale =>
        _ttl.HasValue && DateTimeOffset.UtcNow - LastUpdated > _ttl.Value;

    /// <summary>UTC timestamp of last value change or restore.</summary>
    public DateTimeOffset LastUpdated { get; private set; }

    /// <summary>Fires whenever the property value changes.</summary>
    public event Action? OnChanged;

    internal void MarkRestored(DateTimeOffset persistedAt)
    {
        WasRestored = true;
        LastUpdated = persistedAt;
    }

    internal void MarkDirty()
    {
        IsDirty = true;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    internal void RaiseChanged() => OnChanged?.Invoke();

    internal void ClearHandlers() => OnChanged = null;
}
```

- [ ] **Step 4: Create INotifyStateChanged**

Create `TheBlazorState/Abstractions/INotifyStateChanged.cs`:

```csharp
namespace TheBlazorState.Abstractions;

/// <summary>
/// Implemented by shared state classes to notify consuming components of changes.
/// Generated by the source generator for classes with [Shared] properties.
/// </summary>
public interface INotifyStateChanged
{
    /// <summary>Fires when any [Shared] property on this state class changes.</summary>
    event Action? StateChanged;
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test TheBlazorState.Generators.Tests
```

Expected: All StateMetaTests pass.

- [ ] **Step 6: Commit**

```bash
git add TheBlazorState/Abstractions/StateMeta.cs TheBlazorState/Abstractions/INotifyStateChanged.cs TheBlazorState.Generators.Tests/StateMetaTests.cs
git commit -m "feat: add StateMeta companion and INotifyStateChanged interface"
```

---

### Task 5: Create StateContext (replaces SliceBuilder/SliceInitContext)

The new configuration surface used in `ConfigureState`.

**Files:**
- Create: `TheBlazorState/Configuration/StateContext.cs`
- Create: `TheBlazorState/Configuration/PropertyConfigurator.cs`
- Test: `TheBlazorState.Generators.Tests/StateContextTests.cs`

- [ ] **Step 1: Write tests for StateContext and PropertyConfigurator**

Create `TheBlazorState.Generators.Tests/StateContextTests.cs`:

```csharp
using Shouldly;
using TheBlazorState.Configuration;
using TheBlazorState.Storage;

namespace TheBlazorState.Generators.Tests;

public class StateContextTests
{
    [Fact]
    public void PropertyConfigurator_KeySuffix_Appends_To_BaseKey()
    {
        var config = new PropertyConfigurator<string>();
        config.KeySuffix(42);
        config.ResolveKey("Component.Name").ShouldBe("Component.Name:42");
    }

    [Fact]
    public void PropertyConfigurator_KeySuffix_Multiple_Parts()
    {
        var config = new PropertyConfigurator<string>();
        config.KeySuffix("us", 42);
        config.ResolveKey("Component.Name").ShouldBe("Component.Name:us:42");
    }

    [Fact]
    public void PropertyConfigurator_KeyOverride_Replaces_BaseKey()
    {
        var config = new PropertyConfigurator<string>();
        config.KeyOverride("custom-key");
        config.ResolveKey("Component.Name").ShouldBe("custom-key");
    }

    [Fact]
    public void PropertyConfigurator_LoadFrom_Sets_Factory()
    {
        var config = new PropertyConfigurator<string>();
        config.LoadFrom(() => Task.FromResult("loaded"));
        config.HasAsyncFactory.ShouldBeTrue();
    }

    [Fact]
    public void PropertyConfigurator_No_Factory_By_Default()
    {
        var config = new PropertyConfigurator<string>();
        config.HasAsyncFactory.ShouldBeFalse();
    }

    [Fact]
    public void PropertyConfigurator_Storage_Can_Be_Set()
    {
        var config = new PropertyConfigurator<string>();
        config.Storage = StorageStrategy.SessionStorage();
        config.Storage.ShouldNotBeNull();
    }

    [Fact]
    public void PropertyConfigurator_Storage_Null_By_Default()
    {
        var config = new PropertyConfigurator<string>();
        config.Storage.ShouldBeNull();
    }

    [Fact]
    public void StateContext_Storage_Can_Be_Set_At_Component_Level()
    {
        var ctx = new StateContext();
        ctx.Storage = StorageStrategy.LocalStorage();
        ctx.Storage.ShouldNotBeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test TheBlazorState.Generators.Tests
```

Expected: Compilation error — `StateContext` and `PropertyConfigurator` don't exist.

- [ ] **Step 3: Create PropertyConfigurator**

Create `TheBlazorState/Configuration/PropertyConfigurator.cs`:

```csharp
using System.ComponentModel;
using TheBlazorState.Storage;

namespace TheBlazorState.Configuration;

/// <summary>
/// Per-property configuration available in ConfigureState(StateContext ctx).
/// The source generator creates one of these per [Persist] or [Shared] property.
/// </summary>
public sealed class PropertyConfigurator<T>
{
    private object[]? _suffixParts;
    private string? _keyOverride;
    private Func<Task<T>>? _asyncFactory;

    /// <summary>Override the storage strategy for this specific property.</summary>
    public IStorageStrategy? Storage { get; set; }

    public PropertyConfigurator<T> KeySuffix(params object[] parts)
    {
        _suffixParts = parts;
        return this;
    }

    public PropertyConfigurator<T> KeyOverride(string key)
    {
        _keyOverride = key;
        return this;
    }

    /// <summary>
    /// Register an async factory to load this property's value.
    /// Called during OnInitializedAsync if the value was not restored (or is stale).
    /// </summary>
    public PropertyConfigurator<T> LoadFrom(Func<Task<T>> factory)
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
    public bool HasAsyncFactory => _asyncFactory is not null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<T> InvokeFactoryAsync() =>
        _asyncFactory is not null
            ? await _asyncFactory()
            : throw new InvalidOperationException("No async factory registered.");
}
```

- [ ] **Step 4: Create StateContext**

Create `TheBlazorState/Configuration/StateContext.cs`:

```csharp
using TheBlazorState.Storage;

namespace TheBlazorState.Configuration;

/// <summary>
/// Configuration context passed to ConfigureState(StateContext ctx).
/// Provides per-property configurators (generated as properties) and component-level settings.
/// This is a base class; the generator creates a nested subclass with typed property configurators.
/// </summary>
public class StateContext
{
    /// <summary>
    /// Component-level storage strategy override.
    /// Applied to all [Persist] properties that don't have their own storage set.
    /// </summary>
    public IStorageStrategy? Storage { get; set; }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test TheBlazorState.Generators.Tests
```

Expected: All StateContextTests pass.

- [ ] **Step 6: Commit**

```bash
git add TheBlazorState/Configuration/ TheBlazorState.Generators.Tests/StateContextTests.cs
git commit -m "feat: add StateContext and PropertyConfigurator for state configuration"
```

---

### Task 6: Create TheBlazorStateOptions and AddTheBlazorState

Registration entry point with global configuration.

**Files:**
- Create: `TheBlazorState/Configuration/TheBlazorStateOptions.cs`
- Modify: `TheBlazorState/Extensions/ServiceCollectionExtensions.cs`
- Test: `TheBlazorState.Tests/ServiceRegistrationTests.cs`

- [ ] **Step 1: Write tests**

Create `TheBlazorState.Tests/ServiceRegistrationTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TheBlazorState.Configuration;
using TheBlazorState.Extensions;
using TheBlazorState.Services;
using TheBlazorState.Storage;

namespace TheBlazorState.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddTheBlazorState_Registers_StateManager()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTheBlazorState();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<TheBlazorStateOptions>();
        options.ShouldNotBeNull();
    }

    [Fact]
    public void AddTheBlazorState_With_Options_Sets_DefaultStorage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTheBlazorState(opt =>
        {
            opt.DefaultStorage = StorageStrategy.LocalStorage();
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<TheBlazorStateOptions>();
        options.DefaultStorage.ShouldNotBeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test TheBlazorState.Tests
```

Expected: Compilation errors.

- [ ] **Step 3: Create TheBlazorStateOptions**

Create `TheBlazorState/Configuration/TheBlazorStateOptions.cs`:

```csharp
using TheBlazorState.Storage;

namespace TheBlazorState.Configuration;

/// <summary>
/// Global configuration for TheBlazorState, set in Program.cs via AddTheBlazorState().
/// </summary>
public sealed class TheBlazorStateOptions
{
    /// <summary>
    /// Default storage strategy used when no per-component or per-property strategy is set.
    /// Defaults to PrerenderHtml.
    /// </summary>
    public IStorageStrategy DefaultStorage { get; set; } = StorageStrategy.PrerenderHtml();

    private readonly Dictionary<string, IStorageStrategy> _customStrategies = new();

    /// <summary>Register a custom named storage strategy.</summary>
    public void AddStorage<TStrategy>(string name) where TStrategy : IStorageStrategy, new()
    {
        _customStrategies[name] = new TStrategy();
    }

    /// <summary>Register a custom named storage strategy instance.</summary>
    public void AddStorage(string name, IStorageStrategy strategy)
    {
        _customStrategies[name] = strategy;
    }

    internal IStorageStrategy? ResolveCustom(string name) =>
        _customStrategies.GetValueOrDefault(name);
}
```

- [ ] **Step 4: Update ServiceCollectionExtensions**

Replace the content of `TheBlazorState/Extensions/ServiceCollectionExtensions.cs`:

```csharp
using TheBlazorState.Configuration;
using TheBlazorState.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TheBlazorState.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers TheBlazorState services: StateManager, storage strategies, and options.
    /// </summary>
    public static IServiceCollection AddTheBlazorState(
        this IServiceCollection services,
        Action<TheBlazorStateOptions>? configure = null)
    {
        var options = new TheBlazorStateOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddScoped<StateManager>();
        return services;
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test TheBlazorState.Tests
```

Expected: ServiceRegistrationTests pass.

- [ ] **Step 6: Commit**

```bash
git add TheBlazorState/Configuration/TheBlazorStateOptions.cs TheBlazorState/Extensions/ServiceCollectionExtensions.cs TheBlazorState.Tests/ServiceRegistrationTests.cs
git commit -m "feat: add AddTheBlazorState() registration with global options"
```

---

## Phase 2: [Persist] Property Generator

### Task 7: Rewrite Generator for Partial Properties

Replace the field-based `[Slice]` generator with a property-based `[Persist]` generator. This is the largest task — it rewrites `SliceIncrementalGenerator.cs`, `Emitter.cs`, `ComponentModel.cs`, and `DiagnosticDescriptors.cs`.

**Files:**
- Modify: `TheBlazorState.Generators/SliceIncrementalGenerator.cs` → rename to `PersistIncrementalGenerator.cs`
- Modify: `TheBlazorState.Generators/ComponentModel.cs`
- Modify: `TheBlazorState.Generators/Emitter.cs` → rename to `PersistEmitter.cs`
- Modify: `TheBlazorState.Generators/DiagnosticDescriptors.cs`
- Modify: `TheBlazorState.Generators.Tests/DiagnosticTests.cs`
- Modify: `TheBlazorState.Generators.Tests/GeneratorOutputTests.cs`
- Modify: `TheBlazorState.Generators.Tests/TestHelper.cs`

**Important context:** The generator targets `netstandard2.0` and uses `Microsoft.CodeAnalysis.CSharp 4.12.0`. Partial properties (C# 13) are supported in this Roslyn version. The generator must:
1. Find properties with `[Persist]` attribute
2. Validate they are `partial`
3. Validate the containing class is `partial` (and inherits ComponentBase for components)
4. Emit: backing field, property implementation, Meta companion, lifecycle wiring

- [ ] **Step 1: Write diagnostic tests for new TBS codes**

Rewrite `TheBlazorState.Generators.Tests/DiagnosticTests.cs` to test the new TBS diagnostic codes:

```csharp
using Shouldly;

namespace TheBlazorState.Generators.Tests;

public class DiagnosticTests
{
    [Fact]
    public void TBS001_NonPartialProperty()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist]
    public int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS001");
    }

    [Fact]
    public void TBS002_NonPartialClass()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public class MyComponent : ComponentBase
{
    [Persist]
    public partial int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS002");
    }

    [Fact]
    public void TBS004_InvalidTimeToLive()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist(TimeToLive = ""not-a-timespan"")]
    public partial int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS004");
    }

    [Fact]
    public void TBS005_ConfigureState_References_Unknown_Property()
    {
        // This diagnostic requires detecting ConfigureState usage — defer to a later task
        // For now, just verify other diagnostics work
    }

    [Fact]
    public void No_Diagnostic_On_Valid_Persist_Property()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist]
    public partial int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Update DiagnosticDescriptors for TBS codes**

Replace content of `TheBlazorState.Generators/DiagnosticDescriptors.cs`:

```csharp
using Microsoft.CodeAnalysis;

namespace TheBlazorState.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "TheBlazorState";

    public static readonly DiagnosticDescriptor NonPartialProperty = new(
        "TBS001",
        "[Persist] or [Shared] requires partial property",
        "Property '{0}' has [{1}] but is not declared as partial",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonPartialClass = new(
        "TBS002",
        "[Persist] or [Shared] requires partial class",
        "Property '{0}' has [{1}] but class '{2}' is not partial",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PersistWithoutSharedOnStateClass = new(
        "TBS003",
        "[Persist] without [Shared] on state class property",
        "Property '{0}' has [Persist] but not [Shared]; did you mean to add [Shared]?",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTimeToLive = new(
        "TBS004",
        "Invalid TimeToLive format",
        "TimeToLive '{0}' on property '{1}' is not a valid TimeSpan string",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConfigureStateUnknownProperty = new(
        "TBS005",
        "ConfigureState references unknown property",
        "ConfigureState references '{0}' but no [Persist] or [Shared] property with that name exists",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
```

- [ ] **Step 3: Update ComponentModel for property-based generation**

Replace content of `TheBlazorState.Generators/ComponentModel.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TheBlazorState.Generators;

internal sealed record ComponentModel
{
    public string Namespace { get; init; } = null!;
    public string ClassName { get; init; } = null!;
    public List<PersistPropertyModel> Properties { get; init; } = null!;
    public bool UserImplementsDisposable { get; init; }
    public bool UserOverridesOnInitialized { get; init; }
    public bool UserOverridesOnInitializedAsync { get; init; }
}

internal sealed record PersistPropertyModel
{
    public string PropertyName { get; init; } = null!;
    public string FullTypeName { get; init; } = null!;
    public string? TimeToLive { get; init; }
    public string BaseKey { get; init; } = null!;
    public bool HasParameterAttribute { get; init; }
    public Location PropertyLocation { get; init; } = null!;
}
```

- [ ] **Step 4: Rewrite the generator**

Rename `SliceIncrementalGenerator.cs` → `PersistIncrementalGenerator.cs` and replace content:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TheBlazorState.Generators;

[Generator]
internal sealed class PersistIncrementalGenerator : IIncrementalGenerator
{
    private const string PersistAttributeFullName = "TheBlazorState.Attributes.PersistAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var propertyInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
            PersistAttributeFullName,
            predicate: static (node, _) => node is PropertyDeclarationSyntax,
            transform: static (ctx, ct) => ExtractPropertyInfo(ctx, ct))
            .Where(static x => x != null);

        var collected = propertyInfos.Collect();
        context.RegisterSourceOutput(collected, Execute);
    }

    private static (PropertyData Data, Location Location)? ExtractPropertyInfo(
        GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetNode is not PropertyDeclarationSyntax propertySyntax)
            return null;

        if (ctx.TargetSymbol is not IPropertySymbol propertySymbol)
            return null;

        var containingType = propertySymbol.ContainingType;
        if (containingType == null)
            return null;

        // Check if property is partial
        bool isPartialProperty = false;
        foreach (var modifier in propertySyntax.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword))
            {
                isPartialProperty = true;
                break;
            }
        }

        // Check if class is partial
        bool isPartialClass = false;
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax(ct);
            if (syntax is ClassDeclarationSyntax classDecl)
            {
                foreach (var modifier in classDecl.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.PartialKeyword))
                    {
                        isPartialClass = true;
                        break;
                    }
                }
            }
            if (isPartialClass) break;
        }

        // Check ComponentBase inheritance
        bool inheritsFromComponentBase = false;
        var current = containingType.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == "Microsoft.AspNetCore.Components.ComponentBase")
            {
                inheritsFromComponentBase = true;
                break;
            }
            current = current.BaseType;
        }

        // Check [Parameter] attribute
        bool hasParameterAttribute = false;
        foreach (var attr in propertySymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.ParameterAttribute")
            {
                hasParameterAttribute = true;
                break;
            }
        }

        // Extract [Persist] attribute properties
        string? timeToLive = null;
        foreach (var attr in ctx.Attributes)
        {
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg is { Key: "TimeToLive", Value.Value: string ttl })
                    timeToLive = ttl;
            }
        }

        // Check user IDisposable
        bool userImplementsDisposable = false;
        bool userOverridesOnInitialized = false;
        bool userOverridesOnInitializedAsync = false;
        foreach (var member in containingType.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                if (method is { Name: "Dispose", Parameters.Length: 0, IsAbstract: false })
                    userImplementsDisposable = true;
                if (method is { Name: "OnInitialized", Parameters.Length: 0, IsOverride: true })
                    userOverridesOnInitialized = true;
                if (method is { Name: "OnInitializedAsync", Parameters.Length: 0, IsOverride: true })
                    userOverridesOnInitializedAsync = true;
            }
        }

        string containingClassName = containingType.Name;
        string containingClassNamespace = containingType.ContainingNamespace.IsGlobalNamespace
            ? ""
            : containingType.ContainingNamespace.ToDisplayString();

        string propertyTypeFqn = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string baseKey = containingClassName + "." + propertySymbol.Name;

        var data = new PropertyData(
            PropertyName: propertySymbol.Name,
            ContainingClassName: containingClassName,
            ContainingClassNamespace: containingClassNamespace,
            PropertyTypeFullyQualified: propertyTypeFqn,
            IsPartialProperty: isPartialProperty,
            IsPartialClass: isPartialClass,
            InheritsFromComponentBase: inheritsFromComponentBase,
            HasParameterAttribute: hasParameterAttribute,
            TimeToLive: timeToLive,
            BaseKey: baseKey,
            UserImplementsDisposable: userImplementsDisposable,
            UserOverridesOnInitialized: userOverridesOnInitialized,
            UserOverridesOnInitializedAsync: userOverridesOnInitializedAsync);

        var location = propertySymbol.Locations.FirstOrDefault() ?? Location.None;
        return (data, location);
    }

    private static void Execute(SourceProductionContext spc,
        ImmutableArray<(PropertyData Data, Location Location)?> properties)
    {
        if (properties.IsDefaultOrEmpty)
            return;

        var grouped = new Dictionary<(string, string), List<(PropertyData Data, Location Location)>>();
        foreach (var prop in properties)
        {
            if (prop == null) continue;
            var key = (prop.Value.Data.ContainingClassName, prop.Value.Data.ContainingClassNamespace);
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<(PropertyData, Location)>();
                grouped[key] = list;
            }
            list.Add(prop.Value);
        }

        foreach (var kvp in grouped)
            ProcessClass(spc, kvp.Value);
    }

    private static void ProcessClass(SourceProductionContext spc,
        List<(PropertyData Data, Location Location)> properties)
    {
        var first = properties[0];
        var className = first.Data.ContainingClassName;
        var ns = first.Data.ContainingClassNamespace;

        // Validate partial property
        foreach (var prop in properties)
        {
            if (!prop.Data.IsPartialProperty)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonPartialProperty,
                    prop.Location,
                    prop.Data.PropertyName, "Persist"));
                return;
            }
        }

        // Validate partial class
        if (!first.Data.IsPartialClass)
        {
            foreach (var prop in properties)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonPartialClass,
                    prop.Location,
                    prop.Data.PropertyName, "Persist", className));
            }
            return;
        }

        // Validate ComponentBase (for now — [Shared] on non-component classes handled by SharedGenerator)
        if (!first.Data.InheritsFromComponentBase)
            return;

        // Validate TTL format
        var validProperties = new List<PersistPropertyModel>();
        foreach (var prop in properties)
        {
            if (prop.Data.TimeToLive != null && !TimeSpan.TryParse(prop.Data.TimeToLive, out _))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidTimeToLive,
                    prop.Location,
                    prop.Data.TimeToLive, prop.Data.PropertyName));
                continue;
            }

            validProperties.Add(new PersistPropertyModel
            {
                PropertyName = prop.Data.PropertyName,
                FullTypeName = prop.Data.PropertyTypeFullyQualified,
                TimeToLive = prop.Data.TimeToLive,
                BaseKey = prop.Data.BaseKey,
                HasParameterAttribute = prop.Data.HasParameterAttribute,
                PropertyLocation = prop.Location
            });
        }

        if (validProperties.Count == 0)
            return;

        var model = new ComponentModel
        {
            Namespace = ns,
            ClassName = className,
            Properties = validProperties,
            UserImplementsDisposable = first.Data.UserImplementsDisposable,
            UserOverridesOnInitialized = first.Data.UserOverridesOnInitialized,
            UserOverridesOnInitializedAsync = first.Data.UserOverridesOnInitializedAsync,
        };

        string source = PersistEmitter.Emit(model);
        string hintName = string.IsNullOrEmpty(ns)
            ? $"{className}.g.cs"
            : $"{ns}.{className}.g.cs";
        spc.AddSource(hintName, source);
    }

    private sealed record PropertyData(
        string PropertyName,
        string ContainingClassName,
        string ContainingClassNamespace,
        string PropertyTypeFullyQualified,
        bool IsPartialProperty,
        bool IsPartialClass,
        bool InheritsFromComponentBase,
        bool HasParameterAttribute,
        string? TimeToLive,
        string BaseKey,
        bool UserImplementsDisposable,
        bool UserOverridesOnInitialized,
        bool UserOverridesOnInitializedAsync);
}
```

- [ ] **Step 5: Rewrite the emitter**

Rename `Emitter.cs` → `PersistEmitter.cs` and replace content:

```csharp
using System;
using System.Text;

namespace TheBlazorState.Generators;

internal static class PersistEmitter
{
    public static string Emit(ComponentModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.ComponentModel;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.AspNetCore.Components;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace};");
            sb.AppendLine();
        }

        if (model.UserImplementsDisposable)
            sb.AppendLine($"partial class {model.ClassName}");
        else
            sb.AppendLine($"partial class {model.ClassName} : global::System.IDisposable");

        sb.AppendLine("{");

        // --- Injected StateManager ---
        sb.AppendLine("    [global::Microsoft.AspNetCore.Components.Inject]");
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("    private global::TheBlazorState.Services.StateManager __stateManager { get; set; } = null!;");
        sb.AppendLine();

        // --- Backing fields, property implementations, and Meta companions ---
        foreach (var prop in model.Properties)
        {
            string backingField = $"__{prop.PropertyName}_backing";
            string metaField = $"__{prop.PropertyName}_meta";

            // Backing field
            sb.AppendLine($"    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"    private {prop.FullTypeName} {backingField} = default!;");
            sb.AppendLine();

            // Meta field
            sb.AppendLine($"    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"    private global::TheBlazorState.Abstractions.StateMeta {metaField} = null!;");
            sb.AppendLine();

            // Partial property implementation
            sb.AppendLine($"    public partial {prop.FullTypeName} {prop.PropertyName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => {backingField};");
            sb.AppendLine("        set");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (global::System.Collections.Generic.EqualityComparer<{prop.FullTypeName}>.Default.Equals({backingField}, value))");
            sb.AppendLine("                return;");
            sb.AppendLine($"            {backingField} = value;");
            sb.AppendLine($"            if ({metaField} is not null)");
            sb.AppendLine("            {");
            sb.AppendLine($"                {metaField}.MarkDirty();");
            sb.AppendLine($"                {metaField}.RaiseChanged();");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Meta companion property
            sb.AppendLine($"    public global::TheBlazorState.Abstractions.StateMeta {prop.PropertyName}Meta => {metaField};");
            sb.AppendLine();
        }

        // --- StateContext nested class ---
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine($"    public sealed class __StateContext : global::TheBlazorState.Configuration.StateContext");
        sb.AppendLine("    {");
        foreach (var prop in model.Properties)
        {
            sb.AppendLine($"        public global::TheBlazorState.Configuration.PropertyConfigurator<{prop.FullTypeName}> {prop.PropertyName} {{ get; }} = new();");
        }
        sb.AppendLine();
        sb.AppendLine("        public bool HasAsyncInit");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");
        if (model.Properties.Count == 0)
        {
            sb.AppendLine("                return false;");
        }
        else
        {
            sb.Append("                return ");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                if (i > 0) sb.Append(" || ");
                sb.Append($"{model.Properties[i].PropertyName}.HasAsyncFactory");
            }
            sb.AppendLine(";");
        }
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // --- Context field ---
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine($"    private {model.ClassName}.__StateContext? __stateCtx;");
        sb.AppendLine();

        // --- OnInitialized ---
        sb.AppendLine("    protected override void OnInitialized()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnInitialized();");
        sb.AppendLine();
        sb.AppendLine($"        var __ctx = new {model.ClassName}.__StateContext();");
        sb.AppendLine("        ConfigureState(__ctx);");
        sb.AppendLine();

        foreach (var prop in model.Properties)
        {
            string backingField = $"__{prop.PropertyName}_backing";
            string metaField = $"__{prop.PropertyName}_meta";
            string ttlArg = string.IsNullOrEmpty(prop.TimeToLive)
                ? "null"
                : $"global::System.TimeSpan.Parse(\"{EscapeString(prop.TimeToLive!)}\")";

            sb.AppendLine($"        {metaField} = new global::TheBlazorState.Abstractions.StateMeta({ttlArg});");
            sb.AppendLine($"        __stateManager.RestoreProperty<{prop.FullTypeName}>(");
            sb.AppendLine($"            __ctx.{prop.PropertyName}.ResolveKey(\"{EscapeString(prop.BaseKey)}\"),");
            sb.AppendLine($"            __ctx.{prop.PropertyName}.Storage ?? __ctx.Storage,");
            sb.AppendLine($"            {metaField},");
            sb.AppendLine($"            value => {backingField} = value);");
            sb.AppendLine($"        {metaField}.OnChanged += () => InvokeAsync(StateHasChanged);");
        }

        sb.AppendLine();
        sb.AppendLine("        __stateCtx = __ctx.HasAsyncInit ? __ctx : null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // --- OnInitializedAsync ---
        sb.AppendLine("    protected override async global::System.Threading.Tasks.Task OnInitializedAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.OnInitializedAsync();");
        sb.AppendLine();
        sb.AppendLine("        if (__stateCtx is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var __ctx = __stateCtx;");
        sb.AppendLine("            __stateCtx = null;");
        sb.AppendLine();

        foreach (var prop in model.Properties)
        {
            string backingField = $"__{prop.PropertyName}_backing";
            string metaField = $"__{prop.PropertyName}_meta";
            sb.AppendLine($"            if (__ctx.{prop.PropertyName}.HasAsyncFactory");
            sb.AppendLine($"                && (!{metaField}.WasRestored || {metaField}.IsStale))");
            sb.AppendLine("            {");
            sb.AppendLine($"                {backingField} = await __ctx.{prop.PropertyName}.InvokeFactoryAsync();");
            sb.AppendLine($"                {metaField}.MarkDirty();");
            sb.AppendLine("            }");
        }

        sb.AppendLine("        }");

        if (model.UserOverridesOnInitializedAsync)
        {
            sb.AppendLine();
            sb.AppendLine("        await OnAfterStateInitializedAsync();");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // --- ConfigureState partial ---
        sb.AppendLine($"    partial void ConfigureState({model.ClassName}.__StateContext ctx);");
        sb.AppendLine();

        if (model.UserOverridesOnInitializedAsync)
        {
            sb.AppendLine("    partial global::System.Threading.Tasks.Task OnAfterStateInitializedAsync();");
            sb.AppendLine();
        }

        // --- Dispose ---
        if (model.UserImplementsDisposable)
        {
            sb.AppendLine("    private void DisposeState()");
        }
        else
        {
            sb.AppendLine("    public void Dispose()");
        }
        sb.AppendLine("    {");
        foreach (var prop in model.Properties)
        {
            sb.AppendLine($"        __{prop.PropertyName}_meta?.ClearHandlers();");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
```

- [ ] **Step 6: Update TestHelper for new generator and attribute**

Update `TheBlazorState.Generators.Tests/TestHelper.cs` to:
- Reference `PersistIncrementalGenerator` instead of `SliceIncrementalGenerator`
- Include `TheBlazorState.Attributes.PersistAttribute` in the compilation references
- Use the new namespace references

The key changes are replacing `SliceIncrementalGenerator` with `PersistIncrementalGenerator` in `RunGeneratorInternal`, and ensuring the test compilation can resolve `PersistAttribute`.

- [ ] **Step 7: Update StateManager to support RestoreProperty**

Add a `RestoreProperty<T>` method to `TheBlazorState/Services/StateManager.cs` that the generated code calls. This method:
- Tries to restore from `PersistentComponentState`
- Falls back to `IMemoryCache`
- Calls the value setter with the restored value
- Updates the `StateMeta` if restored

```csharp
public void RestoreProperty<T>(
    string key,
    IStorageStrategy? storageOverride,
    StateMeta meta,
    Action<T> valueSetter,
    T defaultValue = default!)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    // Try prerender state
    if (persistence.TryTakeFromJson<PersistedEnvelope<T>>(key, out var envelope)
        && envelope is not null)
    {
        valueSetter(envelope.Value);
        meta.MarkRestored(envelope.PersistedAt);
        logger.LogDebug("Property '{Key}': restored from prerender", key);
        return;
    }

    // Try server cache
    if (cache.TryGetValue<PersistedEnvelope<T>>(key, out var cached) && cached is not null)
    {
        valueSetter(cached.Value);
        meta.MarkRestored(cached.PersistedAt);
        logger.LogDebug("Property '{Key}': restored from server cache", key);
        return;
    }

    // Use default
    valueSetter(defaultValue);
    logger.LogDebug("Property '{Key}': no persisted value, using default", key);
}
```

Also register a persist callback per property that serializes the current value.

- [ ] **Step 8: Build and verify**

```bash
dotnet build TheBlazorState.slnx
```

Expected: Build succeeds.

- [ ] **Step 9: Run tests**

```bash
dotnet test TheBlazorState.slnx
```

Expected: All new diagnostic tests and existing tests pass. Some old tests that relied on `[Slice]` will need updating or removal — update `GeneratorOutputTests.cs` to use `[Persist]` on partial properties instead.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat: rewrite generator for [Persist] on partial properties"
```

---

### Task 8: Remove Old [Slice] API

Delete all remnants of the old Slice-based API.

**Files:**
- Delete: `TheBlazorState/Attributes/SliceAttribute.cs`
- Delete: `TheBlazorState/Abstractions/IStateSlice.cs`
- Delete: `TheBlazorState/Abstractions/StateSliceOptions.cs`
- Delete: `TheBlazorState/Services/StateSlice.cs`
- Delete: `TheBlazorState/Builders/SliceBuilder.cs`
- Modify: `TheBlazorState/GlobalUsings.cs` (remove SliceAttribute reference)
- Modify: Tests that referenced old types

- [ ] **Step 1: Delete old files**

```bash
cd C:/repo/POC/BlazorStatePlus
rm TheBlazorState/Attributes/SliceAttribute.cs
rm TheBlazorState/Abstractions/IStateSlice.cs
rm TheBlazorState/Abstractions/StateSliceOptions.cs
rm TheBlazorState/Services/StateSlice.cs
rm TheBlazorState/Builders/SliceBuilder.cs
```

- [ ] **Step 2: Update GlobalUsings.cs**

Remove any `using` that references `SliceAttribute` or old namespaces.

- [ ] **Step 3: Remove or rewrite old tests**

- `SliceBuilderTests.cs` — delete (replaced by StateContextTests)
- `RuntimeTests.cs` — delete (StateSlice<T> no longer exists; StateMeta tests replace it)
- Update any remaining test references

- [ ] **Step 4: Build and verify**

```bash
dotnet build TheBlazorState.slnx
```

Expected: Clean build with no references to old Slice types.

- [ ] **Step 5: Run tests**

```bash
dotnet test TheBlazorState.slnx
```

Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: remove old [Slice]/IStateSlice API"
```

---

## Phase 3: [Shared] State Generator

### Task 9: Create SharedIncrementalGenerator

A second generator that targets `[Shared]` properties on non-ComponentBase classes. It generates property implementations with change notification and `INotifyStateChanged`.

**Files:**
- Create: `TheBlazorState.Generators/SharedIncrementalGenerator.cs`
- Create: `TheBlazorState.Generators/SharedEmitter.cs`
- Create: `TheBlazorState.Generators/SharedStateModel.cs`
- Test: `TheBlazorState.Generators.Tests/SharedGeneratorTests.cs`

- [ ] **Step 1: Write tests for [Shared] generator output**

Create `TheBlazorState.Generators.Tests/SharedGeneratorTests.cs`:

```csharp
using Shouldly;

namespace TheBlazorState.Generators.Tests;

public class SharedGeneratorTests
{
    [Fact]
    public void Generates_Backing_Field_And_Property_For_Shared()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial decimal Total { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        diagnostics.ShouldBeEmpty();
        generatedSource.ShouldContain("public partial decimal Total");
        generatedSource.ShouldContain("__Total_backing");
        generatedSource.ShouldContain("TotalMeta");
        generatedSource.ShouldContain("INotifyStateChanged");
    }

    [Fact]
    public void Generates_StateChanged_Event()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial int Count { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldContain("event Action? StateChanged");
    }

    [Fact]
    public void TBS001_On_NonPartial_Shared_Property()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public int Count { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS001");
    }

    [Fact]
    public void TBS002_On_NonPartial_Class()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public class CartState
{
    [Shared]
    public partial int Count { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS002");
    }
}
```

- [ ] **Step 2: Create SharedStateModel**

Create `TheBlazorState.Generators/SharedStateModel.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TheBlazorState.Generators;

internal sealed record SharedStateModel
{
    public string Namespace { get; init; } = null!;
    public string ClassName { get; init; } = null!;
    public List<SharedPropertyModel> Properties { get; init; } = null!;
}

internal sealed record SharedPropertyModel
{
    public string PropertyName { get; init; } = null!;
    public string FullTypeName { get; init; } = null!;
    public bool HasPersistAttribute { get; init; }
    public string? TimeToLive { get; init; }
    public string BaseKey { get; init; } = null!;
    public Location PropertyLocation { get; init; } = null!;
}
```

- [ ] **Step 3: Create SharedIncrementalGenerator**

Create `TheBlazorState.Generators/SharedIncrementalGenerator.cs`:

This generator is structurally similar to `PersistIncrementalGenerator` but:
- Targets `SharedAttribute` instead of `PersistAttribute`
- Does NOT require `ComponentBase` inheritance
- Emits `INotifyStateChanged` implementation
- Emits `StateChanged` event that fires on any `[Shared]` property change
- Checks for `[Persist]` co-presence and sets `HasPersistAttribute` flag

Key differences in emitted code:
- No lifecycle methods (OnInitialized, OnInitializedAsync)
- No StateManager injection
- Implements `INotifyStateChanged`
- Property setter raises both `Meta.RaiseChanged()` and `StateChanged?.Invoke()`

- [ ] **Step 4: Create SharedEmitter**

Create `TheBlazorState.Generators/SharedEmitter.cs`:

The emitter generates:

```csharp
// <auto-generated/>
partial class CartState : INotifyStateChanged
{
    // Backing field + property implementation (same pattern as PersistEmitter)
    private decimal __Total_backing = default!;
    private StateMeta __Total_meta = new(null);

    public partial decimal Total
    {
        get => __Total_backing;
        set
        {
            if (EqualityComparer<decimal>.Default.Equals(__Total_backing, value))
                return;
            __Total_backing = value;
            __Total_meta.MarkDirty();
            __Total_meta.RaiseChanged();
            StateChanged?.Invoke();
        }
    }

    public StateMeta TotalMeta => __Total_meta;

    public event Action? StateChanged;
}
```

- [ ] **Step 5: Update TestHelper to run both generators**

Modify `TestHelper.RunGeneratorInternal` to run both `PersistIncrementalGenerator` and `SharedIncrementalGenerator`, or create a second helper for shared generator tests.

- [ ] **Step 6: Build and run tests**

```bash
dotnet test TheBlazorState.slnx
```

Expected: All shared generator tests pass.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add [Shared] source generator for reactive state classes"
```

---

### Task 10: Auto-Subscribe Components to Shared State

When a component injects a shared state class, it should automatically re-render on changes. This requires the generator to detect `[Inject]` properties of `INotifyStateChanged` types and wire up subscriptions.

**Files:**
- Modify: `TheBlazorState.Generators/PersistIncrementalGenerator.cs` (detect injected shared state)
- Modify: `TheBlazorState.Generators/PersistEmitter.cs` (emit subscription wiring)
- Test: `TheBlazorState.Generators.Tests/SharedSubscriptionTests.cs`

- [ ] **Step 1: Write test for auto-subscription**

Create `TheBlazorState.Generators.Tests/SharedSubscriptionTests.cs`:

```csharp
using Shouldly;

namespace TheBlazorState.Generators.Tests;

public class SharedSubscriptionTests
{
    [Fact]
    public void Component_With_Injected_SharedState_Gets_Subscription()
    {
        var source = @"
using TheBlazorState.Attributes;
using TheBlazorState.Abstractions;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class CartState : INotifyStateChanged
{
    public event Action? StateChanged;
    public decimal Total { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public CartState Cart { get; set; } = default!;

    [Persist]
    public partial int Counter { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        // The generated OnInitialized should subscribe to Cart.StateChanged
        generatedSource.ShouldContain("Cart.StateChanged");
        // Dispose should unsubscribe
        generatedSource.ShouldContain("Cart.StateChanged -=");
    }
}
```

- [ ] **Step 2: Implement detection and wiring**

In the `PersistIncrementalGenerator`, during `ExtractPropertyInfo`, also scan the containing class for `[Inject]` properties whose type implements `INotifyStateChanged`. Pass these as a list to the model.

In the emitter, for each injected shared state:
- In `OnInitialized`: `Cart.StateChanged += __OnSharedStateChanged;`
- Add method: `private void __OnSharedStateChanged() => InvokeAsync(StateHasChanged);`
- In `Dispose`: `Cart.StateChanged -= __OnSharedStateChanged;`

- [ ] **Step 3: Run tests**

```bash
dotnet test TheBlazorState.slnx
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: auto-subscribe components to injected shared state changes"
```

---

## Phase 4: Demo App & Integration

### Task 11: Rewrite Demo App

Update the demo to showcase the new API: `[Persist]`, `[Shared]`, `StateContext`, and `CartState`.

**Files:**
- Modify: `TheBlazorState.Demo/Program.cs`
- Modify: `TheBlazorState.Demo/Components/Pages/Counter.razor` + `.razor.cs`
- Modify: `TheBlazorState.Demo/Components/Pages/Weather.razor` + `.razor.cs`
- Modify: `TheBlazorState.Demo/Components/Pages/ProductDetail.razor` + `.razor.cs`
- Create: `TheBlazorState.Demo/State/CartState.cs`
- Modify: `TheBlazorState.Demo/Components/_Imports.razor`

- [ ] **Step 1: Create CartState shared state class**

Create `TheBlazorState.Demo/State/CartState.cs`:

```csharp
using TheBlazorState.Attributes;

namespace TheBlazorState.Demo.State;

public partial class CartState
{
    [Shared]
    public partial List<CartItem> Items { get; set; } = [];

    [Shared]
    public partial decimal Total { get; set; }

    public void AddItem(CartItem item)
    {
        Items = [..Items, item];
        Total = Items.Sum(i => i.Price * i.Quantity);
    }
}

public record CartItem(int ProductId, string Name, decimal Price, int Quantity);
```

- [ ] **Step 2: Update Program.cs**

```csharp
using TheBlazorState.Demo.Components;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddTheBlazorState();

builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<CartState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

- [ ] **Step 3: Rewrite Counter component**

`Counter.razor.cs`:
```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Counter : ComponentBase
{
    [Persist]
    public partial int CurrentCount { get; set; }

    partial void ConfigureState(Counter.__StateContext ctx)
    {
        // No special configuration — just persist across prerender
    }

    private void Increment() => CurrentCount++;
}
```

`Counter.razor`:
```razor
@page "/counter"
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @CurrentCount</p>

@if (CurrentCountMeta.WasRestored)
{
    <p><em>Restored from prerender (last updated @CurrentCountMeta.LastUpdated.LocalDateTime)</em></p>
}

<button class="btn btn-primary" @onclick="Increment">Click me</button>
```

- [ ] **Step 4: Rewrite Weather component**

`Weather.razor.cs`:
```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Services;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Weather : ComponentBase
{
    [Inject] private WeatherService WeatherSvc { get; set; } = default!;

    [Persist(TimeToLive = "00:05:00")]
    public partial WeatherForecast[]? Forecasts { get; set; }

    partial void ConfigureState(Weather.__StateContext ctx)
    {
        ctx.Forecasts.LoadFrom(() => WeatherSvc.GetForecastAsync());
    }

    private async Task Refresh()
    {
        Forecasts = await WeatherSvc.GetForecastAsync();
    }
}
```

`Weather.razor`: Update to use `ForecastsMeta.WasRestored`, `ForecastsMeta.IsStale`, etc.

- [ ] **Step 5: Rewrite ProductDetail component with CartState**

`ProductDetail.razor.cs`:
```csharp
using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;

namespace TheBlazorState.Demo.Components.Pages;

public partial class ProductDetail : ComponentBase
{
    [Parameter] public int ProductId { get; set; }

    [Inject] private ProductService ProductSvc { get; set; } = default!;
    [Inject] private ReviewService ReviewSvc { get; set; } = default!;
    [Inject] public CartState Cart { get; set; } = default!;

    [Persist(TimeToLive = "00:05:00")]
    public partial ProductPageState? PageState { get; set; }

    partial void ConfigureState(ProductDetail.__StateContext ctx)
    {
        ctx.PageState.LoadFrom(async () =>
        {
            var product = await ProductSvc.GetProductAsync(ProductId);
            var reviews = await ReviewSvc.GetReviewSummaryAsync(ProductId);
            return new ProductPageState(product, reviews, false);
        });
        ctx.PageState.KeySuffix(ProductId);
    }

    private void ToggleWishlist()
    {
        if (PageState is not null)
            PageState = PageState with { IsInWishlist = !PageState.IsInWishlist };
    }

    private void AddToCart()
    {
        if (PageState?.Product is not null)
            Cart.AddItem(new CartItem(
                PageState.Product.Id,
                PageState.Product.Name,
                PageState.Product.Price,
                1));
    }
}

public record ProductPageState(ProductDetailDto Product, ReviewSummary Reviews, bool IsInWishlist);
public record ProductDetailDto(int Id, string Name, string Description, decimal Price);
public record ReviewSummary(double AverageRating, int TotalReviews);
```

`ProductDetail.razor`: Update to use new API + show `Cart.Total`.

- [ ] **Step 6: Update _Imports.razor**

Add `@using TheBlazorState.Demo.State` and `@using TheBlazorState.Attributes`.

- [ ] **Step 7: Build and run demo**

```bash
dotnet build TheBlazorState.Demo
dotnet run --project TheBlazorState.Demo
```

Expected: App runs, counter persists across prerender, weather loads with TTL, product detail has cart integration.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: rewrite demo app with [Persist], [Shared], and CartState"
```

---

### Task 12: Update Integration Tests

Rewrite the integration and unit tests for the new API.

**Files:**
- Modify: `TheBlazorState.Tests/StateManagerTests.cs`
- Modify: `TheBlazorState.Tests/PrerenderIntegrationTests.cs`
- Modify: `TheBlazorState.Generators.Tests/GeneratorOutputTests.cs`

- [ ] **Step 1: Rewrite StateManagerTests**

Update to test the new `RestoreProperty<T>` method and persistence callbacks. The tests should verify:
- Restore from prerender state works
- Restore from cache fallback works
- TTL expiration during restore
- StateMeta is updated correctly on restore
- Persist callbacks serialize values correctly

- [ ] **Step 2: Rewrite GeneratorOutputTests**

Update to verify generated code for `[Persist]` on partial properties:
- Backing field generation
- Property implementation with equality check
- Meta companion generation
- OnInitialized wiring
- Dispose generation
- StateContext nested class

- [ ] **Step 3: Update PrerenderIntegrationTests**

Update to use new demo component names/routes. Verify prerender HTML still contains state markers.

- [ ] **Step 4: Run all tests**

```bash
dotnet test TheBlazorState.slnx
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: update all tests for TheBlazorState API"
```

---

### Task 13: Update README

Rewrite the library README to document the new API.

**Files:**
- Modify: `TheBlazorState/README.md`

- [ ] **Step 1: Rewrite README**

The README should cover:
1. What TheBlazorState does (one paragraph)
2. Quick start: `AddTheBlazorState()`, `[Persist]`, `[Shared]`
3. Before/after comparison (old Blazor boilerplate vs TheBlazorState)
4. `[Persist]` with examples: simple, TTL, async factory, `[Parameter]` combo
5. `[Shared]` with examples: CartState, injection, auto-rerender
6. `[Shared, Persist]` composition
7. Storage strategies table
8. `ConfigureState` reference
9. `Meta` companion reference
10. Diagnostics table (TBS001–TBS005)
11. Security note

- [ ] **Step 2: Commit**

```bash
git add TheBlazorState/README.md
git commit -m "docs: rewrite README for TheBlazorState API"
```

---

## Task Dependency Summary

```
Task 1 (rename) → Task 2 (attributes) → Task 3 (storage) → Task 4 (StateMeta)
    → Task 5 (StateContext) → Task 6 (options) → Task 7 (generator rewrite)
    → Task 8 (remove old API) → Task 9 (shared generator) → Task 10 (auto-subscribe)
    → Task 11 (demo) → Task 12 (tests) → Task 13 (README)
```

All tasks are sequential. Each produces a buildable, committable state.
