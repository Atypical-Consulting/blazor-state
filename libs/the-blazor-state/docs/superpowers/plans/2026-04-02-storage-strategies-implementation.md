# Storage Strategies Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace stub storage strategies with real implementations backed by browser JS APIs (sessionStorage, localStorage, IndexedDB) and make StateManager actually delegate to the configured strategy.

**Architecture:** A JavaScript module (`theblazorstate.js`) provides get/set/remove for all three browser stores. A scoped `BrowserStorageService` wraps `IJSRuntime` calls. Browser strategies delegate to this service. StateManager always tries PrerenderHtml sync restore first (for prerender-to-interactive), then falls back to the configured strategy async in `OnInitializedAsync`. Persist callbacks write to both PrerenderHtml AND the configured strategy.

**Tech Stack:** Blazor JSInterop (`IJSRuntime`), `System.Text.Json`, IndexedDB (via JS wrapper)

**Key design decisions:**
- PrerenderHtml sync restore always runs in `OnInitialized` (for the Blazor prerender gap)
- Browser strategies run async in `OnInitializedAsync` (JSInterop is async-only)
- On persist: always write to PrerenderHtml + ServerMemoryCache + configured strategy
- The `IStorageStrategy` interface stays unchanged — all methods are already async
- Browser strategies are NOT singletons — they hold a reference to `BrowserStorageService`

---

## File Structure

| File | Responsibility |
|------|---------------|
| `TheBlazorState/wwwroot/theblazorstate.js` | Browser-side get/set/remove for sessionStorage, localStorage, IndexedDB |
| `TheBlazorState/Storage/BrowserStorageService.cs` | Scoped C# service wrapping IJSRuntime calls to the JS module |
| `TheBlazorState/Storage/Strategies/SessionStorageStrategy.cs` | Delegates to BrowserStorageService for sessionStorage |
| `TheBlazorState/Storage/Strategies/LocalStorageStrategy.cs` | Delegates to BrowserStorageService for localStorage |
| `TheBlazorState/Storage/Strategies/IndexedDbStrategy.cs` | Delegates to BrowserStorageService for IndexedDB |
| `TheBlazorState/Storage/Strategies/ServerMemoryCacheStrategy.cs` | Uses IMemoryCache directly |
| `TheBlazorState/Storage/StorageStrategy.cs` | Factory updated to accept BrowserStorageService |
| `TheBlazorState/Services/StateManager.cs` | New `RestorePropertyAsync` + pass strategy to persist |
| `TheBlazorState.Generators/PersistEmitter.cs` | Emit async restore call + pass strategy reference |
| `TheBlazorState/Extensions/ServiceCollectionExtensions.cs` | Register BrowserStorageService |

---

### Task 1: JavaScript Interop Module

**Files:**
- Create: `TheBlazorState/wwwroot/theblazorstate.js`

- [ ] **Step 1: Create the JS module**

```javascript
// theblazorstate.js — Browser storage operations for TheBlazorState

export function getItem(storeName, key) {
    if (storeName === "sessionStorage") {
        const raw = sessionStorage.getItem(key);
        return raw ? JSON.parse(raw) : null;
    }
    if (storeName === "localStorage") {
        const raw = localStorage.getItem(key);
        return raw ? JSON.parse(raw) : null;
    }
    // IndexedDB handled by getItemIndexedDb
    return null;
}

export function setItem(storeName, key, value) {
    const json = JSON.stringify(value);
    if (storeName === "sessionStorage") {
        sessionStorage.setItem(key, json);
        return;
    }
    if (storeName === "localStorage") {
        localStorage.setItem(key, json);
        return;
    }
}

export function removeItem(storeName, key) {
    if (storeName === "sessionStorage") {
        sessionStorage.removeItem(key);
        return;
    }
    if (storeName === "localStorage") {
        localStorage.removeItem(key);
        return;
    }
}

// IndexedDB operations (async)
const DB_NAME = "TheBlazorState";
const STORE_NAME = "state";
const DB_VERSION = 1;

function openDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME);
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

export async function getItemIndexedDb(key) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readonly");
        const store = tx.objectStore(STORE_NAME);
        const request = store.get(key);
        request.onsuccess = () => resolve(request.result ?? null);
        request.onerror = () => reject(request.error);
    });
}

export async function setItemIndexedDb(key, value) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        const store = tx.objectStore(STORE_NAME);
        const request = store.put(value, key);
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
    });
}

export async function removeItemIndexedDb(key) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        const store = tx.objectStore(STORE_NAME);
        const request = store.delete(key);
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
    });
}
```

