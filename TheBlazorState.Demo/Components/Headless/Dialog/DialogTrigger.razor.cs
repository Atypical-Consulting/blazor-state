using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class DialogTrigger : HeadlessBase
{
    [CascadingParameter]
    private DialogContext Context { get; set; } = null!;

    private string _triggerId = null!;

    protected override string DefaultTag => "button";

    protected override void OnInitialized()
    {
        _triggerId = $"dialog-trigger-{Guid.NewGuid().ToString("N")[..8]}";
        Context.TriggerId = _triggerId;
    }

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = _triggerId;
        attributes["type"] = "button";
        attributes["onclick"] =
            EventCallback.Factory.Create<MouseEventArgs>(this, Context.Open);
    }
}
