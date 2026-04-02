# Legends Review Persistence Fixes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address the top 5 action items from the Legends Review consensus report on persistence: add defensive guards to `CreateSlice`, rewrite both READMEs, document TTL semantics and fix the clock divergence, add a security note, and add an E2E integration test.

**Architecture:** Five independent tasks. Task 1 adds defensive guards (`_disposed`, null-key, deserialization resilience) to `StateManager.CreateSlice`. Task 2 rewrites `BlazorStatePlus/README.md` to match the current `[Slice]` + generator API. Task 3 rewrites `BlazorStatePlus.Generators/README.md`. Task 4 fixes the TTL clock divergence (initialize `LastUpdated = PersistedAt` for restored slices) and adds XML doc clarifications. Task 5 adds an E2E integration test using `WebApplicationFactory` to prove the prerender-to-interactive roundtrip.

**Tech Stack:** C# / .NET 10 / Blazor / xUnit / Shouldly / bUnit / `Microsoft.AspNetCore.Mvc.Testing`

---

### Task 1: Add defensive guards to `StateManager.CreateSlice`

**Files:**
- Modify: `BlazorStatePlus/Services/StateManager.cs:30-84`
- Modify: `BlazorStatePlus.Tests/StateManagerTests.cs`

- [ ] **Step 1: Write failing test — CreateSlice on disposed manager throws**

Add to `BlazorStatePlus.Tests/StateManagerTests.cs`:

```csharp
[Fact]
public void CreateSlice_AfterDispose_ThrowsObjectDisposedException()
{
    var manager = CreateManager();
    manager.Dispose();

    Should.Throw<ObjectDisposedException>(() => manager.CreateSlice<int>("counter"));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_AfterDispose_ThrowsObjectDisposedException" -v n`
Expected: FAIL — no exception thrown

- [ ] **Step 3: Add `_disposed` guard to `CreateSlice`**

In `BlazorStatePlus/Services/StateManager.cs`, add as the first line of `CreateSlice`:

```csharp
ObjectDisposedException.ThrowIf(_disposed, this);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_AfterDispose_ThrowsObjectDisposedException" -v n`
Expected: PASS

- [ ] **Step 5: Write failing test — CreateSlice with null key throws**

Add to `BlazorStatePlus.Tests/StateManagerTests.cs`:

```csharp
[Fact]
public void CreateSlice_NullKey_ThrowsArgumentException()
{
    using var manager = CreateManager();

    Should.Throw<ArgumentException>(() => manager.CreateSlice<int>(null!));
}

[Fact]
public void CreateSlice_EmptyKey_ThrowsArgumentException()
{
    using var manager = CreateManager();

    Should.Throw<ArgumentException>(() => manager.CreateSlice<int>(""));
}
```

- [ ] **Step 6: Run tests to verify they fail**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_NullKey|CreateSlice_EmptyKey" -v n`
Expected: FAIL — `ArgumentNullException` from `PersistentComponentState` or no exception

- [ ] **Step 7: Add null/empty key guard to `CreateSlice`**

In `BlazorStatePlus/Services/StateManager.cs`, add after the `_disposed` guard:

```csharp
ArgumentException.ThrowIfNullOrWhiteSpace(key);
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_NullKey|CreateSlice_EmptyKey" -v n`
Expected: PASS

- [ ] **Step 9: Write failing test — deserialization mismatch falls back to default**

This tests that if the persisted JSON shape doesn't match (schema drift between deploys), the slice gracefully falls back to the default value instead of crashing.

Add to `BlazorStatePlus.Tests/StateManagerTests.cs`:

```csharp
[Fact]
public void CreateSlice_DeserializationMismatch_FallsBackToDefault()
{
    // Persist an int envelope under a key, then try to restore as string
    FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
    {
        Value = 42,
        PersistedAt = DateTimeOffset.UtcNow
    });

    using var manager = CreateManager();
    // Restoring as string when an int was persisted — type mismatch
    var slice = manager.CreateSlice<string>("counter", defaultValue: "fallback");

    // Should fall back to default rather than crash
    slice.Value.ShouldBe("fallback");
    slice.WasRestored.ShouldBeFalse();
}
```

