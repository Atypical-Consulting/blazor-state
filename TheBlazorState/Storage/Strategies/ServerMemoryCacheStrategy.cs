namespace TheBlazorState.Storage;

internal sealed class ServerMemoryCacheStrategy : IStorageStrategy
{
    internal static readonly ServerMemoryCacheStrategy Instance = new();
    public Task<StorageResult<T>> RestoreAsync<T>(string key) => Task.FromResult(new StorageResult<T>(false, default, null));
    public Task PersistAsync<T>(string key, T value, StorageMetadata metadata) => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
}