- [ ] **Step 2: Verify the .csproj includes wwwroot as static web asset**

The project uses `Microsoft.NET.Sdk.Razor`, which auto-includes `wwwroot/` as static web assets. Verify with:

```bash
dotnet build TheBlazorState/TheBlazorState.csproj
```

Expected: Build succeeds, JS file included in output.

- [ ] **Step 3: Commit**

```bash
git add TheBlazorState/wwwroot/theblazorstate.js
git commit -m "feat: add JS interop module for browser storage strategies"
```

---

### Task 2: BrowserStorageService

**Files:**
- Create: `TheBlazorState/Storage/BrowserStorageService.cs`
- Test: `TheBlazorState.Tests/BrowserStorageServiceTests.cs`

- [ ] **Step 1: Create BrowserStorageService**

```csharp
using System.Text.Json;
using Microsoft.JSInterop;

namespace TheBlazorState.Storage;

/// <summary>
/// Scoped service wrapping IJSRuntime for browser storage operations.
/// Used by SessionStorage, LocalStorage, and IndexedDb strategies.
/// </summary>
public sealed class BrowserStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public BrowserStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/TheBlazorState/theblazorstate.js");
        return _module;
    }

    public async Task<StorageResult<T>> GetAsync<T>(string storeName, string key)
    {
        try
        {
            var module = await GetModuleAsync();

            JsonElement? raw;
            if (storeName == "indexedDb")
                raw = await module.InvokeAsync<JsonElement?>("getItemIndexedDb", key);
            else
                raw = await module.InvokeAsync<JsonElement?>("getItem", storeName, key);

            if (raw is null || raw.Value.ValueKind == JsonValueKind.Null)
                return new StorageResult<T>(false, default, null);

            var envelope = JsonSerializer.Deserialize<StorageEnvelope<T>>(raw.Value.GetRawText());
            if (envelope is null)
                return new StorageResult<T>(false, default, null);

            return new StorageResult<T>(true, envelope.Value, envelope.PersistedAt);
        }
        catch
        {
            return new StorageResult<T>(false, default, null);
        }
    }

    public async Task SetAsync<T>(string storeName, string key, T value, StorageMetadata metadata)
    {
        var module = await GetModuleAsync();

        var envelope = new StorageEnvelope<T>
        {
            Value = value,
            PersistedAt = metadata.Timestamp
        };

        if (storeName == "indexedDb")
            await module.InvokeVoidAsync("setItemIndexedDb", key, envelope);
        else
            await module.InvokeVoidAsync("setItem", storeName, key, envelope);
    }

    public async Task RemoveAsync(string storeName, string key)
    {
        var module = await GetModuleAsync();

        if (storeName == "indexedDb")
            await module.InvokeVoidAsync("removeItemIndexedDb", key);
        else
            await module.InvokeVoidAsync("removeItem", storeName, key);
    }

    internal sealed class StorageEnvelope<T>
    {
        public T Value { get; set; } = default!;
        public DateTimeOffset PersistedAt { get; set; }
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build TheBlazorState/TheBlazorState.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TheBlazorState/Storage/BrowserStorageService.cs
git commit -m "feat: add BrowserStorageService wrapping IJSRuntime"
```

