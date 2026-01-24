# Phase 4: Persistence - Research

**Researched:** 2026-01-24
**Domain:** Browser Storage Persistence for Blazor State Management
**Confidence:** HIGH

## Summary

This phase implements state persistence for Bustand stores via LocalStorage and SessionStorage. The research covers three critical areas: (1) browser storage APIs and their access patterns in Blazor WASM vs Server modes, (2) serialization of C# record types with System.Text.Json, and (3) debouncing patterns for efficient storage writes.

The key finding is that Blazor Server and WebAssembly require fundamentally different approaches to browser storage. WASM can use synchronous JS interop directly, while Server mode requires async calls via SignalR (or ProtectedBrowserStorage for encrypted storage). The decision to use System.Text.Json (from CONTEXT.md) is well-supported - records serialize naturally with zero configuration.

For circuit reconnection in Server mode, the new .NET 10 `[PersistentState]` attribute provides automatic circuit state persistence, but this is orthogonal to our browser storage persistence. For Bustand, browser storage (via JS interop) is the appropriate choice since it persists across full page reloads, not just circuit disconnections.

**Primary recommendation:** Build a lightweight JS interop abstraction (`IBrowserStorage`) that handles both WASM and Server modes, with PersistenceMiddleware using debounced writes (300ms) after state changes and eager reads during store construction.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | Built-in | State serialization | Already in .NET, perfect record support, no dependencies |
| IJSRuntime | Built-in | Browser storage access | Official Blazor JS interop mechanism |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Blazored.LocalStorage | 4.5+ | Simplified storage API | Alternative if more features needed (has prerender handling) |
| ProtectedBrowserStorage | Built-in (Server) | Encrypted server-side storage | When data encryption is required |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Direct IJSRuntime | Blazored.LocalStorage | Adds dependency but has mature prerender handling - NOT needed since we handle timing ourselves |
| System.Text.Json | Newtonsoft.Json | More features but adds dependency and slower - NOT recommended |
| Manual debounce | ThrottleDebounce package | Overkill for single-use case - Timer-based is sufficient |

**No additional packages required** - all functionality available in .NET and Blazor built-ins.

## Architecture Patterns

### Recommended Project Structure
```
src/Bustand/
├── Persistence/
│   ├── PersistAttribute.cs           # [Persist(StorageType)] attribute
│   ├── StorageType.cs                # Enum: Local, Session
│   ├── PersistenceOptions.cs         # Global prefix, JsonSerializerOptions
│   ├── IBrowserStorage.cs            # Abstraction for storage operations
│   ├── BrowserStorageService.cs      # JS interop implementation
│   └── PersistenceMiddleware.cs      # IMiddleware<TState> implementation
├── Middleware/
│   └── (existing middleware files)
└── Configuration/
    └── BustandOptions.cs             # Extended with persistence options
```

### Pattern 1: Attribute-Based Opt-In Persistence
**What:** Stores declare persistence via `[Persist(StorageType.Local)]` attribute on the class
**When to use:** Always - this is the primary developer interface

```csharp
// Source: Bustand convention per CONTEXT.md decisions
[BustandStore]
[Persist(StorageType.Local, Key = "my-counter")]  // Optional custom key
public class CounterStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new(Count: 0);

    public void Increment() => Set(s => s with { Count = s.Count + 1 });
}
```

### Pattern 2: Browser Storage Abstraction
**What:** Interface hiding JS interop details, with async-only API
**When to use:** All browser storage operations

```csharp
// Source: Standard Blazor JS interop pattern
public interface IBrowserStorage
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
    bool IsAvailable { get; }  // False during prerender
}

public class BrowserStorageService : IBrowserStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isAvailable;

    public bool IsAvailable => _isAvailable;

    // Initialized in OnAfterRenderAsync of a component or via circuit events
    public void SetAvailable() => _isAvailable = true;

    public async Task<T?> GetAsync<T>(string key)
    {
        if (!_isAvailable) return default;
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        if (!_isAvailable) return;
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }
}
```

