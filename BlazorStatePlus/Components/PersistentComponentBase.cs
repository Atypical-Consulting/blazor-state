using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Services;

namespace BlazorStatePlus.Components;

/// <summary>
/// Base component class that provides a <see cref="StateManager"/> instance
/// and auto-triggers <see cref="StateHasChanged"/> when any slice changes.
/// 
/// Inherit from this instead of <see cref="ComponentBase"/> to get
/// automatic persistent state management.
/// </summary>
public abstract class PersistentComponentBase : ComponentBase, IDisposable
{
    [Inject]
    private PersistentComponentState Persistence { get; set; } = null!;

    private StateManager? _stateManager;
    private readonly List<IDisposable> _sliceDisposables = [];

    /// <summary>
    /// The state manager for this component. Use it to create slices.
    /// Available from <see cref="OnInitialized"/> onward.
    /// </summary>
    protected StateManager State => _stateManager
        ?? throw new InvalidOperationException(
            "StateManager is not available until OnInitialized.");

    protected override void OnInitialized()
    {
        _stateManager = new StateManager(Persistence);
    }

    /// <summary>
    /// Creates a slice and automatically subscribes to change notifications
    /// so that <see cref="ComponentBase.StateHasChanged"/> is called
    /// whenever the slice value changes.
    /// </summary>
    protected IStateSlice<T> UseSlice<T>(
        string key,
        T defaultValue = default!,
        Action<StateSliceOptions>? configure = null)
    {
        var slice = State.CreateSlice(key, defaultValue, configure);
        SubscribeToSlice(slice);
        return slice;
    }

    /// <summary>
    /// Creates a slice, initializes it with a sync factory if needed,
    /// and subscribes to changes.
    /// </summary>
    protected IStateSlice<T> UseSlice<T>(
        string key,
        Func<T> factory,
        Action<StateSliceOptions>? configure = null)
    {
        var slice = State.CreateAndInit(key, factory, configure);
        SubscribeToSlice(slice);
        return slice;
    }

    /// <summary>
    /// Creates a state group slice and subscribes to changes.
    /// </summary>
    protected IStateSlice<TGroup> UseGroup<TGroup>(
        string key,
        TGroup? defaultValue = null,
        Action<StateSliceOptions>? configure = null)
        where TGroup : class, IStateGroup, new()
    {
        var slice = State.CreateGroup(key, defaultValue, configure);
        SubscribeToSlice(slice);
        return slice;
    }

    private void SubscribeToSlice<T>(IStateSlice<T> slice)
    {
        slice.OnChanged += OnSliceChanged;
        _sliceDisposables.Add(slice);
    }

    private void OnSliceChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        foreach (var d in _sliceDisposables)
            d.Dispose();

        _sliceDisposables.Clear();
        _stateManager?.Dispose();

        GC.SuppressFinalize(this);
    }
}
