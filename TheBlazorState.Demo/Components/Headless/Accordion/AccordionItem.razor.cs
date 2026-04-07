using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Accordion;

public partial class AccordionItem : HeadlessBase
{
    [CascadingParameter]
    private AccordionContext ParentContext { get; set; } = null!;

    [Parameter]
    public bool DefaultOpen { get; set; }

    private string _itemId = null!;
    private AccordionItemContext _itemContext = null!;

    protected override void OnInitialized()
    {
        _itemId = Guid.NewGuid().ToString("N")[..8];

        if (DefaultOpen)
            ParentContext.SetOpen(_itemId);
    }

    protected override void OnParametersSet()
    {
        _itemContext = new AccordionItemContext(
            _itemId,
            ParentContext.IsOpen(_itemId),
            () => ParentContext.Toggle(_itemId));
    }
}
