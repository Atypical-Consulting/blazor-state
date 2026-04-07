using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class DialogClose : HeadlessBase
{
    [CascadingParameter]
    private DialogContext Context { get; set; } = default!;

    protected override string DefaultTag => "button";

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["type"] = "button";
        attributes["onclick"] =
            EventCallback.Factory.Create<MouseEventArgs>(this, Context.Close);
    }
}