### Pattern 3: Debounced Write with Timer
**What:** Delay storage writes until activity settles to reduce I/O
**When to use:** On every state change that triggers persistence

```csharp
// Source: Blazor debounce pattern (blog.jeremylikness.com)
public class DebouncedWriter<TState> : IDisposable where TState : class
{
    private readonly IBrowserStorage _storage;
    private readonly string _key;
    private readonly JsonSerializerOptions _options;
    private Timer? _timer;
    private TState? _pendingState;
    private readonly object _lock = new();
    private const int DebounceMs = 300;  // Claude's discretion: balance perf/safety

    public void QueueWrite(TState state)
    {
        lock (_lock)
        {
            _pendingState = state;
            _timer?.Dispose();
            _timer = new Timer(FlushCallback, null, DebounceMs, Timeout.Infinite);
        }
    }

    private async void FlushCallback(object? _)
    {
        TState? toWrite;
        lock (_lock)
        {
            toWrite = _pendingState;
            _pendingState = null;
        }
        if (toWrite is not null)
        {
            await _storage.SetAsync(_key, toWrite);
        }
    }

    public void Dispose() => _timer?.Dispose();
}
```

### Pattern 4: Middleware-Based Persistence
**What:** PersistenceMiddleware intercepts AfterChange for writes, performs restore during construction
**When to use:** All persistent stores

```csharp
// Source: Existing Bustand middleware pattern (LoggingMiddleware)
public class PersistenceMiddleware<TState> : IMiddleware<TState>, IDisposable
    where TState : class
{
    private readonly IBrowserStorage _storage;
    private readonly DebouncedWriter<TState> _writer;
    private readonly string _storageKey;

    public bool OnBeforeChange(MiddlewareContext<TState> context) => true;

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        _writer.QueueWrite(context.NewState);
    }

    // Called during store factory construction
    public async Task<TState?> RestoreStateAsync()
    {
        return await _storage.GetAsync<TState>(_storageKey);
    }
}
```

### Pattern 5: Schema Migration with Version Field
**What:** Include schema version in persisted state, migrate on restore
**When to use:** When state shape may change between app versions

```csharp
// Source: Couchbase schema versioning pattern
public record PersistedEnvelope<TState>(
    int SchemaVersion,
    TState State,
    DateTimeOffset PersistedAt
);

// On restore:
var envelope = await _storage.GetAsync<PersistedEnvelope<TState>>(key);
if (envelope is null) return InitialState;
if (envelope.SchemaVersion < CurrentVersion)
{
    return MigrateState(envelope.State, envelope.SchemaVersion, CurrentVersion);
}
return envelope.State;
```

### Anti-Patterns to Avoid
- **Synchronous JS interop in Server mode:** Causes SignalR deadlocks; always use async
- **Persisting during OnInitialized:** JS interop unavailable during prerender; use OnAfterRender
- **Writing on every Set() without debounce:** Causes storage thrashing and performance issues
- **Storing non-serializable state:** Events, delegates, and Funcs cannot be serialized; design state records to be pure data
- **Using ProtectedBrowserStorage in WASM:** Server-only API; use plain JS interop for WASM

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON serialization | Custom string conversion | System.Text.Json | Handles records, null, dates, nested types correctly |
| JS interop timing | Manual flags | OnAfterRenderAsync pattern | Blazor guarantees JS available after first render |
| Debouncing | Manual timer management | Timer with callback pattern | Thread-safe, testable, dispose-aware |
| Storage key generation | String concatenation | `$"{prefix}.{typeof(TState).FullName}"` | Consistent, collision-free |

**Key insight:** The browser storage API itself is trivial (getItem/setItem/removeItem). The complexity is in timing (when to call), serialization (what to store), and efficiency (how often to write). Focus implementation effort on these areas, not the storage calls themselves.

## Common Pitfalls

### Pitfall 1: JS Interop During Prerender
**What goes wrong:** `InvalidOperationException: JavaScript interop calls cannot be issued at this time`
**Why it happens:** During Server-side prerendering, no browser exists to execute JavaScript
**How to avoid:**
- Check `IBrowserStorage.IsAvailable` before operations
- Initialize storage service in `OnAfterRenderAsync(firstRender: true)`
- For restore during construction, use async factory pattern with deferred restoration
**Warning signs:** Exception thrown during store DI resolution

