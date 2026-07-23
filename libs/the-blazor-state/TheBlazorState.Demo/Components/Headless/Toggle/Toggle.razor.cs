using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Toggle;

public partial class Toggle : HeadlessBase
{
    [Parameter]
    public bool Value { get; set; }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    [Parameter]
    public string? Label { get; set; }

    private ToggleContext _context = null!;

    protected override string DefaultTag => "button";

    protected override void OnParametersSet()
    {
        _context = new ToggleContext(Value, HandleToggle);
    }

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["role"] = "switch";
        attributes["aria-checked"] = Value.ToString().ToLowerInvariant();
        attributes["type"] = "button";

        if (!string.IsNullOrEmpty(Label))
            attributes["aria-label"] = Label;

        attributes["onclick"] = this.OnClick(HandleToggle);
        attributes["onkeydown"] = this.OnKeyDown(HandleKeyDown);
    }

    private async Task HandleToggle()
    {
        Value = !Value;
        _context.Value = Value;
        await ValueChanged.InvokeAsync(Value);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == " ")
        {
            await HandleToggle();
        }
    }
}
