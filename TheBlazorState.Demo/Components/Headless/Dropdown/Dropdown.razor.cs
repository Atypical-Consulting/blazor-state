using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TheBlazorState.Demo.Services;

namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public partial class Dropdown : HeadlessBase, IAsyncDisposable
{
    [Inject]
    private HeadlessJsModule JsModule { get; set; } = default!;

    private DropdownContext _context = default!;
    private string _id = default!;
    private DotNetObjectReference<Dropdown>? _dotNetRef;
    private bool _isOpen;

    protected override void OnInitialized()
    {
        _id = Guid.NewGuid().ToString("N")[..8];
        _context = new DropdownContext(_id, HandleOpen, HandleClose, HandleToggle, StateHasChanged);
    }

    private async Task HandleOpen()
    {
        if (_isOpen) return;
        _isOpen = true;
        _context.IsOpen = true;
        _context.FocusedIndex = -1;
        StateHasChanged();

        _dotNetRef ??= DotNetObjectReference.Create(this);
        await JsModule.OnClickOutsideAsync(_context.PanelId,
            DotNetObjectReference.Create<object>(new ClickOutsideHandler(HandleClose, StateHasChanged)));
    }

    private async Task HandleClose()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _context.IsOpen = false;
        _context.FocusedIndex = -1;
        await JsModule.RemoveClickOutsideAsync(_context.PanelId);
        StateHasChanged();
    }

    private async Task HandleToggle()
    {
        if (_isOpen)
            await HandleClose();
        else
            await HandleOpen();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isOpen)
            await JsModule.RemoveClickOutsideAsync(_context.PanelId);
        _dotNetRef?.Dispose();
    }

    public class ClickOutsideHandler(Func<Task> close, Action stateChanged)
    {
        [JSInvokable("OnClickOutside")]
        public async Task OnClickOutside()
        {
            await close();
            stateChanged();
        }
    }
}
