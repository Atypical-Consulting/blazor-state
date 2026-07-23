using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Accordion;

public partial class AccordionPanel : HeadlessBase
{
    [CascadingParameter]
    private AccordionItemContext ItemContext { get; set; } = null!;

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = ItemContext.PanelId;
        attributes["role"] = "region";
        attributes["aria-labelledby"] = ItemContext.TriggerId;
    }
}
