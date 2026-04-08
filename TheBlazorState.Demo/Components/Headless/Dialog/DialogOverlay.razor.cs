using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class DialogOverlay : HeadlessBase
{
    [CascadingParameter]
    private DialogContext Context { get; set; } = null!;

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["aria-hidden"] = "true";
        attributes["onclick"] = this.OnClick(Context.Close);
    }
}
