using Bustand.Attributes;
using Bustand.Core;
using Bustand.Persistence;

namespace Bustand.Tests.TestStores;

/// <summary>
/// Test store with persistence enabled.
/// </summary>
[BustandStore]
[Persist(StorageType.Local)]
public class PersistentCounterStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new(0);

    public void Increment() => Set(s => s with { Count = s.Count + 1 });
    public void SetCount(int count) => Set(s => s with { Count = count });
}

/// <summary>
/// Test store with session storage and custom key.
/// </summary>
[BustandStore]
[Persist(StorageType.Session, Key = "custom-session-counter")]
public class SessionCounterStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new(100);

    public void Increment() => Set(s => s with { Count = s.Count + 1 });
}