- [ ] **Step 10: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_DeserializationMismatch_FallsBackToDefault" -v n`
Expected: FAIL — may throw `JsonException` or `InvalidCastException`

- [ ] **Step 11: Wrap `TryTakeFromJson` in try-catch**

In `BlazorStatePlus/Services/StateManager.cs`, replace the `TryTakeFromJson` call and the surrounding restore logic (lines 37-62) with:

```csharp
T restoredValue;
bool effectivelyRestored;

try
{
    var wasRestored = persistence.TryTakeFromJson<PersistedEnvelope<T>>(
        options.Key!, out var envelope);

    if (wasRestored && envelope is not null)
    {
        if (options.TimeToLive.HasValue
            && DateTimeOffset.UtcNow - envelope.PersistedAt > options.TimeToLive.Value)
        {
            restoredValue = defaultValue;
            effectivelyRestored = false;
            logger.LogDebug("Slice '{Key}': restored value discarded (TTL expired)", options.Key);
        }
        else
        {
            restoredValue = envelope.Value;
            effectivelyRestored = true;
            logger.LogDebug("Slice '{Key}': restored from prerender", options.Key);
        }
    }
    else
    {
        restoredValue = defaultValue;
        effectivelyRestored = false;
        logger.LogDebug("Slice '{Key}': no persisted value, using default", options.Key);
    }
}
catch (Exception ex)
{
    restoredValue = defaultValue;
    effectivelyRestored = false;
    logger.LogWarning(ex, "Slice '{Key}': deserialization failed, using default", options.Key);
}
```

Also remove the separate logging block that was after the if/else (lines 64-69), since logging is now inline.

- [ ] **Step 12: Run all StateManager tests**

Run: `dotnet test BlazorStatePlus.Tests -v n`
Expected: ALL PASS

- [ ] **Step 13: Add security XML doc warning to `CreateSlice`**

Add to the XML doc on `CreateSlice` in `StateManager.cs`, inside the existing `<summary>` or as a `<remarks>`:

```csharp
/// <remarks>
/// <b>Security:</b> Slice values are serialized as JSON into the prerendered HTML response
/// and are visible in the page source. Do not store sensitive data (auth tokens, PII, secrets)
/// in state slices.
/// </remarks>
```

- [ ] **Step 14: Commit**

```bash
git add BlazorStatePlus/Services/StateManager.cs BlazorStatePlus.Tests/StateManagerTests.cs
git commit -m "fix: add defensive guards to CreateSlice (disposed, null-key, deserialization)"
```

---

### Task 2: Rewrite `BlazorStatePlus/README.md`

**Files:**
- Rewrite: `BlazorStatePlus/README.md`

The current README references `PersistentComponentBase`, `UseSlice`, `UseGroup`, `CreateAndInit`, `CreateAndInitAsync`, `CreateGroup`, `IStateGroup`, and `AllowUpdatesOnNavigation` — all of which have been deleted from the codebase. The README must be rewritten from scratch to match the current `[Slice]` + source generator API.

- [ ] **Step 1: Rewrite the README**

Replace the entire contents of `BlazorStatePlus/README.md` with:

```markdown
# BlazorStatePlus

Zero-boilerplate prerender-to-interactive state handoff for Blazor components (.NET 10+).

> **What this is:** A source-generator-powered library that wraps `PersistentComponentState` to eliminate the manual `TryTakeFromJson` / `RegisterOnPersisting` / `IDisposable` ceremony.
>
> **What this is NOT:** This library does **not** persist state across page refreshes, navigation, browser sessions, or server restarts. It handles the prerender-to-interactive handoff only — the moment between when the server renders HTML and when the interactive runtime takes over.

## Setup

```csharp
// Program.cs
builder.Services.AddBlazorStatePlus();
```

## Quick Comparison

### Before (raw Blazor API)

```csharp
@page "/weather"
@implements IDisposable
@inject PersistentComponentState ApplicationState

