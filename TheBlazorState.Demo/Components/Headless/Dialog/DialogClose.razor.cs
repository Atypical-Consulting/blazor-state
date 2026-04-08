using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class DialogClose : HeadlessBase
{
    [CascadingParameter]
    private DialogContext Context { get; set; } = null!;

    protected override string DefaultTag => "button";

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["type"] = "button";
        attributes["onclick"] = this.OnClick(Context.Close);
    }
}