---

### Task 3: Implement Browser Storage Strategies

Replace the stubs with real implementations.

**Files:**
- Modify: `TheBlazorState/Storage/Strategies/SessionStorageStrategy.cs`
- Modify: `TheBlazorState/Storage/Strategies/LocalStorageStrategy.cs`
- Modify: `TheBlazorState/Storage/Strategies/IndexedDbStrategy.cs`
- Modify: `TheBlazorState/Storage/Strategies/ServerMemoryCacheStrategy.cs`
- Modify: `TheBlazorState/Storage/StorageStrategy.cs`
- Modify: `TheBlazorState/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Rewrite SessionStorageStrategy**

```csharp
namespace TheBlazorState.Storage;

/// <summary>
/// Persists state to browser sessionStorage. Survives page refresh within the same tab.
/// Requires Blazor WASM or InteractiveServer with browser context.
/// </summary>
public sealed class SessionStorageStrategy : IStorageStrategy
{
    internal static readonly SessionStorageStrategy Instance = new();

    private BrowserStorageService? _service;

    internal void Initialize(BrowserStorageService service) => _service = service;

    private BrowserStorageService Service =>
        _service ?? throw new InvalidOperationException(
            "SessionStorage strategy requires BrowserStorageService. Ensure AddTheBlazorState() is called.");

    public Task<StorageResult<T>> RestoreAsync<T>(string key) =>
        Service.GetAsync<T>("sessionStorage", key);

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata) =>
        Service.SetAsync("sessionStorage", key, value, metadata);

    public Task RemoveAsync(string key) =>
        Service.RemoveAsync("sessionStorage", key);
}
```

- [ ] **Step 2: Rewrite LocalStorageStrategy (same pattern)**

```csharp
namespace TheBlazorState.Storage;

public sealed class LocalStorageStrategy : IStorageStrategy
{
    internal static readonly LocalStorageStrategy Instance = new();

    private BrowserStorageService? _service;

    internal void Initialize(BrowserStorageService service) => _service = service;

    private BrowserStorageService Service =>
        _service ?? throw new InvalidOperationException(
            "LocalStorage strategy requires BrowserStorageService. Ensure AddTheBlazorState() is called.");

    public Task<StorageResult<T>> RestoreAsync<T>(string key) =>
        Service.GetAsync<T>("localStorage", key);

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata) =>
        Service.SetAsync("localStorage", key, value, metadata);

    public Task RemoveAsync(string key) =>
        Service.RemoveAsync("localStorage", key);
}
```

- [ ] **Step 3: Rewrite IndexedDbStrategy (same pattern)**

```csharp
namespace TheBlazorState.Storage;

public sealed class IndexedDbStrategy : IStorageStrategy
{
    internal static readonly IndexedDbStrategy Instance = new();

    private BrowserStorageService? _service;

    internal void Initialize(BrowserStorageService service) => _service = service;

    private BrowserStorageService Service =>
        _service ?? throw new InvalidOperationException(
            "IndexedDb strategy requires BrowserStorageService. Ensure AddTheBlazorState() is called.");

