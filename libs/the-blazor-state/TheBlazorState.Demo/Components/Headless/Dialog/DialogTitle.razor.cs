using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class DialogTitle : HeadlessBase
{
    [CascadingParameter]
    private DialogContext Context { get; set; } = null!;

    protected override string DefaultTag => "h2";

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.TitleId;
    }
}
