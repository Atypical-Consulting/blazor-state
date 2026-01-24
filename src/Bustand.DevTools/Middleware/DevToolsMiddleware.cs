using Bustand.Core;
using Bustand.DevTools.Services;
using Bustand.Middleware;

namespace Bustand.DevTools.Middleware;

/// <summary>
/// Middleware that captures state changes and records them to <see cref="IDevToolsStore"/>
/// for debugging and time-travel functionality.
/// </summary>
/// <typeparam name="TState">The state type managed by the store.</typeparam>
/// <remarks>
/// <para>
/// <b>Key behaviors:</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Never blocks changes:</b> <see cref="OnBeforeChange"/> always returns <c>true</c>.
/// DevTools is a passive observer that never interferes with state updates.
/// </item>
/// <item>
/// <b>Records to DevToolsStore:</b> <see cref="OnAfterChange"/> sends state snapshots
/// to the DevTools history for inspection and time-travel.
/// </item>
/// <item>
/// <b>Time-travel aware:</b> Skips recording when <see cref="IDevToolsStore.IsTimeTraveling"/>
/// is <c>true</c> to prevent history pollution during time-travel navigation.
/// </item>
/// <item>
/// <b>Auto-registers stores:</b> On first state change, registers the store instance
/// with DevToolsStore for time-travel support.
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register via AddBustandDevTools in development:
/// if (builder.Environment.IsDevelopment())
/// {
///     builder.Services.AddBustandDevTools(builder.Environment);
/// }
/// </code>
/// </example>
public class DevToolsMiddleware<TState> : IMiddleware<TState> where TState : class
{
    private readonly IDevToolsStore _devToolsStore;
    private bool _storeRegistered;
    private object? _storeInstance;

    /// <summary>
    /// Initializes a new instance of <see cref="DevToolsMiddleware{TState}"/>.
    /// </summary>
    /// <param name="devToolsStore">The DevTools store for recording state changes.</param>
    public DevToolsMiddleware(IDevToolsStore devToolsStore)
    {
        _devToolsStore = devToolsStore ?? throw new ArgumentNullException(nameof(devToolsStore));
    }

    /// <summary>
    /// Sets the store instance for time-travel support.
    /// Called by DI factory during store construction.
    /// </summary>
    /// <param name="store">The store instance to register.</param>
    /// <remarks>
    /// This method is called via reflection by the Bustand DI factory when constructing
    /// stores with middleware. It enables time-travel functionality by providing the
    /// store instance to DevToolsStore.
    /// </remarks>
    internal void SetStoreInstance(object store)
    {
        _storeInstance = store;
    }

    /// <summary>
    /// Called before a state change is applied. Always returns <c>true</c> because
    /// DevTools never blocks state changes.
    /// </summary>
    /// <param name="context">The middleware context with state change information.</param>
    /// <returns>Always <c>true</c> to allow the state change.</returns>
    /// <remarks>
    /// DevTools is a passive observer. It records state changes for debugging purposes
    /// but never interferes with the application's state management flow.
    /// </remarks>
    public bool OnBeforeChange(MiddlewareContext<TState> context)
    {
        // DevTools never blocks state changes - it's purely observational
        return true;
    }

    /// <summary>
    /// Called after a state change has been applied. Records the change to
    /// <see cref="IDevToolsStore"/> for history tracking and time-travel debugging.
    /// </summary>
    /// <param name="context">The middleware context with state change information.</param>
    /// <remarks>
    /// <para>
    /// This method records state changes to enable:
    /// </para>
    /// <list type="bullet">
    /// <item>State history inspection in the DevTools UI.</item>
    /// <item>Time-travel debugging to replay historical states.</item>
    /// <item>Action tracking for understanding state change flow.</item>
    /// </list>
    /// <para>
    /// When <see cref="IDevToolsStore.IsTimeTraveling"/> is <c>true</c>, recording is
    /// skipped to prevent the time-travel operation itself from creating new history entries.
    /// </para>
    /// <para>
    /// On the first state change, the store is registered with DevToolsStore for time-travel.
    /// This lazy registration prevents issues with store construction order.
    /// </para>
    /// </remarks>
    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        // Don't record during time-travel to prevent duplicate/polluted history
        if (_devToolsStore.IsTimeTraveling)
        {
            return;
        }

        // Register store on first state change (lazy registration)
        if (!_storeRegistered && _storeInstance != null)
        {
            var storeName = context.StoreType.Name;

            // Access RegisterStore on DevToolsStore (internal method)
            if (_devToolsStore is DevToolsStore devToolsStore)
            {
                devToolsStore.RegisterStore(storeName, (IStore)_storeInstance);
                _storeRegistered = true;
            }
        }

        _devToolsStore.RecordStateChange(
            context.StoreType,
            context.OldState,
            context.NewState,
            context.ActionName,
            context.Timestamp);
    }
}