@code {
    private WeatherForecast[]? forecasts;
    private PersistingComponentStateSubscription sub;

    protected override async Task OnInitializedAsync()
    {
        if (!ApplicationState.TryTakeFromJson<WeatherForecast[]>(
                "forecasts", out var restored))
        {
            forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("/api/weather");
        }
        else
        {
            forecasts = restored;
        }

        sub = ApplicationState.RegisterOnPersisting(() =>
        {
            ApplicationState.PersistAsJson("forecasts", forecasts);
            return Task.CompletedTask;
        });
    }

    void IDisposable.Dispose() => sub.Dispose();
}
```

### After (with BlazorStatePlus)

```csharp
public partial class Weather : ComponentBase
{
    [Inject] private WeatherService WeatherSvc { get; set; } = null!;

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<WeatherForecast[]> _forecasts = null!;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Forecasts.InitializeFrom(() => WeatherSvc.GetForecastAsync());
    }
}
```

No `IDisposable`, no manual `TryTakeFromJson`, no `RegisterOnPersisting` callback. The source generator handles it all.

## How It Works

1. Mark fields with `[Slice]` on a `partial class` that extends `ComponentBase`
2. The source generator emits `OnInitialized` / `OnInitializedAsync` / `Dispose` overrides
3. On prerender: `StateManager` registers an `OnPersisting` callback that serializes each slice's value as JSON into the prerendered HTML
4. On interactive boot: `StateManager` calls `TryTakeFromJson` to restore each slice's value from the embedded JSON

## Core Concepts

### The `[Slice]` Attribute

Marks a field of type `IStateSlice<T>` for automatic wiring:

```csharp
public partial class Counter : ComponentBase
{
    [Slice]
    private IStateSlice<int> _counter = null!;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Counter.DefaultValue(Random.Shared.Next(100));
    }

    private void Increment() => _counter.Value++;
}
```

### Runtime Configuration via `OnInitializeSlices`

The generated `SliceInitContext` provides a fluent builder per field:

```csharp
partial void OnInitializeSlices(SliceInitContext ctx)
{
    ctx.Page
       .KeySuffix(ProductId)          // Dynamic key: "ProductDetail.page:42"
       .InitializeFrom(async () =>    // Async factory (skipped if restored)
           new ProductPageState
           {
               Product = await Products.GetAsync(ProductId),
               Reviews = await Reviews.GetSummaryAsync(ProductId)
           });
}
```

**Builder methods:**
| Method | Description |
|--------|-------------|
| `DefaultValue(T value)` | Fallback value when nothing is restored |
| `KeySuffix(params object[] parts)` | Append dynamic segments to the auto-derived key |
| `KeyOverride(string key)` | Replace the auto-derived key entirely |
| `InitializeFrom(Func<Task<T>> factory)` | Async factory called only when no restored value exists (or when stale) |

### Staleness / TTL

Flag state as stale after a duration. When the prerendered data is older than the TTL (e.g., cached by a CDN or delayed by a slow connection), the library falls back to the default and the `InitializeFrom` factory runs instead:

```csharp
[Slice(TimeToLive = "00:05:00")]
private IStateSlice<WeatherForecast[]> _forecasts = null!;
```

### IStateSlice&lt;T&gt; Properties

| Property | Description |
|----------|-------------|
| `Value` | Get/set the current value. Fires `OnChanged` on mutation. |
| `WasRestored` | `true` if the value was restored from prerendered state |
| `IsDirty` | `true` if the value was modified after creation |
| `IsStale` | `true` if the value has exceeded its configured TTL |
| `LastUpdated` | UTC timestamp of last value change |
| `OnChanged` | Event fired on every value change |
| `InitializeIfNeeded(T)` | Sets value only if not restored (sync) |
| `InitializeIfNeededAsync(Func<Task<T>>)` | Calls factory only if not restored (async) |

### Direct `StateManager` Usage

If you cannot use `[Slice]` (e.g., non-partial class), inject `StateManager` directly:

```csharp
@inject StateManager State
@implements IDisposable

