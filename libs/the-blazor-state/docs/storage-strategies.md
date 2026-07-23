# Storage Strategies

TheBlazorState supports multiple persistence backends. Each `[Persist]` property can use a different strategy, or you can set a global default.

## Built-in Strategies

### PrerenderHtml (default)

```csharp
StorageStrategy.PrerenderHtml()
```

State is embedded in the initial HTML response during server-side prerendering and restored synchronously when the component becomes interactive. This is the fastest path — zero JavaScript calls, zero network requests.

**Survives:** Page load (prerender → interactive transition only)
**Best for:** Data fetched during SSR that shouldn't be re-fetched on hydration

### ServerMemoryCache

```csharp
StorageStrategy.ServerMemoryCache()
```

State is stored in the server's `IMemoryCache` with a 30-minute sliding expiration. Works synchronously during `OnInitialized`.

**Survives:** Navigation within the same server process
**Best for:** Server-side sessions, expensive computations

### LocalStorage

```csharp
StorageStrategy.LocalStorage()
```

State is stored in the browser's `localStorage` via JS interop. Restored asynchronously during `OnInitializedAsync`. Automatically enables **cross-tab synchronization**.

**Survives:** Browser restarts, multiple tabs
**Best for:** User preferences, persistent UI state, anything that should sync across tabs

### SessionStorage

```csharp
StorageStrategy.SessionStorage()
```

State is stored in the browser's `sessionStorage`. Scoped to a single tab. Restored asynchronously.

**Survives:** Page navigations within the same tab
**Best for:** Form drafts, wizard state, tab-specific data

### IndexedDB

```csharp
StorageStrategy.IndexedDb()
```

State is stored in the browser's IndexedDB (database name: `TheBlazorState`, store: `state`). Fully asynchronous. No practical size limit.

**Survives:** Browser restarts
**Best for:** Large datasets (>5 MB), offline-capable apps

## Choosing a Strategy

| Question | Strategy |
|---|---|
| Is it SSR hydration data? | PrerenderHtml |
| Should it persist across tabs? | LocalStorage |
| Is it tab-specific? | SessionStorage |
| Is the data large (>5 MB)? | IndexedDB |
| Is it server-side only? | ServerMemoryCache |

## Configuration

### Global default

```csharp
builder.Services.AddTheBlazorState(options =>
{
    options.DefaultStorage = StorageStrategy.ServerMemoryCache();
});
```

### Per-property override

```csharp
partial void ConfigureState(__StateContext ctx)
{
    ctx.Theme.Storage = StorageStrategy.LocalStorage();
    ctx.DraftEmail.Storage = StorageStrategy.SessionStorage();
    ctx.OfflineData.Storage = StorageStrategy.IndexedDb();
    // ctx.Stats uses the global default (no override)
}
```

### Restoration order

TheBlazorState always attempts restoration in this order:

1. **PrerenderHtml** (sync, in `OnInitialized`) — fastest, embedded in HTML
2. **ServerMemoryCache** (sync, in `OnInitialized`) — next fastest, no JS needed
3. **Browser strategies** (async, in `OnInitializedAsync`) — requires JS interop

If a property uses `LocalStorage`, the sync phase still checks PrerenderHtml and ServerMemoryCache as fallbacks. The browser restore happens in the async phase.

## Custom Strategies

Implement `IStorageStrategy` to create your own backend:

```csharp
public interface IStorageStrategy
{
    Task<StorageResult<T>> RestoreAsync<T>(string key);
    Task PersistAsync<T>(string key, T value, StorageMetadata metadata);
    Task RemoveAsync(string key);
}
```

### Supporting types

```csharp
// Returned by RestoreAsync
public record StorageResult<T>(bool Found, T? Value, DateTimeOffset? PersistedAt);

// Passed to PersistAsync
public record StorageMetadata(string Key, TimeSpan? TimeToLive, DateTimeOffset Timestamp);
```

### Example: Redis strategy

```csharp
public class RedisStorageStrategy(IConnectionMultiplexer redis) : IStorageStrategy
{
    public async Task<StorageResult<T>> RestoreAsync<T>(string key)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync($"blazorstate:{key}");

        if (value.IsNullOrEmpty)
            return new StorageResult<T>(false, default, null);

        var envelope = JsonSerializer.Deserialize<Envelope<T>>(value!);
        return new StorageResult<T>(true, envelope!.Value, envelope.PersistedAt);
    }

    public async Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
    {
        var db = redis.GetDatabase();
        var envelope = new Envelope<T>(value, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(envelope);
        var expiry = metadata.TimeToLive;
        await db.StringSetAsync($"blazorstate:{key}", json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync($"blazorstate:{key}");
    }

    private record Envelope<T>(T Value, DateTimeOffset PersistedAt);
}
```

### Register a custom strategy

```csharp
builder.Services.AddTheBlazorState(options =>
{
    options.AddStorage("redis", new RedisStorageStrategy(redis));
});
```

Use it by name:

```csharp
ctx.UserProfile.Storage = StorageStrategy.Custom("redis");
```

## Data Format

All browser strategies wrap values in a `StorageEnvelope<T>`:

```json
{
  "value": { "count": 42, "name": "Alice" },
  "persistedAt": "2026-04-08T10:30:00+00:00"
}
```

The `PersistedAt` timestamp is used for TTL calculations. JSON serialization uses `System.Text.Json` with case-insensitive deserialization.
