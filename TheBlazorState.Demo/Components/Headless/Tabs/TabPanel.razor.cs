using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Tabs;

public partial class TabPanel : HeadlessBase
{
    [CascadingParameter]
    private TabsContext Context { get; set; } = default!;

    private int _index;

    protected override void OnInitialized()
    {
        _index = Context.RegisterPanel();
    }

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.PanelId(_index);
        attributes["role"] = "tabpanel";
        attributes["aria-labelledby"] = Context.TabId(_index);
        attributes["tabindex"] = "0";
    }
}
