using TheBlazorState.Attributes;

namespace TheBlazorState.Demo.State;

public partial class CrossTabState
{
    [Shared]
    public partial int SharedCounter { get; set; }

    [Shared]
    public partial string SharedColor { get; set; }

    public CrossTabState()
    {
        SharedCounter = 0;
        SharedColor = "#F97316";
    }
}
