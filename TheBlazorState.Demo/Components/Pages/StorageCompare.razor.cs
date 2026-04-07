using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Services;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class StorageCompare : ComponentBase
{
    [Inject] private StateInspectorService Inspector { get; set; } = default!;

    [Persist]
    public partial string? PrerenderValue { get; set; }

    [Persist]
    public partial string? ServerCacheValue { get; set; }

    [Persist]
    public partial string? SessionValue { get; set; }

    [Persist]
    public partial string? LocalValue { get; set; }

    [Persist]
    public partial string? IndexedDbValue { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.PrerenderValue.DefaultValue("(not yet set)");
        ctx.ServerCacheValue.Storage = StorageStrategy.ServerMemoryCache();
        ctx.ServerCacheValue.DefaultValue("(not yet set)");
        ctx.SessionValue.Storage = StorageStrategy.SessionStorage();
        ctx.SessionValue.DefaultValue("(not yet set)");
        ctx.LocalValue.Storage = StorageStrategy.LocalStorage();
        ctx.LocalValue.DefaultValue("(not yet set)");
        ctx.IndexedDbValue.Storage = StorageStrategy.IndexedDb();
        ctx.IndexedDbValue.DefaultValue("(not yet set)");
    }

    protected override void OnParametersSet()
    {
        Inspector.Register("StorageCompare",
        [
            new("PrerenderValue", "PrerenderHtml", PrerenderValueMeta),
            new("ServerCacheValue", "ServerMemoryCache", ServerCacheValueMeta),
            new("SessionValue", "SessionStorage", SessionValueMeta),
            new("LocalValue", "LocalStorage", LocalValueMeta),
            new("IndexedDbValue", "IndexedDB", IndexedDbValueMeta)
        ]);
    }

    private void SetAll()
    {
        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff");
        PrerenderValue = timestamp;
        ServerCacheValue = timestamp;
        SessionValue = timestamp;
        LocalValue = timestamp;
        IndexedDbValue = timestamp;
    }

    private record StrategyInfo(string Name, string Description, string Value, bool WasRestored, string AccentClass);

    private IEnumerable<StrategyInfo> Strategies =>
    [
        new("PrerenderHtml", "Survives prerender only (default)", PrerenderValue ?? "(empty)", PrerenderValueMeta.WasRestored, "bg-canvas-300 dark:bg-canvas-600"),
        new("ServerMemoryCache", "Survives page reload (server-side)", ServerCacheValue ?? "(empty)", ServerCacheValueMeta.WasRestored, "bg-indigo-400"),
        new("SessionStorage", "Survives refresh, clears on tab close", SessionValue ?? "(empty)", SessionValueMeta.WasRestored, "bg-amber-400"),
        new("LocalStorage", "Survives browser restart", LocalValue ?? "(empty)", LocalValueMeta.WasRestored, "bg-signal-400"),
        new("IndexedDB", "Survives browser restart, large data", IndexedDbValue ?? "(empty)", IndexedDbValueMeta.WasRestored, "bg-emerald-400")
    ];

}
