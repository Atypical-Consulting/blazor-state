# Phase 5: DevTools - Research

**Researched:** 2026-01-24
**Domain:** Blazor in-app developer tools - state inspection, history tracking, time-travel debugging, diff visualization
**Confidence:** HIGH

## Summary

This research covers the implementation of built-in DevTools for Bustand, providing developers with real-time state inspection, action history, time-travel debugging, and state diff visualization accessible at `/bustand-devtools`. The phase implements requirements DEVO-01 through DEVO-14.

The architecture follows a three-component design: (1) **DevToolsMiddleware** captures state changes via the existing middleware pipeline, (2) **DevToolsStore** holds the history of state snapshots and provides time-travel capabilities, and (3) **DevTools Page Components** render the UI using plain HTML/CSS. The page is delivered via a Razor Class Library (RCL) pattern, leveraging `AdditionalAssemblies` for routing.

Key architectural decisions from CONTEXT.md are validated: dark theme with sidebar+main panel layout (familiar DevTools UX), tree view for state inspection with type coloring, side-by-side diff visualization, newest-first action history limited to 100 entries, and development-only availability enforced via environment checks.

**Primary recommendation:** Implement DevToolsMiddleware that captures state changes to a DevToolsStore service, with the DevTools page subscribing to the store for real-time updates. Use System.Text.Json for state serialization and CompareNETObjects for diff detection (already in project from Phase 3).

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.Components.Web | 10.0.x | Blazor component rendering, RCL support | Official Blazor APIs |
| System.Text.Json | 10.0.x | State serialization for tree view, export | Built-in, high performance |
| CompareNETObjects | 4.84.0 | State diff detection | Already in project (Phase 3), proven for deep comparison |
| Microsoft.Extensions.Hosting | 10.0.x | IHostEnvironment for development check | Standard environment detection |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.JSInterop | 10.0.x | Clipboard API, file download | Export features (copy to clipboard, download JSON) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Plain HTML/CSS | Blazorise/MudBlazor | Would add UI framework dependency - explicitly avoided per DEVO-12 |
| Custom tree view | @alenaksu/json-viewer | Would require JS interop - keeping pure Blazor for simplicity |
| Custom diff | htmldiff.net | Over-engineered - CompareNETObjects provides sufficient diff data |

**Installation:**
No new packages required - all dependencies already in project.

## Architecture Patterns

### Recommended Project Structure
```
src/Bustand.DevTools/
+-- Extensions/
|   +-- DevToolsServiceCollectionExtensions.cs  # Existing - enhance with middleware/service registration
+-- Middleware/
|   +-- DevToolsMiddleware.cs                   # Captures state changes for history
+-- Services/
|   +-- DevToolsStore.cs                        # Holds state history, provides time-travel
|   +-- IDevToolsStore.cs                       # Interface for testability
+-- Models/
|   +-- StateSnapshot.cs                        # Immutable snapshot record
|   +-- DiffResult.cs                           # Diff between two states
+-- Components/
|   +-- DevToolsPage.razor                      # Main DevTools page (@page "/bustand-devtools")
|   +-- StoreSidebar.razor                      # Store list sidebar with search
|   +-- StateTreeView.razor                     # Recursive tree view component
|   +-- ActionHistoryPanel.razor                # History list component
|   +-- DiffViewer.razor                        # Side-by-side diff component
|   +-- JsonExporter.razor                      # Copy/download actions
+-- Styles/
|   +-- devtools.css                            # Dark theme styles (embedded in RCL)
+-- wwwroot/
    +-- Bustand.DevTools.bundle.scp.css         # Auto-generated scoped CSS bundle
```

