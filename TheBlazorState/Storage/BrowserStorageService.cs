using System.Text.Json;
using Microsoft.JSInterop;

namespace TheBlazorState.Storage;

/// <summary>
/// Scoped service wrapping IJSRuntime for browser storage operations.
/// Used by SessionStorage, LocalStorage, and IndexedDb strategies.
/// </summary>
public sealed class BrowserStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public BrowserStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/TheBlazorState/theblazorstate.js");
        return _module;
    }

    public async Task<StorageResult<T>> GetAsync<T>(string storeName, string key)
    {
        try
        {
            var module = await GetModuleAsync();

            JsonElement? raw;
            if (storeName == "indexedDb")
                raw = await module.InvokeAsync<JsonElement?>("getItemIndexedDb", key);
            else
                raw = await module.InvokeAsync<JsonElement?>("getItem", storeName, key);

            if (raw is null || raw.Value.ValueKind == JsonValueKind.Null || raw.Value.ValueKind == JsonValueKind.Undefined)
                return new StorageResult<T>(false, default, null);

            var envelope = JsonSerializer.Deserialize<StorageEnvelope<T>>(raw.Value.GetRawText());
            if (envelope is null)
                return new StorageResult<T>(false, default, null);

            return new StorageResult<T>(true, envelope.Value, envelope.PersistedAt);
        }
        catch
        {
            return new StorageResult<T>(false, default, null);
        }
    }

    public async Task SetAsync<T>(string storeName, string key, T value, StorageMetadata metadata)
    {
        var module = await GetModuleAsync();

        var envelope = new StorageEnvelope<T>
        {
            Value = value,
            PersistedAt = metadata.Timestamp
        };

        if (storeName == "indexedDb")
            await module.InvokeVoidAsync("setItemIndexedDb", key, envelope);
        else
            await module.InvokeVoidAsync("setItem", storeName, key, envelope);
    }

    public async Task RemoveAsync(string storeName, string key)
    {
        var module = await GetModuleAsync();

        if (storeName == "indexedDb")
            await module.InvokeVoidAsync("removeItemIndexedDb", key);
        else
            await module.InvokeVoidAsync("removeItem", storeName, key);
    }

    public sealed class StorageEnvelope<T>
    {
        public T Value { get; set; } = default!;
        public DateTimeOffset PersistedAt { get; set; }
    }
}
