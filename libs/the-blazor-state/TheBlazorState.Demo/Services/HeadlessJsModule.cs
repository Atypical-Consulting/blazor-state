using Microsoft.JSInterop;

namespace TheBlazorState.Demo.Services;

public sealed class HeadlessJsModule(IJSRuntime jsRuntime) : IAsyncDisposable, IDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module = new(
        () => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/headless.module.js").AsTask());

    public async Task TrapFocusAsync(string elementId)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("trapFocus", elementId);
    }

    public async Task ReleaseFocusAsync(string elementId)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("releaseFocus", elementId);
    }

    public async Task OnClickOutsideAsync(string elementId, DotNetObjectReference<object> dotNetRef)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("onClickOutside", elementId, dotNetRef);
    }

    public async Task RemoveClickOutsideAsync(string elementId)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("removeClickOutside", elementId);
    }

    public async Task FocusElementAsync(string elementId)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("focusElement", elementId);
    }

    public void Dispose()
    {
        // Sync dispose fallback for DI containers that don't support IAsyncDisposable
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
    }
}