### Pattern 1: DevTools Middleware Integration
**What:** A middleware that intercepts state changes and records them to DevToolsStore
**When to use:** Always - this is the capture mechanism for all DevTools features
**Example:**
```csharp
// Source: Existing middleware pattern from Phase 3
public class DevToolsMiddleware<TState> : IMiddleware<TState> where TState : class
{
    private readonly IDevToolsStore _devToolsStore;

    public DevToolsMiddleware(IDevToolsStore devToolsStore)
    {
        _devToolsStore = devToolsStore;
    }

    public bool OnBeforeChange(MiddlewareContext<TState> context) => true; // Never block

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        _devToolsStore.RecordStateChange(
            context.StoreType,
            context.OldState,
            context.NewState,
            context.ActionName,
            context.Timestamp);
    }
}
```

### Pattern 2: DevToolsStore as State History Manager
**What:** A scoped service that maintains state history and provides time-travel operations
**When to use:** Centralized state management for DevTools
**Example:**
```csharp
// Source: Adapted from Fluxor DevTools pattern
public interface IDevToolsStore
{
    event EventHandler? StateHistoryChanged;
    IReadOnlyList<string> RegisteredStoreNames { get; }
    IReadOnlyList<StateSnapshot> GetHistory(string storeName);
    StateSnapshot? GetCurrentSnapshot(string storeName);
    void RecordStateChange(Type storeType, object oldState, object newState, string? actionName, DateTimeOffset timestamp);
    void JumpToState(string storeName, int index);
}

public record StateSnapshot(
    int Index,
    object State,
    string? ActionName,
    DateTimeOffset Timestamp,
    string StateJson // Pre-serialized for display
);
```

### Pattern 3: Recursive Tree View Component
**What:** A component that recursively renders JSON/object structure with expand/collapse
**When to use:** State inspector panel
**Example:**
```razor
@* StateTreeView.razor - recursive rendering *@
@if (Value is JsonElement element)
{
    @switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            <div class="tree-node object">
                <span class="tree-toggle @(IsExpanded ? "expanded" : "")" @onclick="Toggle">{</span>
                @if (IsExpanded)
                {
                    <div class="tree-children">
                        @foreach (var prop in element.EnumerateObject())
                        {
                            <div class="tree-property">
                                <span class="property-name">@prop.Name:</span>
                                <StateTreeView Value="prop.Value" InitiallyExpanded="@(Depth < 1)" Depth="@(Depth + 1)" />
                            </div>
                        }
                    </div>
                }
                <span>}</span>
            </div>
            break;
        case JsonValueKind.Array:
            @* Similar pattern for arrays *@
            break;
        case JsonValueKind.String:
            <span class="value string">"@element.GetString()"</span>
            break;
        case JsonValueKind.Number:
            <span class="value number">@element.GetRawText()</span>
            break;
        case JsonValueKind.True:
        case JsonValueKind.False:
            <span class="value boolean">@element.GetBoolean().ToString().ToLower()</span>
            break;
        case JsonValueKind.Null:
            <span class="value null">null</span>
            break;
    }
}
```

### Pattern 4: RCL Page Routing
**What:** DevTools page is a routable component in the RCL, discovered via AdditionalAssemblies
**When to use:** Required for DEVO-02
**Documentation:**
```csharp
// Consumer app's App.razor or Routes.razor must include:
<Router AdditionalAssemblies="new[] { typeof(Bustand.DevTools.DevToolsPage).Assembly }">
    ...
</Router>
```
**Source:** [Microsoft Learn - Consume ASP.NET Core Razor components from RCL](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries?view=aspnetcore-10.0)

