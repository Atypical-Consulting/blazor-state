namespace TheBlazorState.Demo.Components.Headless.Accordion;

public class AccordionItemContext(string itemId, bool isOpen, Action toggle)
{
    public string ItemId { get; } = itemId;
    public bool IsOpen { get; } = isOpen;
    public Action Toggle { get; } = toggle;
    public string TriggerId => $"accordion-trigger-{ItemId}";
    public string PanelId => $"accordion-panel-{ItemId}";
}
