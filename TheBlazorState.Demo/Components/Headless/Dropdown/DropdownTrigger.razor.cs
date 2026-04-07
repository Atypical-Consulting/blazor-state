using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public partial class DropdownTrigger : HeadlessBase
{
    [CascadingParameter]
    private DropdownContext Context { get; set; } = default!;

    protected override string DefaultTag => "button";

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.TriggerId;
        attributes["type"] = "button";
        attributes["aria-expanded"] = Context.IsOpen.ToString().ToLowerInvariant();
        attributes["aria-haspopup"] = "true";
        attributes["aria-controls"] = Context.PanelId;
        attributes["onclick"] =
            EventCallback.Factory.Create<MouseEventArgs>(this, Context.Toggle);
    }
}