### Pattern 5: Environment-Aware Registration
**What:** DevTools services only register in Development environment; warn/fail in Production
**When to use:** DEVO-01 and DEVO-14
**Example:**
```csharp
// Source: Microsoft Blazor environment docs
public static IServiceCollection AddBustandDevTools(
    this IServiceCollection services,
    IHostEnvironment environment)
{
    if (!environment.IsDevelopment())
    {
        Console.WriteLine("[Bustand DevTools] WARNING: DevTools should only be enabled in Development. " +
            "Skipping registration for security.");
        return services;
    }

    services.AddScoped<IDevToolsStore, DevToolsStore>();
    // Note: Middleware registration handled via BustandOptions.UseDevTools()

    return services;
}

// Alternative: Throw in production if explicitly requested
public static IServiceCollection AddBustandDevTools(
    this IServiceCollection services,
    IHostEnvironment environment,
    bool throwInProduction = false)
{
    if (!environment.IsDevelopment())
    {
        if (throwInProduction)
            throw new InvalidOperationException("Bustand DevTools cannot be enabled in Production.");
        // Otherwise warn and skip
    }
    // ...
}
```

### Pattern 6: Time-Travel via Store State Injection
**What:** Time-travel works by directly setting the store's state to a historical snapshot
**When to use:** DEVO-07 and DEVO-08
**Example:**
```csharp
// In DevToolsStore
public void JumpToState(string storeName, int snapshotIndex)
{
    var snapshot = _stateHistory[storeName][snapshotIndex];

    // Find the store instance and call internal SetRestoredState
    var store = GetStoreInstance(storeName);
    if (store == null) return;

    // Use reflection to call internal SetRestoredState method
    var setMethod = store.GetType().GetMethod("SetRestoredState",
        BindingFlags.Instance | BindingFlags.NonPublic);

    // Mark as time-traveling to skip history recording
    _isTimeTraveling = true;
    try
    {
        setMethod?.Invoke(store, new[] { snapshot.State });
        // Trigger StateChanged to update UI
        var onChangedMethod = store.GetType().GetMethod("OnStateChanged",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        onChangedMethod?.Invoke(store, null);
    }
    finally
    {
        _isTimeTraveling = false;
    }

    _currentSnapshotIndex[storeName] = snapshotIndex;
    StateHistoryChanged?.Invoke(this, EventArgs.Empty);
}
```

### Anti-Patterns to Avoid
- **Recording during time-travel:** When jumping to a historical state, don't record that as a new history entry (check `_isTimeTraveling` flag)
- **Unbounded history growth:** Always enforce the 100-action limit per CONTEXT.md
- **Blocking middleware:** DevToolsMiddleware should never return false from OnBeforeChange
- **Production exposure:** Never register DevTools routes/services without environment check
- **Synchronous state serialization:** For large states, serialize lazily to avoid blocking

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Object diff detection | Custom reflection-based differ | CompareNETObjects | Already in project, handles circular refs, collections, 275+ edge cases |
| JSON serialization | Manual property iteration | System.Text.Json | Built-in, fast, handles all edge cases |
| Environment detection | Custom config checks | IHostEnvironment.IsDevelopment() | Official API, works in all Blazor modes |
| Deep copy for snapshots | Manual cloning | JSON round-trip serialization | Handles nested objects correctly |
| Tree view collapse state | Global state tracking | Component-local state | Each node manages its own expansion |

**Key insight:** The existing middleware pipeline (Phase 3) and state serialization patterns already handle the hard parts. DevTools is primarily UI work with a thin middleware layer.

## Common Pitfalls

### Pitfall 1: Memory Growth from Unbounded History
**What goes wrong:** DevToolsStore accumulates unlimited state snapshots, causing memory exhaustion in long sessions
**Why it happens:** No limit enforcement on history list
**How to avoid:**
1. Enforce 100-entry limit per CONTEXT.md decision
2. Remove oldest entry when limit exceeded
3. Consider size-based limit for large states
**Warning signs:** Browser/server memory grows continuously during development

### Pitfall 2: Circular Reference in State Serialization
**What goes wrong:** JSON serialization throws or hangs on state with circular references
**Why it happens:** User's state has circular object references
**How to avoid:**
1. Use `ReferenceHandler.Preserve` in JsonSerializerOptions
2. Catch serialization exceptions and display error message
3. CompareNETObjects handles circular refs for diff
**Warning signs:** DevTools crashes or hangs when selecting certain stores

