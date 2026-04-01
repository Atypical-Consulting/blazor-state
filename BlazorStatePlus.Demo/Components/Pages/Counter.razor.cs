using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;
using Microsoft.AspNetCore.Components;

namespace BlazorStatePlus.Demo.Components.Pages;

public partial class Counter : ComponentBase
{
    [Slice]
    private IStateSlice<int> _counter = null!;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Counter.DefaultValue(Random.Shared.Next(100));
    }

    private void Increment() => _counter.Value++;
}
