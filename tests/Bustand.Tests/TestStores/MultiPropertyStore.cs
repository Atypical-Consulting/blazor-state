using Bustand.Attributes;
using Bustand.Core;

namespace Bustand.Tests.TestStores;

public record MultiPropertyState(int Count = 0, string Name = "");

/// <summary>
/// Test store with multiple properties for testing selector-based subscriptions.
/// </summary>
[BustandStore]
public class MultiPropertyStore : ZustandStore<MultiPropertyState>
{
    protected override MultiPropertyState InitialState => new();

    public void SetCount(int count) => Set(s => s with { Count = count });

    public void SetName(string name) => Set(s => s with { Name = name });
}
