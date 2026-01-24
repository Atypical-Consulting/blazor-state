# Phase 1: Foundation - Research

**Researched:** 2026-01-24
**Domain:** Blazor state management library foundation (.NET 10)
**Confidence:** HIGH

## Summary

This research covers the foundational architecture for Bustand, a Zustand-inspired state management library for Blazor that must work across all rendering modes (Server, WebAssembly, Static SSR, Auto). The phase establishes project scaffolding, DI integration with auto-discovery via Scrutor, and mode-aware service lifetimes.

The primary challenge is designing a mode-agnostic core that adapts behavior based on runtime context. .NET 10's `RendererInfo` API provides component-level mode detection, while `OperatingSystem.IsBrowser()` enables service-level detection. Scrutor 7.0.0 supports attribute-based filtering with `WithAttribute<T>()` for the `[BustandStore]` discovery mechanism.

Key architectural insight: Service lifetime semantics differ fundamentally between modes. In WebAssembly, Scoped behaves as Singleton (no true DI scopes). In Server mode, Scoped means per-SignalR-circuit. The library must default to mode-appropriate lifetimes while allowing explicit overrides.

**Primary recommendation:** Build mode-agnostic abstractions from day one. Use `RendererInfo` for component behavior adaptation and `OperatingSystem.IsBrowser()` for service-level mode detection at startup.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 10 | 10.0 | Target framework | LTS release, required for unified Blazor rendering model |
| Microsoft.NET.Sdk.Razor | SDK | Project SDK | Required for Blazor component compilation, `.razor` support |
| Microsoft.AspNetCore.Components | 10.0.x | Blazor framework | Core dependency for component lifecycle, render trees |
| Scrutor | 7.0.0 | Assembly scanning | Established library for DI auto-discovery, `WithAttribute<T>()` support |
| C# 13 | 13.0 | Language | Ships with .NET 10, improved records for immutable state |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.x | DI abstractions | Service registration APIs (transitive via Scrutor) |
| xUnit | 2.9.x | Testing | Unit test framework |
| bUnit | 2.5.3 | Component testing | Blazor component testing, supports render mode mocking |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Scrutor | Manual registration | Zero dependencies but requires explicit `AddStore<T>()` calls for each store |
| Scrutor | Source generators | AOT-friendly but significant implementation complexity |

**Installation:**
```bash
# Create solution
dotnet new sln -n Bustand

# Core library (Razor class library)
dotnet new razorclasslib -n Bustand -f net10.0
dotnet sln add src/Bustand/Bustand.csproj

# Add Scrutor
dotnet add src/Bustand/Bustand.csproj package Scrutor --version 7.0.0
```

## Architecture Patterns

### Recommended Project Structure
```
Bustand/
├── src/
│   ├── Bustand/                          # Core library
│   │   ├── Bustand.csproj
│   │   ├── Core/
│   │   │   ├── ZustandStore.cs           # Abstract base class
│   │   │   ├── StoreState.cs             # State wrapper
│   │   │   └── IStore.cs                 # Store interface
│   │   ├── Attributes/
│   │   │   └── BustandStoreAttribute.cs  # [BustandStore] discovery attribute
│   │   ├── Configuration/
│   │   │   ├── BustandOptions.cs         # Configuration options
│   │   │   └── StoreLifetime.cs          # Lifetime enum
│   │   ├── Detection/
│   │   │   ├── BlazorModeDetector.cs     # Runtime mode detection
│   │   │   └── IBlazorMode.cs            # Mode abstraction
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs  # AddBustand()
│   └── Bustand.DevTools/                 # Separate package (Phase 4+)
│       └── Bustand.DevTools.csproj
├── tests/
│   └── Bustand.Tests/
│       ├── Bustand.Tests.csproj
│       └── ...
├── samples/
│   └── Bustand.Sample/
│       └── Bustand.Sample.csproj
├── Directory.Build.props                 # Shared build config
└── Bustand.sln
```

### Pattern 1: Mode-Aware Service Lifetime Registration
**What:** Register stores with lifetime based on detected Blazor mode at startup.
**When to use:** Always for `AddBustand()` registration.
**Example:**
```csharp
// Source: Microsoft DI documentation + Scrutor patterns
public static IServiceCollection AddBustand(
    this IServiceCollection services,
    Action<BustandOptions>? configure = null)
{
    var options = new BustandOptions();
    configure?.Invoke(options);

    // Detect mode at registration time
    var isWasm = OperatingSystem.IsBrowser();
    var defaultLifetime = isWasm
        ? ServiceLifetime.Singleton
        : ServiceLifetime.Scoped;

    // Scan for [BustandStore] attributed classes
    services.Scan(scan => scan
        .FromAssemblies(options.AssembliesToScan)
        .AddClasses(c => c.WithAttribute<BustandStoreAttribute>())
        .AsSelf()
        .WithLifetime(options.DefaultLifetime ?? defaultLifetime));

    return services;
}
```

