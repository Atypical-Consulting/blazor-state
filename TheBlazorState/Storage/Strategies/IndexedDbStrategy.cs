namespace TheBlazorState.Storage;

internal sealed class IndexedDbStrategy : IStorageStrategy
{
    internal static readonly IndexedDbStrategy Instance = new();
    public Task<StorageResult<T>> RestoreAsync<T>(string key)
        => throw new NotImplementedException("IndexedDb requires browser JSInterop. Coming in a future release.");
    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata)
        => throw new NotImplementedException("IndexedDb requires browser JSInterop. Coming in a future release.");
    public Task RemoveAsync(string key)
        => throw new NotImplementedException("IndexedDb requires browser JSInterop. Coming in a future release.");
}
