using Bustand.Attributes;
using Bustand.Core;

namespace Bustand.Tests.TestStores;

[BustandStore]
public class CounterStore : ZustandStore<CounterState>
{
    public CounterStore() : base(new CounterState()) { }

    public void Increment() => Set(s => s with { Count = s.Count + 1 });

    public void Decrement() => Set(s => s with { Count = s.Count - 1 });

    public void SetCount(int count) => Set(s => s with { Count = count });
}