### Pitfall 2: SignalR Message Size Limit
**What goes wrong:** Large state fails to persist in Server mode with SignalR exception
**Why it happens:** Default SignalR message size is 32KB; JS interop uses SignalR
**How to avoid:**
- Keep persisted state small (< 10KB recommended)
- Warn in documentation about size limits
- Consider compression for larger states (future enhancement)
**Warning signs:** Works in WASM but fails in Server mode

### Pitfall 3: Circular Reference in State
**What goes wrong:** `JsonException: A possible object cycle was detected`
**Why it happens:** State contains navigation properties or self-references
**How to avoid:**
- Design state records as pure data without navigation
- Use `[JsonIgnore]` on non-serializable members
- Configure `ReferenceHandler.IgnoreCycles` in JsonSerializerOptions (with warning)
**Warning signs:** Serialization works for simple state, fails when state grows complex

### Pitfall 4: Concurrent Restore and Update Race
**What goes wrong:** Store receives Set() before async restore completes, losing persisted data
**Why it happens:** Async restore runs after constructor, Set() can happen immediately
**How to avoid:**
- Use synchronous restore with fallback to InitialState
- Or block state access until restore completes (IsInitialized pattern)
- Existing `EnsureInitializedAsync` can coordinate with persistence restore
**Warning signs:** Intermittent loss of persisted state on page reload

### Pitfall 5: Memory Leak from Undisposed Timer
**What goes wrong:** Timer callbacks continue after component/store disposal
**Why it happens:** Debounce timer not disposed when store is disposed (Scoped stores in Server mode)
**How to avoid:**
- Implement IDisposable on PersistenceMiddleware
- Dispose timer in Dispose() method
- Register disposal with DI container or store lifecycle
**Warning signs:** Multiple timer callbacks for same store, memory growth

### Pitfall 6: Schema Mismatch After App Update
**What goes wrong:** Deserialization fails with `JsonException` or returns wrong values
**Why it happens:** Stored state shape doesn't match current record definition
**How to avoid:**
- Wrap state in envelope with schema version
- Implement migration logic for breaking changes
- Fall back to InitialState on deserialization failure (graceful degradation per CONTEXT.md)
**Warning signs:** Works locally, fails after deployment

## Code Examples

Verified patterns from official sources:

### Storage Key Generation
```csharp
// Source: CONTEXT.md decisions - prefix + store type name
public static string GetStorageKey(Type storeType, string? customKey, string? prefix)
{
    var key = customKey ?? storeType.FullName ?? storeType.Name;
    return string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
}

// Usage: "MyApp.Bustand.CounterStore" or "MyApp.my-counter" (custom key)
```

### IJSRuntime Storage Operations
```csharp
// Source: Microsoft Learn - Blazor JS interop
// LocalStorage
await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, jsonValue);
var value = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);

// SessionStorage (same API, different object)
await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", key, jsonValue);
```

### Record Serialization
```csharp
// Source: Microsoft Learn - System.Text.Json immutability
public record CounterState(int Count, string? Label = null);

var state = new CounterState(42, "Test");
var json = JsonSerializer.Serialize(state);
// Output: {"Count":42,"Label":"Test"}

var restored = JsonSerializer.Deserialize<CounterState>(json);
// restored.Count == 42, restored.Label == "Test"
```

### Graceful Deserialization with Fallback
```csharp
// Source: CONTEXT.md decisions - corrupted data falls back to InitialState
public async Task<TState> RestoreOrDefaultAsync<TState>(string key, TState defaultState)
    where TState : class
{
    try
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json))
            return defaultState;

        var restored = JsonSerializer.Deserialize<TState>(json, _options);
        return restored ?? defaultState;
    }
    catch (JsonException ex)
    {
        // Log warning per CONTEXT.md decisions
        Console.WriteLine($"[Bustand] Failed to restore state for {key}: {ex.Message}. Using InitialState.");
        return defaultState;
    }
}
```