@code {
    private IStateSlice<string> UserName = default!;

    protected override void OnInitialized()
    {
        UserName = State.CreateSlice("username", "Anonymous");
    }

    void IDisposable.Dispose() => State.Dispose();
}
```

## Security

Slice values are serialized as JSON into the prerendered HTML response and are visible in the page source. **Do not store sensitive data** (auth tokens, PII, secrets, role information) in state slices.

## Diagnostics

The source generator emits compile-time diagnostics:

| ID | Severity | Description |
|---|---|---|
| BSP001 | Error | `[Slice]` on non-partial class |
| BSP002 | Error | Field type is not `IStateSlice<T>` |
| BSP003 | Error | Class doesn't inherit `ComponentBase` |
| BSP005 | Error | Invalid `TimeToLive` format |
| BSP006 | Warning | Class already implements `IDisposable` — call `__DisposeSlices()` manually |
| BSP007 | Error | Duplicate slice keys |
| BSP008 | Error | `[Slice]` on static field |
| BSP011 | Warning | Class overrides `OnInitialized` — use `OnAfterSlicesCreated` instead |
| BSP012 | Warning | Field has initializer (will be overwritten by generator) |
```

- [ ] **Step 2: Verify the README renders correctly**

Read through the file to check for markdown formatting issues, broken tables, and ensure every code sample uses the current API (`[Slice]`, `OnInitializeSlices`, `SliceInitContext`, `StateManager.CreateSlice`). Confirm no references to deleted APIs (`PersistentComponentBase`, `UseSlice`, `UseGroup`, `CreateAndInit`, `CreateAndInitAsync`, `CreateGroup`, `IStateGroup`, `AllowUpdatesOnNavigation`).

- [ ] **Step 3: Commit**

```bash
git add BlazorStatePlus/README.md
git commit -m "docs: rewrite README to match current [Slice] + generator API"
```

---

### Task 3: Rewrite `BlazorStatePlus.Generators/README.md`

**Files:**
- Modify: `BlazorStatePlus.Generators/README.md`

The generators README is mostly accurate but needs a scope clarification at the top.

- [ ] **Step 1: Add scope note to generators README**

Add the following note after the first paragraph (line 3) of `BlazorStatePlus.Generators/README.md`:

```markdown
> **Scope:** This generator supports `PersistentComponentState` — the prerender-to-interactive handoff mechanism. State managed by this generator does **not** survive page refreshes, navigation, or browser sessions.
```

- [ ] **Step 2: Commit**

```bash
git add BlazorStatePlus.Generators/README.md
git commit -m "docs: add scope clarification to generators README"
```

---

### Task 4: Fix TTL clock divergence and document TTL semantics

**Files:**
- Modify: `BlazorStatePlus/Services/StateSlice.cs:14-19`
- Modify: `BlazorStatePlus/Services/StateManager.cs:30-84`
- Modify: `BlazorStatePlus/Abstractions/StateSliceOptions.cs`
- Modify: `BlazorStatePlus.Tests/StateManagerTests.cs`

The Legends Review found that restored slices reset `LastUpdated` to `UtcNow`, effectively extending the TTL window. For cached-prerender scenarios, the original `PersistedAt` timestamp should be preserved so `IsStale` reflects the true data age.

- [ ] **Step 1: Write failing test — restored slice preserves original TTL for staleness**

Add to `BlazorStatePlus.Tests/StateManagerTests.cs`:

