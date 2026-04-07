using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Tabs;

public partial class TabList : HeadlessBase
{
    [CascadingParameter]
    private TabsContext Context { get; set; } = null!;

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["role"] = "tablist";
        attributes["onkeydown"] =
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        var newIndex = KeyboardNavigation.HandleArrowNavigation(
            e.Key, Context.SelectedIndex, Context.TabCount, vertical: false);

        if (newIndex != Context.SelectedIndex)
        {
            await Context.SelectAsync(newIndex);
        }
    }
}