### Persist Attribute Definition
```csharp
// Source: CONTEXT.md decisions
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PersistAttribute : Attribute
{
    public StorageType Storage { get; }
    public string? Key { get; init; }

    public PersistAttribute(StorageType storage)
    {
        Storage = storage;
    }
}

public enum StorageType
{
    Local,   // localStorage - persists indefinitely
    Session  // sessionStorage - cleared when tab closes
}
```

### DI Registration for Persistence
```csharp
// Source: Existing Bustand DI patterns (ServiceCollectionExtensions.cs)
services.AddBustand(options =>
{
    options.StorageKeyPrefix = "MyApp";  // Global prefix
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    options.UseMiddleware<PersistenceMiddleware<>>();  // Open generic
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ProtectedBrowserStorage everywhere | Direct IJSRuntime for WASM, Protected for Server if encryption needed | Blazor 5+ | Simpler WASM implementation |
| Newtonsoft.Json | System.Text.Json | .NET Core 3.0+ | Built-in, faster, smaller |
| Synchronous JS interop | Async-only pattern | Blazor Server | Required for SignalR compatibility |
| Manual prerender detection | OnAfterRenderAsync(firstRender) | Blazor unified | Cleaner lifecycle integration |
| Circuit state = persistence | Separate concerns | .NET 10 | Circuit persistence is for reconnects, browser storage is for page reloads |

**Deprecated/outdated:**
- `Microsoft.AspNetCore.ProtectedBrowserStorage` NuGet package: Was experimental, now built into Server components
- Synchronous `ISyncLocalStorageService` in Server mode: Only works in WASM
- Manual prerender flags: Replaced by lifecycle hooks

## Open Questions

Things that couldn't be fully resolved:

1. **Scoped Store Persistence in Server Mode**
   - What we know: Scoped stores are per-circuit; browser storage is per-browser
   - What's unclear: Should persisted state be shared across circuits (same user, same browser)?
   - Recommendation: Use browser storage - state persists across circuits. Document that multiple tabs share persisted state.

2. **Auto Mode (Server -> WASM) Migration**
   - What we know: Auto mode renders Server initially, then WASM
   - What's unclear: Whether persisted state needs migration or is seamless
   - Recommendation: Browser storage is shared - should be seamless. Test and document any edge cases.

3. **Exact Debounce Timing**
   - What we know: Need balance between responsiveness and I/O reduction
   - What's unclear: Optimal timing for typical use cases
   - Recommendation: Use 300ms as default (standard UI debounce), allow configuration via options

4. **Large State Handling**
   - What we know: SignalR has 32KB limit; localStorage has 5-10MB per origin
   - What's unclear: What happens when state approaches limits
   - Recommendation: Implement size check in WASM, warn if over 100KB. Throw in Server mode if over 30KB.

## Sources

### Primary (HIGH confidence)
- Microsoft Learn - Blazor protected browser storage (ASP.NET Core 10.0): Verified ProtectedLocalStorage/ProtectedSessionStorage API
- Microsoft Learn - System.Text.Json immutability: Verified record serialization patterns
- GitHub Blazored/LocalStorage: Reference implementation patterns
- Existing Bustand codebase: LoggingMiddleware, ServiceCollectionExtensions for integration patterns

### Secondary (MEDIUM confidence)
- Thomas Claudius Huber blog: Direct IJSRuntime usage patterns (verified against MS docs)
- Jeremy Likness blog: Debounce pattern for Blazor (verified approach, implementation details may vary)
- Couchbase schema versioning: Envelope pattern concept (adapted for JSON/localStorage)

### Tertiary (LOW confidence)
- Various Stack Overflow and GitHub discussions: Helped identify pitfalls, verified through primary sources

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Using only built-in .NET/Blazor components, well-documented
- Architecture: HIGH - Patterns derived from existing Bustand code + verified Blazor patterns
- Pitfalls: HIGH - Multiple sources confirm JS interop timing and serialization issues

**Research date:** 2026-01-24
**Valid until:** 60 days (stable APIs, no breaking changes expected in .NET 10)