### Pitfall 3: Real-Time Updates Not Working
**What goes wrong:** DevTools page doesn't update when state changes
**Why it happens:** DevTools page not subscribed to DevToolsStore.StateHistoryChanged
**How to avoid:**
1. Subscribe in OnInitialized with IDisposable cleanup
2. Use InvokeAsync(StateHasChanged) for thread safety
3. Test with rapid state changes
**Warning signs:** Must manually refresh to see new state

### Pitfall 4: Time-Travel Causes Duplicate History Entries
**What goes wrong:** Jumping to historical state adds a new history entry
**Why it happens:** Middleware records the "restored" state as a new change
**How to avoid:**
1. Set `_isTimeTraveling` flag before state restoration
2. Check flag in DevToolsMiddleware.OnAfterChange, skip if true
3. Clear flag after restoration complete
**Warning signs:** History grows when clicking historical entries

### Pitfall 5: Diff View Shows Everything Changed
**What goes wrong:** Diff highlights entire state as changed even for small updates
**Why it happens:** Reference comparison on nested objects, or state not using records properly
**How to avoid:**
1. Use CompareNETObjects with proper configuration
2. Document that state should use records with `with` expressions
3. Consider limiting diff depth for large states
**Warning signs:** Every field shows as modified

### Pitfall 6: DevTools Route Accessible in Production
**What goes wrong:** `/bustand-devtools` route works in production, exposing application state
**Why it happens:** Developer forgot to conditionally register DevTools
**How to avoid:**
1. AddBustandDevTools requires IHostEnvironment parameter
2. Fails/warns if not Development environment
3. Documentation emphasizes conditional registration
**Warning signs:** Route accessible without Development environment

### Pitfall 7: Store Selection Lost on Real-Time Update
**What goes wrong:** Selected store deselects when new state arrives
**Why it happens:** Component re-renders from root, losing selection state
**How to avoid:**
1. Selection state stored in component, not derived from props
2. Use @key on list items for stable identity
3. Don't re-render sidebar on main panel updates
**Warning signs:** Selection flickers or resets during state changes

## Code Examples

Verified patterns from official sources:

### DevTools Page Layout
```razor
@* DevToolsPage.razor *@
@page "/bustand-devtools"
@implements IDisposable
@inject IDevToolsStore DevToolsStore
@inject IHostEnvironment Environment

@if (!Environment.IsDevelopment())
{
    <div class="devtools-error">
        <h2>DevTools Unavailable</h2>
        <p>Bustand DevTools is only available in Development environment.</p>
    </div>
}
else
{
    <div class="devtools-container">
        <aside class="devtools-sidebar">
            <div class="sidebar-header">
                <h2>Stores</h2>
                <input type="search" @bind="SearchFilter" @bind:event="oninput"
                       placeholder="Filter stores..." class="store-search" />
            </div>
            <ul class="store-list">
                @foreach (var storeName in FilteredStores)
                {
                    var snapshot = DevToolsStore.GetCurrentSnapshot(storeName);
                    <li @key="storeName"
                        class="store-item @(SelectedStore == storeName ? "selected" : "")"
                        @onclick="() => SelectStore(storeName)">
                        <span class="store-name">@storeName</span>
                        <span class="store-timestamp">@(snapshot?.Timestamp.ToString("HH:mm:ss"))</span>
                    </li>
                }
            </ul>
        </aside>

        <main class="devtools-main">
            @if (SelectedStore != null)
            {
                <div class="tab-bar">
                    <button class="tab @(ActiveTab == "state" ? "active" : "")"
                            @onclick='() => ActiveTab = "state"'>Current State</button>
                    <button class="tab @(ActiveTab == "history" ? "active" : "")"
                            @onclick='() => ActiveTab = "history"'>History</button>
                    <button class="tab @(ActiveTab == "diff" ? "active" : "")"
                            @onclick='() => ActiveTab = "diff"'>Diff View</button>
                </div>

                @switch (ActiveTab)
                {
                    case "state":
                        <StateInspectorPanel StoreName="@SelectedStore" />
                        break;
                    case "history":
                        <ActionHistoryPanel StoreName="@SelectedStore"
                                           OnSnapshotSelected="JumpToSnapshot" />
                        break;
                    case "diff":
                        <DiffViewerPanel StoreName="@SelectedStore" />
                        break;
                }
            }
            else
            {
                <div class="empty-state">
                    <p>Select a store to inspect</p>
                </div>
            }
        </main>
    </div>
}

@code {
    private string? SelectedStore;
    private string ActiveTab = "state";
    private string SearchFilter = "";

    private IEnumerable<string> FilteredStores =>
        string.IsNullOrWhiteSpace(SearchFilter)
            ? DevToolsStore.RegisteredStoreNames
            : DevToolsStore.RegisteredStoreNames
                .Where(n => n.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

    protected override void OnInitialized()
    {
        DevToolsStore.StateHistoryChanged += OnStateHistoryChanged;
    }

    private void OnStateHistoryChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void SelectStore(string storeName)
    {
        SelectedStore = storeName;
    }

    private void JumpToSnapshot(int index)
    {
        if (SelectedStore != null)
        {
            DevToolsStore.JumpToState(SelectedStore, index);
        }
    }

    public void Dispose()
    {
        DevToolsStore.StateHistoryChanged -= OnStateHistoryChanged;
    }
}
```

