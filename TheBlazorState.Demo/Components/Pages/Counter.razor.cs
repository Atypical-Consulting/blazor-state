using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Counter : ComponentBase
{
    [Persist]
    public partial int Count { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.Count.DefaultValue(Random.Shared.Next(100));
    }

    private void Increment() => Count++;
}
