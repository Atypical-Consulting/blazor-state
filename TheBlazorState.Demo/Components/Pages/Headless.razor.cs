namespace TheBlazorState.Demo.Components.Pages;

public partial class Headless
{
    private bool DarkMode { get; set; }
    private bool Notifications { get; set; } = true;
    private bool _dialogOpen;
    private string _lastAction = "(none)";

    private void HandleEdit() => _lastAction = "Edit";
    private void HandleDuplicate() => _lastAction = "Duplicate";
    private void HandleDelete() => _lastAction = "Delete";

    private void ConfirmDelete()
    {
        _lastAction = "Deleted!";
        _dialogOpen = false;
    }
}