    public Task<StorageResult<T>> RestoreAsync<T>(string key) =>
        Service.GetAsync<T>("indexedDb", key);

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata) =>
        Service.SetAsync("indexedDb", key, value, metadata);

    public Task RemoveAsync(string key) =>
        Service.RemoveAsync("indexedDb", key);
}
```

- [ ] **Step 4: Implement ServerMemoryCacheStrategy properly**

Replace the marker with a real implementation that uses `IMemoryCache`:

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace TheBlazorState.Storage;

/// <summary>
/// Persists state in server-side IMemoryCache. Survives page reloads on Blazor Server.
/// </summary>
public sealed class ServerMemoryCacheStrategy : IStorageStrategy
{
    internal static readonly ServerMemoryCacheStrategy Instance = new();

    private IMemoryCache? _cache;

    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    internal void Initialize(IMemoryCache cache) => _cache = cache;

    private IMemoryCache Cache =>
        _cache ?? throw new InvalidOperationException(
            "ServerMemoryCache strategy requires IMemoryCache. Ensure AddTheBlazorState() is called.");

    public Task<StorageResult<T>> RestoreAsync<T>(string key)
    {
        if (Cache.TryGetValue<BrowserStorageService.StorageEnvelope<T>>(key, out var envelope)
            && envelope is not null)
        {
            return Task.FromResult(new StorageResult<T>(true, envelope.Value, envelope.PersistedAt));
        }
        return Task.FromResult(new StorageResult<T>(false, default, null));
    }

    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
    {
        var envelope = new BrowserStorageService.StorageEnvelope<T>
        {
            Value = value,
            PersistedAt = metadata.Timestamp
        };
        Cache.Set(key, envelope, CacheEntryOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        Cache.Remove(key);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: Update ServiceCollectionExtensions to register BrowserStorageService and initialize strategies**

```csharp
using TheBlazorState.Configuration;
using TheBlazorState.Services;
using TheBlazorState.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace TheBlazorState.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTheBlazorState(
        this IServiceCollection services,
        Action<TheBlazorStateOptions>? configure = null)
    {
        var options = new TheBlazorStateOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddScoped<BrowserStorageService>();
        services.AddScoped<StateManager>();

        // Initialize strategy singletons with their dependencies at first resolution
        services.AddScoped<StorageStrategyInitializer>();

        return services;
    }
}

/// <summary>
/// Scoped service that initializes storage strategy singletons with their DI dependencies.
/// Resolved once per scope by StateManager.
/// </summary>
internal sealed class StorageStrategyInitializer
{
    public StorageStrategyInitializer(
        BrowserStorageService browserStorage,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        SessionStorageStrategy.Instance.Initialize(browserStorage);
        LocalStorageStrategy.Instance.Initialize(browserStorage);
        IndexedDbStrategy.Instance.Initialize(browserStorage);
        ServerMemoryCacheStrategy.Instance.Initialize(cache);
    }
}
```

- [ ] **Step 6: Build**

```bash
dotnet build TheBlazorState.slnx
```

Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: implement browser storage strategies with JSInterop"
```

---

### Task 4: Update StateManager for Strategy Delegation

Make StateManager pass the configured strategy through, and add an async restore path.

**Files:**
- Modify: `TheBlazorState/Services/StateManager.cs`

- [ ] **Step 1: Add strategy parameters back to RestoreProperty**

Update `RestoreProperty` signature to accept the storage strategy. And add `RestorePropertyAsync` for browser strategy fallback. Also update persist callbacks to write through the strategy.

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TheBlazorState.Abstractions;
using TheBlazorState.Configuration;
using TheBlazorState.Storage;

namespace TheBlazorState.Services;

