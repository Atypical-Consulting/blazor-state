using Bustand.Attributes;
using Bustand.Core;

namespace Bustand.Tests.TestStores;

public record AsyncInitState(int Value = 0, bool Loaded = false);

/// <summary>
/// Test store that demonstrates async initialization.
/// </summary>
[BustandStore]
public class AsyncInitStore : ZustandStore<AsyncInitState>
{
    private readonly int _loadValue;

    protected override AsyncInitState InitialState => new AsyncInitState();

    public AsyncInitStore(int loadValue = 42)
    {
        _loadValue = loadValue;
    }

    protected override async Task InitializeAsync()
    {
        await Task.Delay(10); // Simulate async load
        Set(s => s with { Value = _loadValue, Loaded = true });
    }
}
