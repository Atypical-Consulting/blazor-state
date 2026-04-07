using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.Components.Shared;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class CrossTab : ComponentBase
{
    [Inject] public CrossTabState CrossState { get; set; } = default!;
    [Inject] private StateInspectorService Inspector { get; set; } = default!;

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

    private readonly List<string> _eventLog = [];

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // Restore shared state from persisted values
            if (SavedCounterMeta.WasRestored && SavedCounter is not null)
                CrossState.SharedCounter = SavedCounter.Value;
            if (SavedColorMeta.WasRestored && SavedColor is not null)
                CrossState.SharedColor = SavedColor;

            CrossState.SharedCounterMeta.OnChanged += OnCounterChanged;
            CrossState.SharedColorMeta.OnChanged += OnColorChanged;
        }

        Inspector.Register("CrossTab",
        [
            new("SavedCounter", "LocalStorage", SavedCounterMeta),
            new("SavedColor", "LocalStorage", SavedColorMeta)
        ]);
    }

    private void OnCounterChanged()
    {
        SavedCounter = CrossState.SharedCounter;
        _eventLog.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] Counter → {CrossState.SharedCounter}");
        if (_eventLog.Count > 10) _eventLog.RemoveRange(10, _eventLog.Count - 10);
        InvokeAsync(StateHasChanged);
    }

    private void OnColorChanged()
    {
        SavedColor = CrossState.SharedColor;
        _eventLog.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] Color → {CrossState.SharedColor}");
        if (_eventLog.Count > 10) _eventLog.RemoveRange(10, _eventLog.Count - 10);
        InvokeAsync(StateHasChanged);
    }

    private void Increment() => CrossState.SharedCounter++;
    private void Decrement() => CrossState.SharedCounter = Math.Max(0, CrossState.SharedCounter - 1);
    private void SetColor(string color) => CrossState.SharedColor = color;

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
