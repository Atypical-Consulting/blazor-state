using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public partial class DropdownTrigger : HeadlessBase
{
    [CascadingParameter]
    private DropdownContext Context { get; set; } = null!;

    protected override string DefaultTag => "button";

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.TriggerId;
        attributes["type"] = "button";
        attributes["aria-expanded"] = Context.IsOpen.ToString().ToLowerInvariant();
        attributes["aria-haspopup"] = "true";
        attributes["aria-controls"] = Context.PanelId;
        attributes["onclick"] = this.OnClick(Context.Toggle);
    }
}
