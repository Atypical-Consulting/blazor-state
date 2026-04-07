using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Accordion;

public partial class Accordion : HeadlessBase
{
    [Parameter]
    public bool Multiple { get; set; }

    private AccordionContext _context = default!;

    protected override void OnInitialized()
    {
        _context = new AccordionContext(Multiple, StateHasChanged);
    }
}