```csharp
[Fact]
public void CreateSlice_RestoredSlice_IsStaleReflectsPersistedAt()
{
    // Persisted 4 minutes ago, TTL is 5 minutes
    FakeState.Persist("data", new StateManager.PersistedEnvelope<string>
    {
        Value = "old",
        PersistedAt = DateTimeOffset.UtcNow.AddMinutes(-4)
    });

    using var manager = CreateManager();
    var slice = manager.CreateSlice<string>("data", defaultValue: "new",
        configure: o => o.TimeToLive = TimeSpan.FromMinutes(5));

    // Value should be restored (4 min < 5 min TTL)
    slice.Value.ShouldBe("old");
    slice.WasRestored.ShouldBeTrue();

    // But IsStale should reflect the ORIGINAL data age (4 min into a 5 min TTL),
    // not reset to fresh. After 1 more minute, this should go stale.
    // For now, 4 min < 5 min, so not yet stale.
    slice.IsStale.ShouldBeFalse();

    // The key assertion: LastUpdated should be close to PersistedAt, not UtcNow
    var age = DateTimeOffset.UtcNow - slice.LastUpdated;
    age.TotalMinutes.ShouldBeGreaterThan(3.5); // Should be ~4 minutes old
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_RestoredSlice_IsStaleReflectsPersistedAt" -v n`
Expected: FAIL — `LastUpdated` is `UtcNow`, age is ~0 minutes

- [ ] **Step 3: Thread `PersistedAt` through to `StateSlice` constructor**

In `BlazorStatePlus/Services/StateSlice.cs`, modify the constructor to accept an optional `persistedAt` parameter:

```csharp
public StateSlice(T restoredValue, bool wasRestored, StateSliceOptions options,
    DateTimeOffset? persistedAt = null)
{
    _value = restoredValue;
    WasRestored = wasRestored;
    Options = options;
    LastUpdated = persistedAt ?? DateTimeOffset.UtcNow;
}
```

- [ ] **Step 4: Pass `PersistedAt` from `StateManager.CreateSlice` when restoring**

In `BlazorStatePlus/Services/StateManager.cs`, change the `StateSlice` construction to pass the persisted timestamp when the value was effectively restored. Inside the try-catch block (from Task 1), capture `persistedAt` alongside the restore logic:

Replace the line that creates the slice:
```csharp
var slice = new StateSlice<T>(restoredValue, effectivelyRestored, options);
```

With:
```csharp
var slice = new StateSlice<T>(restoredValue, effectivelyRestored, options, persistedAt);
```

And capture `persistedAt` alongside the restore logic. Add `DateTimeOffset? persistedAt = null;` next to `restoredValue` and `effectivelyRestored`, then set `persistedAt = envelope.PersistedAt;` in the successful restore branch (the `else` branch inside the `wasRestored && envelope is not null` check).

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test BlazorStatePlus.Tests --filter "CreateSlice_RestoredSlice_IsStaleReflectsPersistedAt" -v n`
Expected: PASS

- [ ] **Step 6: Run ALL tests to check for regressions**

Run: `dotnet test --verbosity normal`
Expected: ALL PASS. The `RuntimeTests.cs` in `BlazorStatePlus.Generators.Tests` calls `StateSlice` constructor without the new parameter — the default `null` ensures backward compatibility.

- [ ] **Step 7: Add XML doc for TTL semantics on `StateSliceOptions.TimeToLive`**

In `BlazorStatePlus/Abstractions/StateSliceOptions.cs`, replace the existing XML doc on `TimeToLive` with:

```csharp
/// <summary>
/// How long a restored value is considered "fresh".
/// <para>
/// <b>At restore time:</b> If the persisted data is older than this duration
/// (e.g., due to CDN caching, slow connections, or tab backgrounding),
/// the restored value is discarded and the default is used instead.
/// </para>
/// <para>
/// <b>At runtime:</b> <see cref="IStateSlice{T}.IsStale"/> returns <c>true</c>
/// when the time since the value was last updated (or originally persisted)
/// exceeds this duration, signaling that the consumer should re-fetch.
/// </para>
/// <para>Null means the value never goes stale.</para>
/// </summary>
```

- [ ] **Step 8: Commit**

```bash
git add BlazorStatePlus/Services/StateSlice.cs BlazorStatePlus/Services/StateManager.cs BlazorStatePlus/Abstractions/StateSliceOptions.cs BlazorStatePlus.Tests/StateManagerTests.cs
git commit -m "fix: preserve PersistedAt for restored slices so IsStale reflects true data age"
```

---

### Task 5: Add E2E integration test for prerender-to-interactive roundtrip

**Files:**
- Create: `BlazorStatePlus.Tests/PrerenderIntegrationTests.cs`
- Modify: `BlazorStatePlus.Tests/BlazorStatePlus.Tests.csproj`

This test uses `WebApplicationFactory` to serve the demo app, fetches the prerendered HTML, and verifies that slice state is embedded in the response. This proves the persist side of the roundtrip works with real Blazor infrastructure (not bUnit fakes). A full interactive-boot verification would require a browser (Playwright), which is out of scope for this task — but proving the state appears in the HTML is the critical gap.

- [ ] **Step 1: Add `Microsoft.AspNetCore.Mvc.Testing` to the test project**

In `BlazorStatePlus.Tests/BlazorStatePlus.Tests.csproj`, add:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0-*" />
</ItemGroup>
```

