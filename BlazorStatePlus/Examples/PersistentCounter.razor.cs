using BlazorStatePlus.Abstractions;

namespace BlazorStatePlus.Examples;

public partial class PersistentCounter : ComponentBase
{
    [Slice]
    private IStateSlice<int> _counter;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Counter.DefaultValue(Random.Shared.Next(100));
    }

    private void Increment() => _counter.Value++;
}