public sealed class StateManager(
    PersistentComponentState persistence,
    IMemoryCache cache,
    ILogger<StateManager> logger,
    TheBlazorStateOptions options,
    StorageStrategyInitializer initializer) : IDisposable
{
    private readonly List<PersistingComponentStateSubscription> _subscriptions = [];
    private readonly List<Func<Task>> _persistCallbacks = [];
    private readonly HashSet<string> _registeredKeys = [];
    private bool _registered;
    private bool _disposed;

    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    /// <summary>
    /// Resolves the effective storage strategy using the cascade:
    /// property-level > component-level > global > PrerenderHtml
    /// </summary>
    internal IStorageStrategy ResolveStrategy(IStorageStrategy? propertyLevel, IStorageStrategy? componentLevel)
    {
        return propertyLevel ?? componentLevel ?? options.DefaultStorage;
    }

    /// <summary>
    /// Sync restore from PrerenderHtml + ServerMemoryCache.
    /// Always runs in OnInitialized regardless of configured strategy.
    /// </summary>
    public void RestoreProperty<T>(
        string key,
        IStorageStrategy? strategy,
        StateMeta meta,
        Action<T> valueSetter,
        Func<T> valueGetter)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_registeredKeys.Add(key))
            throw new InvalidOperationException(
                $"A property with key '{key}' has already been registered.");

        var ttl = meta.TimeToLive;

        // Sync path: try prerender state
        try
        {
            if (persistence.TryTakeFromJson<PersistedEnvelope<T>>(key, out var envelope)
                && envelope is not null)
            {
                if (ttl.HasValue && DateTimeOffset.UtcNow - envelope.PersistedAt > ttl.Value)
                {
                    logger.LogDebug("Property '{Key}': prerender value discarded (TTL expired)", key);
                }
                else
                {
                    valueSetter(envelope.Value);
                    meta.MarkRestored(envelope.PersistedAt);
                    logger.LogDebug("Property '{Key}': restored from prerender", key);
                }
            }

            // Sync path: try server memory cache
            if (!meta.WasRestored
                && cache.TryGetValue<PersistedEnvelope<T>>(key, out var cached)
                && cached is not null)
            {
                if (ttl.HasValue && DateTimeOffset.UtcNow - cached.PersistedAt > ttl.Value)
                {
                    logger.LogDebug("Property '{Key}': cache value discarded (TTL expired)", key);
                }
                else
                {
                    valueSetter(cached.Value);
                    meta.MarkRestored(cached.PersistedAt);
                    logger.LogDebug("Property '{Key}': restored from server cache", key);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Property '{Key}': sync restore failed", key);
        }

        // Register persist callback (always writes prerender + cache + strategy)
        var resolvedStrategy = strategy ?? options.DefaultStorage;
        RegisterPersistPropertyCallback(key, valueGetter, resolvedStrategy);
    }

    /// <summary>
    /// Async restore from the configured storage strategy.
    /// Called in OnInitializedAsync if sync restore didn't find a value.
    /// Only runs for strategies that actually store data (not PrerenderHtml/ServerMemoryCache markers).
    /// </summary>
    public async Task RestorePropertyAsync<T>(
        string key,
        IStorageStrategy? strategy,
        StateMeta meta,
        Action<T> valueSetter)
    {
        if (meta.WasRestored)
            return;

        var resolvedStrategy = strategy ?? options.DefaultStorage;

        // Skip if the strategy is PrerenderHtml or ServerMemoryCache (already tried sync)
        if (resolvedStrategy is PrerenderHtmlStrategy or ServerMemoryCacheStrategy)
            return;

        try
        {
            var result = await resolvedStrategy.RestoreAsync<T>(key);
            if (result.Found && result.Value is not null)
            {
                var ttl = meta.TimeToLive;
                if (ttl.HasValue && result.PersistedAt.HasValue
                    && DateTimeOffset.UtcNow - result.PersistedAt.Value > ttl.Value)
                {
                    logger.LogDebug("Property '{Key}': strategy value discarded (TTL expired)", key);
                    return;
                }

                valueSetter(result.Value);
                meta.MarkRestored(result.PersistedAt ?? DateTimeOffset.UtcNow);
                logger.LogDebug("Property '{Key}': restored from {Strategy}", key, resolvedStrategy.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Property '{Key}': async restore failed from {Strategy}", key, resolvedStrategy.GetType().Name);
        }
    }

    private void RegisterPersistPropertyCallback<T>(string key, Func<T> valueGetter, IStorageStrategy strategy)
    {
        RegisterPersistCallback(async () =>
        {
            var value = valueGetter();
            var now = DateTimeOffset.UtcNow;
            var envelope = new PersistedEnvelope<T>
            {
                Value = value,
                PersistedAt = now
            };

            // Always write to prerender + cache
            persistence.PersistAsJson(key, envelope);
            cache.Set(key, envelope, CacheEntryOptions);

            // Also write to the configured strategy if it's not prerender/cache
            if (strategy is not PrerenderHtmlStrategy and not ServerMemoryCacheStrategy)
            {
                try
                {
                    var metadata = new StorageMetadata(key, null, now);
                    await strategy.PersistAsync(key, value, metadata);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Property '{Key}': strategy persist failed", key);
                }
            }
        });
    }

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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var sub in _subscriptions)
            sub.Dispose();

        _subscriptions.Clear();
        _persistCallbacks.Clear();
        _registeredKeys.Clear();
    }

    internal sealed class PersistedEnvelope<T>
    {
        public T Value { get; init; } = default!;
        public DateTimeOffset PersistedAt { get; init; }
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build TheBlazorState.slnx
```

Expected: May have build errors in tests that reference old RestoreProperty signature. Fix them in the next step.

- [ ] **Step 3: Update existing tests for new RestoreProperty signature**

`StateManagerRestoreTests.cs` calls `RestoreProperty` without the `strategy` parameter. Add `null` as the second argument to all calls:

Old: `sm.RestoreProperty<string>(key, meta, setter, getter);`
New: `sm.RestoreProperty<string>(key, null, meta, setter, getter);`

Also update the StateManager construction to include `TheBlazorStateOptions` and `StorageStrategyInitializer` (or adjust test setup).

- [ ] **Step 4: Run tests**

```bash
dotnet test TheBlazorState.slnx
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: StateManager delegates to configured storage strategy"
```

---

### Task 5: Update Generator to Pass Strategy and Call Async Restore

**Files:**
- Modify: `TheBlazorState.Generators/PersistEmitter.cs`

- [ ] **Step 1: Update OnInitialized emission to pass strategy**

In `PersistEmitter.cs`, change the `RestoreProperty` call to include the strategy:

Old (line ~144):
```csharp
sb.AppendLine($"        __stateManager.RestoreProperty<{prop.FullTypeName}>(");
sb.AppendLine($"            __ctx.{prop.PropertyName}.ResolveKey(\"{EscapeString(prop.BaseKey)}\"),");
sb.AppendLine($"            {metaField},");
sb.AppendLine($"            __v => {backingField} = __v,");
sb.AppendLine($"            () => {backingField});");
```

New:
```csharp
sb.AppendLine($"        __stateManager.RestoreProperty<{prop.FullTypeName}>(");
sb.AppendLine($"            __ctx.{prop.PropertyName}.ResolveKey(\"{EscapeString(prop.BaseKey)}\"),");
sb.AppendLine($"            __ctx.{prop.PropertyName}.Storage ?? __ctx.Storage,");
sb.AppendLine($"            {metaField},");
sb.AppendLine($"            __v => {backingField} = __v,");
sb.AppendLine($"            () => {backingField});");
```

- [ ] **Step 2: Add async restore call in OnInitializedAsync**

In `PersistEmitter.cs`, in the `OnInitializedAsync` method, BEFORE the async factory section, add async restore for each property. The `__stateCtx` should always be kept (not just when HasAsyncInit), so change the logic:

Replace the `__stateCtx = __ctx.HasAsyncInit ? __ctx : null;` line in OnInitialized with:
```csharp
sb.AppendLine("        __stateCtx = __ctx;");
```

Then in OnInitializedAsync, emit before the factory calls:
```csharp
// Async restore from configured strategy (browser storage etc.)
foreach (var prop in model.Properties)
{
    string backingField = $"__{prop.PropertyName}_backing";
    string metaField = $"__{prop.PropertyName}_meta";

    sb.AppendLine($"        await __stateManager.RestorePropertyAsync<{prop.FullTypeName}>(");
    sb.AppendLine($"            __ctx.{prop.PropertyName}.ResolveKey(\"{EscapeString(prop.BaseKey)}\"),");
    sb.AppendLine($"            __ctx.{prop.PropertyName}.Storage ?? __ctx.Storage,");
    sb.AppendLine($"            {metaField},");
    sb.AppendLine($"            __v => {backingField} = __v);");
}
```

Then the factory section follows:
```csharp
foreach (var prop in model.Properties)
{
    // existing factory code...
}
```

The full OnInitializedAsync should look like:
```csharp
protected override async Task OnInitializedAsync()
{
    await base.OnInitializedAsync();

    var __ctx = __stateCtx;
    __stateCtx = null;

    if (__ctx is not null)
    {
        // Async restore from configured strategy
        await __stateManager.RestorePropertyAsync<int>(
            __ctx.Counter.ResolveKey("MyComponent.Counter"),
            __ctx.Counter.Storage ?? __ctx.Storage,
            __Counter_meta,
            __v => __Counter_backing = __v);

        // Async factories
        if (__ctx.Counter.HasAsyncFactory
            && (!__Counter_meta.WasRestored || __Counter_meta.IsStale))
        {
            __Counter_backing = await __ctx.Counter.InvokeFactoryAsync();
            __Counter_meta.MarkDirty();
        }
    }
}
```

- [ ] **Step 3: Build and run tests**

```bash
dotnet build TheBlazorState.slnx
dotnet test TheBlazorState.slnx
```

Expected: All tests pass. Some generator output tests may need updating to match new generated code shape.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: generator emits strategy-aware restore and async restore"
```

---

### Task 6: Integration Tests for Storage Strategies

**Files:**
- Create: `TheBlazorState.Tests/StorageStrategyIntegrationTests.cs`
- Modify: `TheBlazorState.Tests/StorageStrategyTests.cs`

- [ ] **Step 1: Add tests for ServerMemoryCacheStrategy**

```csharp
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Tests;

public class ServerMemoryCacheIntegrationTests
{
    [Fact]
    public async Task ServerMemoryCache_PersistAndRestore_RoundTrips()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var strategy = ServerMemoryCacheStrategy.Instance;
        strategy.Initialize(cache);

        var metadata = new StorageMetadata("test.key", null, DateTimeOffset.UtcNow);
        await strategy.PersistAsync("test.key", "hello", metadata);

        var result = await strategy.RestoreAsync<string>("test.key");
        result.Found.ShouldBeTrue();
        result.Value.ShouldBe("hello");
        result.PersistedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ServerMemoryCache_RestoreAsync_Returns_NotFound_When_Empty()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var strategy = ServerMemoryCacheStrategy.Instance;
        strategy.Initialize(cache);

        var result = await strategy.RestoreAsync<string>("nonexistent");
        result.Found.ShouldBeFalse();
    }

    [Fact]
    public async Task ServerMemoryCache_RemoveAsync_Clears_Value()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var strategy = ServerMemoryCacheStrategy.Instance;
        strategy.Initialize(cache);

        var metadata = new StorageMetadata("test.key", null, DateTimeOffset.UtcNow);
        await strategy.PersistAsync("test.key", 42, metadata);
        await strategy.RemoveAsync("test.key");

        var result = await strategy.RestoreAsync<int>("test.key");
        result.Found.ShouldBeFalse();
    }
}
```

Note: Browser strategies can't be tested without a real browser/JSInterop. They would need E2E/Playwright tests which are out of scope for this task.

- [ ] **Step 2: Update existing StorageStrategyTests**

The existing tests that check `StorageStrategy.SessionStorage()` etc. return instances — update to verify the instances are the correct types and have the expected behavior (throw when uninitialized).

- [ ] **Step 3: Run all tests**

```bash
dotnet test TheBlazorState.slnx
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test: add integration tests for storage strategies"
```

---

## Task Dependency Summary

```
Task 1 (JS module) → Task 2 (BrowserStorageService) → Task 3 (strategy impls)
    → Task 4 (StateManager) → Task 5 (generator) → Task 6 (tests)
```

All tasks are sequential.
