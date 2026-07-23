using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Components;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Services;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class CrossTab : StateComponentBase
{
    [Inject] public CrossTabState CrossState { get; set; } = null!;
    [Inject] private StateInspectorService Inspector { get; set; } = null!;

    [Persist]
    public partial int? SavedCounter { get; set; }

    [Persist]
    public partial string? SavedColor { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.SavedCounter.Storage = StorageStrategy.LocalStorage();
        ctx.SavedCounter.DefaultValue(0);
        ctx.SavedColor.Storage = StorageStrategy.LocalStorage();
        ctx.SavedColor.DefaultValue("#F97316");
    }

    private int DisplayCounter => SavedCounter ?? 0;
    private string DisplayColor => SavedColor ?? "#F97316";

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SyncToSharedState();

            // Keep shared state in sync when persisted values change.
            // No manual logging needed — StateMeta.ChangeLog handles it.
            SavedCounterMeta.OnChanged += SyncToSharedState;
            SavedColorMeta.OnChanged += SyncToSharedState;
        }

        Inspector.Register("CrossTab",
        [
            new("SavedCounter", "LocalStorage", SavedCounterMeta),
            new("SavedColor", "LocalStorage", SavedColorMeta)
        ]);
    }

    private void SyncToSharedState()
    {
        if (SavedCounter is not null)
            CrossState.SharedCounter = SavedCounter.Value;
        if (SavedColor is not null)
            CrossState.SharedColor = SavedColor;
    }

    private void Increment() => SavedCounter = (SavedCounter ?? 0) + 1;
    private void Decrement() => SavedCounter = Math.Max(0, (SavedCounter ?? 0) - 1);
    private void SetColor(string color) => SavedColor = color;

    private static readonly string[] Colors =
    [
        "#F97316", // signal/orange
        "#6366f1", // indigo
        "#10b981", // emerald
        "#f43f5e", // rose
        "#f59e0b", // amber
        "#0ea5e9", // sky
        "#8b5cf6", // violet
        "#ec4899"  // pink
    ];
}
