# The `[Persist]` Attribute

`[Persist]` marks a partial property for automatic state persistence across Blazor's prerender-to-interactive transitions. The source generator emits all the serialization, restoration, and lifecycle code.

## Basic Usage

```csharp
public partial class MyComponent : ComponentBase
{
    [Persist]
    public partial string? SearchQuery { get; set; }
}
```

This single attribute gives you:
- Automatic save during prerender
- Automatic restore when the component becomes interactive
- A `SearchQueryMeta` companion object with lifecycle metadata

## TimeToLive

Control how long persisted data is considered fresh:

```csharp
[Persist(TimeToLive = "00:05:00")]  // 5 minutes
public partial DashboardData? Stats { get; set; }
```

When TTL expires:
- `StatsMeta.IsStale` returns `true`
- If an async factory is configured (via `LoadFrom`), it re-invokes automatically
- Otherwise, the stale value is still returned — you decide what to do

The format is a standard `TimeSpan` string: `"hh:mm:ss"` or `"d.hh:mm:ss"`.

## ConfigureState

The `ConfigureState` partial method is where you configure advanced persistence behavior. The generator declares it; you implement it:

```csharp
public partial class Dashboard : ComponentBase
{
    [Inject] private StatsService StatsService { get; set; } = null!;
    [Inject] public ProjectState Project { get; set; } = null!;

    [Persist(TimeToLive = "00:02:00")]
    public partial DashboardData? Stats { get; set; }

    [Persist]
    public partial int? SavedProjectId { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        // Dynamic cache key based on current project
        ctx.Stats
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(async () => await StatsService.GetDashboardAsync(Project.SelectedProject.Id));

        // Store in browser localStorage instead of the default strategy
        ctx.SavedProjectId.Storage = StorageStrategy.LocalStorage();
    }
}
```

### PropertyConfigurator API

Each property in `__StateContext` is a `PropertyConfigurator<T>` with these methods:

| Method | Description |
|---|---|
| `.KeySuffix(params object[] parts)` | Append parts to the auto-generated cache key. Useful for per-entity state. |
| `.KeyOverride(string key)` | Replace the auto-generated key entirely. |
| `.DefaultValue(T value)` | Set a default value if nothing is restored. |
| `.LoadFrom(Func<Task<T>> factory)` | Async factory called on first load and when TTL expires. |
| `.Storage` (property) | Set a per-property `IStorageStrategy` (overrides the global default). |

### Key generation

By default, the cache key is derived from the component type and property name:

```
MyApp.Components.Dashboard::Stats
```

With `.KeySuffix("project-42")`:
```
MyApp.Components.Dashboard::Stats::project-42
```

With `.KeyOverride("custom-key")`:
```
custom-key
```

## StateMeta Companion

Every `[Persist]` property gets a companion `{PropertyName}Meta` of type `StateMeta`:

```csharp
// Was this value loaded from persistence (cache, localStorage, etc.)?
if (StatsMeta.WasRestored)
{
    // Skip expensive API call — we have cached data
}

// Has the value been modified since initialization?
if (FormMeta.IsDirty)
{
    // Show "unsaved changes" warning
}

// Has the TTL expired?
if (StatsMeta.IsStale)
{
    Stats = await StatsService.Reload();
}

// When was the value last updated?
var age = DateTimeOffset.UtcNow - StatsMeta.LastUpdated;
```

### StateMeta Properties

| Property | Type | Description |
|---|---|---|
| `WasRestored` | `bool` | `true` if the value was loaded from a storage strategy |
| `IsDirty` | `bool` | `true` if the value has been modified since initialization |
| `IsStale` | `bool` | `true` if the TTL has expired (always `false` if no TTL configured) |
| `TimeToLive` | `TimeSpan?` | The configured TTL, or `null` |
| `LastUpdated` | `DateTimeOffset` | UTC timestamp of the last change or restore |
| `ChangeLog` | `IReadOnlyList<StateChangeEntry>` | Last 10 changes (newest first) |

### ChangeLog

The change log is a ring buffer of the 10 most recent mutations:

```csharp
foreach (var entry in CountMeta.ChangeLog)
{
    Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] {entry.OldValue} → {entry.NewValue} ({entry.Source})");
}
// [14:23:01] 5 → 6 (Local)
// [14:22:58] 4 → 5 (CrossTab)
// [14:22:45] 3 → 4 (Local)
```

Each `StateChangeEntry` contains:
- `Timestamp` — when the change occurred
- `OldValue` / `NewValue` — serialized as strings
- `Source` — `ChangeSource.Local` or `ChangeSource.CrossTab`

## Restoration Flow

When a component with `[Persist]` properties initializes, the generator wires up a multi-step restoration:

### `OnInitialized` (synchronous)

1. Creates `__StateContext` and calls your `ConfigureState(ctx)` partial method
2. Initializes `StateMeta` with TTL (if configured)
3. Applies defaults from `DefaultValue()`
4. Tries **PrerenderHtml** strategy (embedded in initial HTML — zero JS calls)
5. Falls back to **ServerMemoryCache** (if available)
6. Registers change handlers for persistence and cross-tab sync

### `OnInitializedAsync` (asynchronous)

1. Tries **browser strategies** (LocalStorage, SessionStorage, IndexedDB) via JS interop
2. If a value was restored and is not stale, uses it
3. If no value found or value is stale, invokes the `LoadFrom` async factory (if configured)
4. Starts the `CrossTabSyncService` listener (if using LocalStorage)

This two-phase approach ensures the component renders immediately with cached data (sync phase), then upgrades to fresh data if needed (async phase).

## Combining with [Shared]

`[Persist]` and `[Shared]` can be used together. See [The `[Shared]` Attribute](shared-attribute.md) for details on reactive state that is also persisted.
