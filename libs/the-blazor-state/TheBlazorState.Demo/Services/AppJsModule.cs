using Microsoft.JSInterop;

namespace TheBlazorState.Demo.Services;

public sealed class AppJsModule(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module = new(
        () => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/app.module.js").AsTask());

    public async Task SetThemeClassAsync(string theme)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("setThemeClass", theme);
    }

    public async Task SetDensityClassAsync(string density)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("setDensityClass", density);
    }

    public async Task ClearAllBlazorStateAsync()
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("clearAllBlazorState");
    }

    public async Task<string?> GetStoredPreferenceAsync(string key)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<string?>("getStoredPreference", key);
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
