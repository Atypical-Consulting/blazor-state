namespace TheBlazorState.Demo.Components.Headless.Accordion;

public class AccordionItemContext
{
    public string ItemId { get; }
    public bool IsOpen { get; }
    public Action Toggle { get; }
    public string TriggerId => $"accordion-trigger-{ItemId}";
    public string PanelId => $"accordion-panel-{ItemId}";

    public AccordionItemContext(string itemId, bool isOpen, Action toggle)
    {
        ItemId = itemId;
        IsOpen = isOpen;
        Toggle = toggle;
    }
}
