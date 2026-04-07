using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public partial class DropdownPanel : HeadlessBase
{
    [CascadingParameter]
    private DropdownContext Context { get; set; } = null!;

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.PanelId;
        attributes["role"] = "menu";
        attributes["aria-labelledby"] = Context.TriggerId;
        attributes["tabindex"] = "-1";
        attributes["onkeydown"] =
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                await Context.Close();
                break;
            case "ArrowDown":
            case "ArrowUp":
                var newIndex = e.Key == "ArrowDown"
                    ? Context.FindNextEnabledItem(Context.FocusedIndex, forward: true)
                    : Context.FindNextEnabledItem(Context.FocusedIndex, forward: false);
                Context.FocusedIndex = newIndex;
                StateHasChanged();
                break;
            case "Home":
                Context.FocusedIndex = Context.FindNextEnabledItem(-1, forward: true);
                StateHasChanged();
                break;
            case "End":
                Context.FocusedIndex = Context.FindNextEnabledItem(Context.ItemCount, forward: false);
                StateHasChanged();
                break;
        }
    }
}
