using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Accordion;

public partial class AccordionTrigger : HeadlessBase
{
    [CascadingParameter]
    private AccordionItemContext ItemContext { get; set; } = null!;

    protected override string DefaultTag => "button";

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = ItemContext.TriggerId;
        attributes["type"] = "button";
        attributes["aria-expanded"] = ItemContext.IsOpen.ToString().ToLowerInvariant();
        attributes["aria-controls"] = ItemContext.PanelId;
        attributes["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, ItemContext.Toggle);
        attributes["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown);
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (KeyboardNavigation.IsActivationKey(e.Key))
        {
            ItemContext.Toggle();
        }
    }
}