### Pattern 2: Attribute-Based Store Discovery
**What:** Use custom `[BustandStore]` attribute for explicit opt-in to auto-discovery.
**When to use:** All store classes that should be auto-registered.
**Example:**
```csharp
// Source: Scrutor WithAttribute<T>() documentation
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BustandStoreAttribute : Attribute
{
    public ServiceLifetime? Lifetime { get; }

    public BustandStoreAttribute() { }

    public BustandStoreAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}

// Usage
[BustandStore]  // Uses mode-aware default
public class CounterStore : ZustandStore<CounterState> { }

[BustandStore(ServiceLifetime.Singleton)]  // Override
public class AppConfigStore : ZustandStore<AppConfigState> { }
```

### Pattern 3: Mode Detection via OperatingSystem.IsBrowser()
**What:** Detect WebAssembly vs Server at service registration time.
**When to use:** During `AddBustand()` to determine default lifetime.
**Example:**
```csharp
// Source: .NET API + community patterns
public static class BlazorModeDetector
{
    // Called once at startup
    public static bool IsWebAssembly => OperatingSystem.IsBrowser();

    // For Server mode, scoped = per-circuit (per-user)
    // For WASM, scoped = effectively singleton
    public static ServiceLifetime RecommendedStoreLifetime =>
        IsWebAssembly ? ServiceLifetime.Singleton : ServiceLifetime.Scoped;
}
```

### Pattern 4: Component-Level Mode Detection with RendererInfo
**What:** Use `RendererInfo` for component-level adaptation.
**When to use:** When components need to behave differently based on render mode.
**Example:**
```csharp
// Source: Microsoft Blazor Render Modes documentation
// Available in any ComponentBase-derived class
protected override void OnInitialized()
{
    // RendererInfo.IsInteractive: true when interactive, false during prerender/static
    // RendererInfo.Name: "Static", "Server", "WebAssembly", or "WebView"

    if (!RendererInfo.IsInteractive)
    {
        // Prerendering or static SSR - don't subscribe to state changes
        return;
    }

    // Interactive mode - set up subscriptions
    _subscription = Store.Subscribe(OnStateChanged);
}
```

### Anti-Patterns to Avoid
- **Hardcoding @rendermode in library components:** Let consuming apps specify render mode. Library components must be mode-agnostic.
- **Singleton stores in Server mode without explicit override:** Data leaks between users/circuits if state is shared.
- **Calling StateHasChanged directly from events:** Always use `InvokeAsync(StateHasChanged)` for thread safety in Server mode.
- **Global static stores:** Prevents testing, breaks DI patterns, no circuit isolation.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Assembly scanning for DI | Custom reflection code | Scrutor 7.0.0 | Handles edge cases, tested, maintained |
| Mode detection | Custom environment checks | `OperatingSystem.IsBrowser()` + `RendererInfo` | Official .NET APIs, reliable |
| Lifetime management | Custom scope handling | Microsoft.Extensions.DI scopes | Integrates with Blazor circuit lifecycle |
| Component re-render dispatch | Direct StateHasChanged | `InvokeAsync(StateHasChanged)` | Thread-safe in Server mode |

**Key insight:** Blazor's DI and component lifecycle are tightly integrated. Working with the framework (scoped services, RendererInfo) produces better results than custom solutions.

## Common Pitfalls

### Pitfall 1: Service Lifetime Mismatch Across Modes
**What goes wrong:** Scoped services in WASM behave as Singleton, but developers expect per-scope isolation. State unexpectedly shares across components.
**Why it happens:** WASM has no true DI scope concept - scoped = singleton for the app lifetime.
**How to avoid:**
1. Default to Singleton in WASM (makes behavior explicit)
2. Default to Scoped in Server (per-circuit isolation)
3. Document the difference prominently
4. Log warning if Singleton detected in Server mode (data leak risk)
**Warning signs:** State updates appear in components that shouldn't see them in WASM.

### Pitfall 2: Synchronization Context Violations
**What goes wrong:** State updates from background tasks cause `InvalidOperationException: The current thread is not associated with the Dispatcher` in Server mode.
**Why it happens:** Blazor Server uses a SynchronizationContext per circuit. StateHasChanged must run on the render thread.
**How to avoid:**
1. All state change notifications must use `InvokeAsync()`
2. Store base class should wrap notifications internally
3. Test with timers/background services in Server mode
**Warning signs:** Works in WASM but crashes in Server; intermittent threading errors.

