using BlazorStatePlus.Abstractions;

namespace BlazorStatePlus.Examples;

public partial class PersistentCounter : Components.PersistentComponentBase
{
    private IStateSlice<int> _counter = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Factory only runs during prerender; value is restored on interactive load.
        _counter = UseSlice("counter", () => Random.Shared.Next(100));
    }

    private void Increment() => _counter.Value++;
}