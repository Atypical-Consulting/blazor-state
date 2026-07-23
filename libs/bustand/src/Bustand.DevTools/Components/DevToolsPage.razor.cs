namespace Bustand.DevTools.Components;

/// <summary>
/// Main DevTools page component providing state inspection, history, and diff views.
/// </summary>
/// <remarks>
/// <para>
/// The DevTools page is accessible at the <c>/bustand-devtools</c> route and provides:
/// </para>
/// <list type="bullet">
/// <item>A sidebar listing all registered stores with search filtering.</item>
/// <item>Tabbed views for Current State, History, and Diff visualization.</item>
/// <item>Real-time updates when state changes via StateHistoryChanged subscription.</item>
/// </list>
/// <para>
/// <b>Environment Protection:</b> The page only renders in Development environment.
/// In other environments, an error message is displayed instead.
/// </para>
/// <para>
/// <b>Consumer Setup:</b> To enable routing to this page, add the DevTools assembly
/// to the Router's AdditionalAssemblies:
/// </para>
/// <code>
/// &lt;Router AdditionalAssemblies="new[] { typeof(DevToolsPage).Assembly }"&gt;
/// </code>
/// </remarks>
public partial class DevToolsPage : IDisposable
{
    private string? SelectedStore;
    private string ActiveTab = "state";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        DevToolsStore.StateHistoryChanged += OnStateHistoryChanged;
    }

    private void OnStateHistoryChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void SelectStore(string storeName)
    {
        SelectedStore = storeName;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DevToolsStore.StateHistoryChanged -= OnStateHistoryChanged;
    }
}