### Dark Theme CSS
```css
/* devtools.css - embedded in RCL wwwroot */
:root {
    --devtools-bg: #1e1e1e;
    --devtools-sidebar-bg: #252526;
    --devtools-border: #3c3c3c;
    --devtools-text: #cccccc;
    --devtools-text-dim: #808080;
    --devtools-accent: #0e639c;
    --devtools-accent-hover: #1177bb;
    --devtools-selected: #094771;

    /* Type colors */
    --color-string: #ce9178;
    --color-number: #b5cea8;
    --color-boolean: #569cd6;
    --color-null: #808080;
    --color-property: #9cdcfe;

    /* Diff colors */
    --diff-added: #2ea043;
    --diff-added-bg: rgba(46, 160, 67, 0.15);
    --diff-removed: #f85149;
    --diff-removed-bg: rgba(248, 81, 73, 0.15);
    --diff-modified: #d29922;
    --diff-modified-bg: rgba(210, 153, 34, 0.15);
}

.devtools-container {
    display: flex;
    height: 100vh;
    background: var(--devtools-bg);
    color: var(--devtools-text);
    font-family: 'Segoe UI', Consolas, monospace;
    font-size: 13px;
}

.devtools-sidebar {
    width: 280px;
    min-width: 280px;
    background: var(--devtools-sidebar-bg);
    border-right: 1px solid var(--devtools-border);
    display: flex;
    flex-direction: column;
}

.store-list {
    list-style: none;
    margin: 0;
    padding: 0;
    overflow-y: auto;
    flex: 1;
}

.store-item {
    padding: 8px 12px;
    cursor: pointer;
    display: flex;
    justify-content: space-between;
    border-bottom: 1px solid var(--devtools-border);
}

.store-item:hover {
    background: var(--devtools-accent-hover);
}

.store-item.selected {
    background: var(--devtools-selected);
}

/* Tree view styles */
.tree-node {
    margin-left: 16px;
}

.tree-toggle {
    cursor: pointer;
    user-select: none;
}

.tree-toggle::before {
    content: '+';
    margin-right: 4px;
    color: var(--devtools-text-dim);
}

.tree-toggle.expanded::before {
    content: '-';
}

.property-name {
    color: var(--color-property);
}

.value.string { color: var(--color-string); }
.value.number { color: var(--color-number); }
.value.boolean { color: var(--color-boolean); }
.value.null { color: var(--color-null); font-style: italic; }

/* Diff view */
.diff-container {
    display: flex;
    gap: 16px;
}

.diff-panel {
    flex: 1;
    padding: 12px;
    background: var(--devtools-sidebar-bg);
    border-radius: 4px;
    overflow: auto;
}

.diff-added { background: var(--diff-added-bg); border-left: 3px solid var(--diff-added); }
.diff-removed { background: var(--diff-removed-bg); border-left: 3px solid var(--diff-removed); }
.diff-modified { background: var(--diff-modified-bg); border-left: 3px solid var(--diff-modified); }
```

