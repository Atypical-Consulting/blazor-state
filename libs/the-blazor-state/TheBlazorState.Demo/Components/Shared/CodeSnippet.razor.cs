using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Shared;

public partial class CodeSnippet
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter, EditorRequired] public string Code { get; set; } = "";
    private bool _open;
    private void Toggle() => _open = !_open;
}