### Pitfall 3: Forgetting IDisposable for Subscriptions
**What goes wrong:** Components subscribe to state changes but don't unsubscribe. Memory grows continuously in Server mode as circuits hold references.
**Why it happens:** Event handlers maintain strong references. Circuit-scoped services live for circuit duration (hours potentially).
**How to avoid:**
1. Document IDisposable requirement prominently
2. Consider providing base component that auto-unsubscribes
3. Subscription API must return IDisposable
**Warning signs:** Server memory grows over time; components render after disposal.

### Pitfall 4: Prerendering Double-Initialization
**What goes wrong:** OnInitialized runs during prerender AND hydration. Components initialize twice, potentially with different data.
**Why it happens:** Prerender executes on server, hydration re-executes on client (or new circuit for Server mode).
**How to avoid:**
1. Use `RendererInfo.IsInteractive` to skip initialization during prerender
2. Defer subscriptions until interactive
3. Leverage .NET 10's `[PersistentState]` for state preservation (Phase 4)
**Warning signs:** UI flashes, duplicate API calls, state resets on hydration.

### Pitfall 5: Specifying @rendermode in Library Code
**What goes wrong:** Library components that specify `@rendermode` force consuming apps into that mode, breaking flexibility.
**Why it happens:** Developer assumes their use case is universal.
**How to avoid:**
1. Never specify `@rendermode` in library components
2. Design components to work in any mode (static graceful degradation)
3. Use `RendererInfo` to adapt, not restrict
**Warning signs:** Library components fail in certain hosting models.

## Code Examples

Verified patterns from official sources:

### Store Base Class Foundation
```csharp
// Source: Zustand patterns + Blazor DI best practices
public abstract class ZustandStore<TState> where TState : class
{
    private TState _state;
    private readonly object _lock = new();

    public TState State => _state;

    // Event for subscribers - library handles dispatch internally
    public event EventHandler? StateChanged;

    protected ZustandStore(TState initialState)
    {
        _state = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    protected void Set(Func<TState, TState> mutator)
    {
        lock (_lock)
        {
            _state = mutator(_state);
        }
        // Phase 2 will add proper async notification with InvokeAsync
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

### BustandStore Attribute
```csharp
// Source: Scrutor attribute-based scanning patterns
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BustandStoreAttribute : Attribute
{
    /// <summary>
    /// Optional explicit lifetime. If null, uses mode-aware default.
    /// </summary>
    public ServiceLifetime? Lifetime { get; }

    public BustandStoreAttribute() { }

    public BustandStoreAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}
```

### AddBustand Extension Method
```csharp
// Source: Scrutor + Microsoft DI patterns
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBustand(
        this IServiceCollection services,
        Action<BustandOptions>? configure = null)
    {
        var options = new BustandOptions();
        configure?.Invoke(options);

        // Resolve assemblies to scan
        var assemblies = options.AssembliesToScan.Count > 0
            ? options.AssembliesToScan
            : new[] { Assembly.GetCallingAssembly() };

        // Determine default lifetime based on mode
        var isWasm = OperatingSystem.IsBrowser();
        var defaultLifetime = options.DefaultLifetimeOverride
            ?? (isWasm ? ServiceLifetime.Singleton : ServiceLifetime.Scoped);

        // Log warning for Singleton in Server mode
        if (!isWasm && defaultLifetime == ServiceLifetime.Singleton)
        {
            // ILogger would be resolved later - use console for startup
            Console.WriteLine(
                "[Bustand Warning] Singleton stores in Server mode may leak data between users.");
        }

        // Scan and register stores with [BustandStore] attribute
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.WithAttribute<BustandStoreAttribute>())
            .AsSelf()
            .WithLifetime(defaultLifetime));

        return services;
    }
}
```

### BustandOptions Configuration
```csharp
// Source: Standard .NET options pattern
public class BustandOptions
{
    /// <summary>
    /// Assemblies to scan for [BustandStore] attributed classes.
    /// Defaults to calling assembly if empty.
    /// </summary>
    public List<Assembly> AssembliesToScan { get; } = new();

    /// <summary>
    /// Override the mode-aware default lifetime for all stores.
    /// </summary>
    public ServiceLifetime? DefaultLifetimeOverride { get; set; }