### DevToolsStore Implementation
```csharp
// Source: Fluxor DevTools pattern + project conventions
public class DevToolsStore : IDevToolsStore
{
    private readonly Dictionary<string, List<StateSnapshot>> _history = new();
    private readonly Dictionary<string, int> _currentIndex = new();
    private readonly Dictionary<string, IStore> _stores = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxHistoryLength = 100;
    private bool _isTimeTraveling;

    public event EventHandler? StateHistoryChanged;

    public IReadOnlyList<string> RegisteredStoreNames => _history.Keys.ToList();

    public DevToolsStore()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void RecordStateChange(
        Type storeType,
        object oldState,
        object newState,
        string? actionName,
        DateTimeOffset timestamp)
    {
        if (_isTimeTraveling) return;

        var storeName = storeType.Name;

        if (!_history.ContainsKey(storeName))
        {
            _history[storeName] = new List<StateSnapshot>();
            _currentIndex[storeName] = -1;
        }

        var history = _history[storeName];

        // Truncate if we were time-traveling and now making new changes
        var currentIdx = _currentIndex[storeName];
        if (currentIdx < history.Count - 1)
        {
            history.RemoveRange(currentIdx + 1, history.Count - currentIdx - 1);
        }

        // Enforce max history length
        if (history.Count >= _maxHistoryLength)
        {
            history.RemoveAt(0);
        }

        // Create snapshot
        string stateJson;
        try
        {
            stateJson = JsonSerializer.Serialize(newState, _jsonOptions);
        }
        catch (Exception ex)
        {
            stateJson = $"{{\"error\": \"Serialization failed: {ex.Message}\"}}";
        }

        var snapshot = new StateSnapshot(
            Index: history.Count,
            State: newState,
            ActionName: actionName,
            Timestamp: timestamp,
            StateJson: stateJson
        );

        history.Add(snapshot);
        _currentIndex[storeName] = history.Count - 1;

        StateHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<StateSnapshot> GetHistory(string storeName)
    {
        return _history.TryGetValue(storeName, out var history)
            ? history.AsReadOnly()
            : Array.Empty<StateSnapshot>();
    }

    public StateSnapshot? GetCurrentSnapshot(string storeName)
    {
        if (!_history.TryGetValue(storeName, out var history) || history.Count == 0)
            return null;

        var idx = _currentIndex.GetValueOrDefault(storeName, history.Count - 1);
        return history[idx];
    }

    public void JumpToState(string storeName, int index)
    {
        if (!_history.TryGetValue(storeName, out var history))
            return;
        if (index < 0 || index >= history.Count)
            return;
        if (!_stores.TryGetValue(storeName, out var store))
            return;

        var snapshot = history[index];

        _isTimeTraveling = true;
        try
        {
            // Use reflection to set state (SetRestoredState is internal)
            var setMethod = store.GetType().GetMethod("SetRestoredState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            setMethod?.Invoke(store, new[] { snapshot.State });

            // Trigger notification
            var onChangedMethod = store.GetType().GetMethod("OnStateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Protected);
            onChangedMethod?.Invoke(store, null);
        }
        finally
        {
            _isTimeTraveling = false;
        }

        _currentIndex[storeName] = index;
        StateHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void RegisterStore(string storeName, IStore store)
    {
        _stores[storeName] = store;
    }
}
```

