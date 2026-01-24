using Bustand.Core;

namespace Bustand.Tests.TestStores;

// Note: No [BustandStore] attribute - should NOT be auto-registered
public record UnattributedState(string Value = "");

public class UnattributedStore : ZustandStore<UnattributedState>
{
    public UnattributedStore() : base(new UnattributedState()) { }
}
