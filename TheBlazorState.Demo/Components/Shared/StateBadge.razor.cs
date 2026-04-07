using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Shared;

public partial class StateBadge
{
    [Parameter, EditorRequired] public bool Value { get; set; }
    [Parameter] public string TrueLabel { get; set; } = "Yes";
    [Parameter] public string FalseLabel { get; set; } = "No";

    [Parameter]
    public string TrueClass { get; set; } =
        "bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400";

    [Parameter]
    public string FalseClass { get; set; } = "bg-canvas-100 dark:bg-canvas-800 text-canvas-500 dark:text-canvas-500";
}