### Export to Clipboard/File
```csharp
// JsonExporter.razor.cs
public partial class JsonExporter : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Parameter] public string Json { get; set; } = "";
    [Parameter] public string FileName { get; set; } = "state.json";

    private async Task CopyToClipboard()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", Json);
    }

    private async Task DownloadFile()
    {
        // Create a data URL and trigger download
        var dataUrl = $"data:application/json;charset=utf-8,{Uri.EscapeDataString(Json)}";
        await JS.InvokeVoidAsync("eval", $@"
            var a = document.createElement('a');
            a.href = '{dataUrl}';
            a.download = '{FileName}';
            a.click();
        ");
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Redux DevTools browser extension | In-app DevTools page | Design decision | No extension install, works everywhere |
| Complex JS interop for tree view | Pure Blazor recursive component | Project constraint | Simpler, no JS dependencies |
| Manual JSON formatting | System.Text.Json WriteIndented | .NET 6+ | Built-in, configurable |
| Custom diff implementation | CompareNETObjects | Already in project | Proven, handles edge cases |

**Deprecated/outdated:**
- External browser extension: Bustand intentionally uses in-app page per PROJECT.md design decision
- jQuery-based tree views: Pure Blazor is preferred for component consistency

## Open Questions

Things that couldn't be fully resolved:

1. **Time-Travel Mode Behavior**
   - What we know: User can click history to jump to state
   - What's unclear: Should subsequent actions branch history or reset to current?
   - Recommendation: Branch behavior (truncate future, start new branch) - standard time-travel pattern. CONTEXT.md delegates to Claude's discretion.

2. **Store Instance Access for Time-Travel**
   - What we know: Need to set state on actual store instance
   - What's unclear: Best way to get reference to DI-registered stores
   - Recommendation: DevToolsStore maintains dictionary of registered stores (populated during middleware setup) or use IServiceProvider to resolve stores by type.

3. **Large State Performance**
   - What we know: 100 snapshots of large states could use significant memory
   - What's unclear: Acceptable size threshold
   - Recommendation: Document recommendation (states under 100KB), consider lazy serialization, potentially add size warning.

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - Blazor environments](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/environments?view=aspnetcore-10.0) - IHostEnvironment.IsDevelopment() pattern
- [Microsoft Learn - Blazor RCL routing](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries?view=aspnetcore-10.0) - AdditionalAssemblies pattern
- [Microsoft Learn - System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to) - Serialization with WriteIndented
- [CompareNETObjects GitHub](https://github.com/GregFinzer/Compare-Net-Objects) - Object diff API (already in project)

### Secondary (MEDIUM confidence)
- [Fluxor Redux DevTools Tutorial](https://github.com/mrpmorris/Fluxor/blob/master/Source/Tutorials/02-Blazor/02D-ReduxDevToolsTutorial/README.md) - DevTools middleware pattern
- [EasyAppDev.Blazor.Store](https://github.com/mashrulhaque/EasyAppDev.Blazor.Store) - Zustand-inspired Blazor library with DevTools
- [jsonTreeViewer](https://github.com/summerstyle/jsonTreeViewer) - Tree view expand/collapse pattern
- [json-diff-viewer-component](https://github.com/metaory/json-diff-viewer-component) - Side-by-side diff theming

### Tertiary (LOW confidence)
- CSS Script tree view libraries - UI pattern reference
- Community blog posts on Blazor DevTools patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Using existing project libraries (CompareNETObjects) and built-in .NET (System.Text.Json)
- Architecture: HIGH - Based on proven Fluxor pattern adapted for Bustand middleware
- UI patterns: MEDIUM - Tree view and diff patterns from community libraries, adapted to pure Blazor
- Environment protection: HIGH - Official Microsoft IHostEnvironment pattern
- Time-travel: MEDIUM - Standard pattern but implementation details may need refinement

**Research date:** 2026-01-24
**Valid until:** 2026-02-24 (30 days - stable patterns, UI-focused phase)