    /// <summary>
    /// Scan the assembly containing the specified type.
    /// </summary>
    public BustandOptions ScanAssemblyContaining<T>()
    {
        AssembliesToScan.Add(typeof(T).Assembly);
        return this;
    }
}
```

### Component Subscription Pattern (Preview for Phase 2)
```csharp
// Source: Blazor synchronization context docs
@implements IDisposable
@inject CounterStore Store

<p>Count: @Store.State.Count</p>

@code {
    protected override void OnInitialized()
    {
        // Only subscribe when interactive (skip prerender)
        if (RendererInfo.IsInteractive)
        {
            Store.StateChanged += OnStateChanged;
        }
    }

    private async void OnStateChanged(object? sender, EventArgs e)
    {
        // InvokeAsync required for Server mode thread safety
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Store.StateChanged -= OnStateChanged;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| No mode detection API | `RendererInfo.IsInteractive` + `RendererInfo.Name` | .NET 8 | Components can adapt behavior per mode |
| Manual render mode detection | `OperatingSystem.IsBrowser()` | .NET 5+ | Reliable WASM detection at service level |
| Complex prerender state sync | `[PersistentState]` attribute | .NET 10 | Simplified state persistence across prerender |
| Scrutor 4.x | Scrutor 7.0.0 | 2025-11 | .NET 10 support, `WithAttribute<T>()` filtering |

**Deprecated/outdated:**
- `IJSRuntime` injection check for mode detection: Unreliable, use `OperatingSystem.IsBrowser()` instead
- Checking for `NavigationManager` differences: Not a reliable detection mechanism

## Open Questions

Things that couldn't be fully resolved:

1. **Per-Store Lifetime Override via Attribute**
   - What we know: Scrutor supports `UsingAttributes()` with `[ServiceDescriptor]`
   - What's unclear: Whether attribute-specified lifetime can override mode-aware default cleanly
   - Recommendation: Implement in Phase 1, test with custom attribute carrying lifetime. May need post-scan adjustment for stores with explicit lifetime in attribute.

2. **Mixed-Mode Component Support (.NET 10)**
   - What we know: .NET 10 allows per-component render modes in same app
   - What's unclear: How to detect the active mode for a specific component at runtime when app has mixed modes
   - Recommendation: Use `RendererInfo` at component level for adaptation. For service-level, rely on startup detection which captures the "primary" mode.

3. **Singleton Warning Mechanism**
   - What we know: Need to warn when Singleton used in Server mode
   - What's unclear: Best timing (startup vs first resolution) and mechanism (ILogger vs Console vs exception)
   - Recommendation: Console.WriteLine at registration time for Phase 1, upgrade to ILogger in Phase 2+ when logging infrastructure is clearer.

## Sources

### Primary (HIGH confidence)
- [ASP.NET Core Blazor render modes (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0) - RendererInfo, AssignedRenderMode, prerendering
- [ASP.NET Core Blazor DI (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-10.0) - Service lifetimes, circuit scoping
- [Blazor Synchronization Context](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0) - InvokeAsync patterns
- [Scrutor GitHub](https://github.com/khellang/Scrutor) - Version 7.0.0, WithAttribute<T>() API
- [NuGet: Scrutor 7.0.0](https://www.nuget.org/packages/scrutor/) - .NET 10 compatibility confirmed
- [Reusable Razor UI in class libraries](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/ui-class?view=aspnetcore-10.0) - RCL project setup

### Secondary (MEDIUM confidence)
- [Andrew Lock: Using Scrutor](https://andrewlock.net/using-scrutor-to-automatically-register-your-services-with-the-asp-net-core-di-container/) - Attribute scanning patterns
- [Thinktecture: DI Scopes in Blazor](https://www.thinktecture.com/en/blazor/dependency-injection-scopes-in-blazor/) - Circuit scope behavior
- [Rockford Lhotka: Blazor 8 Render Mode Detection](https://blog.lhotka.net/2024/03/30/Blazor-8-Render-Mode-Detection) - Mode detection patterns
- [bUnit: Render modes and RendererInfo](https://bunit.dev/docs/interaction/render-modes.html) - Testing render modes

### Tertiary (LOW confidence)
- Community articles on Scrutor attribute patterns
- Stack Overflow discussions on Blazor mode detection

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official Microsoft packages + established Scrutor library
- Architecture patterns: HIGH - Verified with official documentation
- DI/Lifetime behavior: HIGH - Microsoft DI documentation confirms mode differences
- Mode detection: HIGH - RendererInfo and OperatingSystem APIs are official
- Pitfalls: MEDIUM-HIGH - Combination of official docs and community experience

**Research date:** 2026-01-24
**Valid until:** ~2026-03-24 (stable .NET 10 patterns, 60 days)
