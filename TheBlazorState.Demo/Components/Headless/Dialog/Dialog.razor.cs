using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class Dialog : ComponentBase
{
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private DialogContext _context = default!;
    private string _id = default!;

    protected override void OnInitialized()
    {
        _id = Guid.NewGuid().ToString("N")[..8];
        _context = new DialogContext(_id, HandleOpen, HandleClose, HandleToggle);
    }

    protected override void OnParametersSet()
    {
        _context.IsOpen = Open;
    }

    private async Task HandleOpen()
    {
        Open = true;
        _context.IsOpen = true;
        await OpenChanged.InvokeAsync(true);
    }

    private async Task HandleClose()
    {
        Open = false;
        _context.IsOpen = false;
        await OpenChanged.InvokeAsync(false);
    }

    private async Task HandleToggle()
    {
        if (Open)
            await HandleClose();
        else
            await HandleOpen();
    }
}
