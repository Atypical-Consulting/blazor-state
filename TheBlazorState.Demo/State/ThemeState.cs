using TheBlazorState.Attributes;

namespace TheBlazorState.Demo.State;

public partial class ThemeState
{
    [Shared]
    public partial string Theme { get; set; }

    [Shared]
    public partial string Density { get; set; }

    public ThemeState()
    {
        Theme = "light";
        Density = "comfortable";
    }
}