And add a project reference to the demo app:

```xml
<ItemGroup>
  <ProjectReference Include="..\BlazorStatePlus.Demo\BlazorStatePlus.Demo.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Make the Demo app's `Program.cs` discoverable by `WebApplicationFactory`**

Check if the demo project already has an entry point accessible to `WebApplicationFactory`. The default `WebApplication.CreateBuilder` pattern works if the test project references the demo and there's a public partial class or `InternalsVisibleTo`. The simplest approach: add to `BlazorStatePlus.Demo/BlazorStatePlus.Demo.csproj`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="BlazorStatePlus.Tests" />
</ItemGroup>
```

- [ ] **Step 3: Write the E2E test**

Create `BlazorStatePlus.Tests/PrerenderIntegrationTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace BlazorStatePlus.Tests;

public class PrerenderIntegrationTests : IClassFixture<WebApplicationFactory<BlazorStatePlus.Demo.Program>>
{
    private readonly HttpClient _client;

    public PrerenderIntegrationTests(WebApplicationFactory<BlazorStatePlus.Demo.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CounterPage_PrerenderContainsPersistentState()
    {
        var response = await _client.GetAsync("/counter");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        // Blazor embeds PersistentComponentState in a <persist-component-state> element
        // or in a <!--Blazor-Component-State:--> comment with base64 JSON.
        // The exact format depends on the .NET version, but the key "Counter.counter"
        // should appear somewhere in the serialized state.
        html.ShouldContain("blazor-component-state",
            "Prerendered HTML should contain embedded component state");
    }

    [Fact]
    public async Task WeatherPage_PrerenderContainsPersistentState()
    {
        var response = await _client.GetAsync("/weather");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        html.ShouldContain("blazor-component-state",
            "Prerendered HTML should contain embedded component state");
    }
}
```

- [ ] **Step 4: Ensure `Program` class is accessible**

If the demo's `Program.cs` uses top-level statements (it does), `WebApplicationFactory<Program>` needs the `Program` class to be accessible. Add to the bottom of `BlazorStatePlus.Demo/Program.cs`:

```csharp
namespace BlazorStatePlus.Demo;

public partial class Program { }
```

- [ ] **Step 5: Run the E2E tests**

Run: `dotnet test BlazorStatePlus.Tests --filter "PrerenderIntegrationTests" -v n`
Expected: PASS — both pages return HTML containing `blazor-component-state`

If the tests fail because the state marker format differs in .NET 10, inspect the actual HTML response and adjust the assertion string accordingly. The goal is to verify that `PersistentComponentState` data is embedded in the prerendered output.

- [ ] **Step 6: Run all tests to confirm no regressions**

Run: `dotnet test --verbosity normal`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add BlazorStatePlus.Tests/PrerenderIntegrationTests.cs BlazorStatePlus.Tests/BlazorStatePlus.Tests.csproj BlazorStatePlus.Demo/BlazorStatePlus.Demo.csproj BlazorStatePlus.Demo/Program.cs
git commit -m "test: add E2E integration tests for prerender-to-interactive roundtrip"
```
