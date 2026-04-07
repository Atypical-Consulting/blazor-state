using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TheBlazorState.Demo.Services;

namespace TheBlazorState.Demo.Components.Headless.Dialog;

public partial class DialogContent : HeadlessBase, IAsyncDisposable
{
    [CascadingParameter]
    private DialogContext Context { get; set; } = null!;

    [Inject]
    private HeadlessJsModule JsModule { get; set; } = null!;

    private bool _wasPreviouslyOpen;

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.ContentId;
        attributes["role"] = "dialog";
        attributes["aria-modal"] = "true";
        attributes["aria-labelledby"] = Context.TitleId;
        attributes["onkeydown"] =
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Context.IsOpen && !_wasPreviouslyOpen)
        {
            await JsModule.TrapFocusAsync(Context.ContentId);
            _wasPreviouslyOpen = true;
        }
        else if (!Context.IsOpen && _wasPreviouslyOpen)
        {
            await JsModule.ReleaseFocusAsync(Context.ContentId);

            if (Context.TriggerId != null)
                await JsModule.FocusElementAsync(Context.TriggerId);

            _wasPreviouslyOpen = false;
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            await Context.Close();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_wasPreviouslyOpen)
        {
            await JsModule.ReleaseFocusAsync(Context.ContentId);
        }
    }
}
