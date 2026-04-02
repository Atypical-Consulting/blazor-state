namespace TheBlazorState.Storage;

internal sealed class LocalStorageStrategy : IStorageStrategy
{
    internal static readonly LocalStorageStrategy Instance = new();
    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => throw new NotImplementedException("LocalStorage requires browser JSInterop. Coming in a future release.");
    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => throw new NotImplementedException("LocalStorage requires browser JSInterop. Coming in a future release.");
    public Task RemoveAsync(string key)
        => throw new NotImplementedException("LocalStorage requires browser JSInterop. Coming in a future release.");
}
