using TheBlazorState.Abstractions;

namespace TheBlazorState.Services;

/// <summary>
/// Wraps a single value backed by <see cref="PersistentComponentState"/>.
/// Handles the restore-or-init dance, change notifications, and staleness.
/// </summary>
internal sealed class StateSlice<T> : IStateSlice<T>
{
    private T _value;
    private bool _disposed;

    public StateSlice(T restoredValue, bool wasRestored, StateSliceOptions options,
        DateTimeOffset? persistedAt = null)
    {
        _value = restoredValue;
        WasRestored = wasRestored;
        Options = options;
        LastUpdated = persistedAt ?? DateTimeOffset.UtcNow;
    }

    internal StateSliceOptions Options { get; }

    public T Value
    {
        get => _value;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Skip if reference/value equal to avoid unnecessary notifications
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;
            IsDirty = true;
            LastUpdated = DateTimeOffset.UtcNow;
            OnChanged?.Invoke();
        }
    }

    public bool WasRestored { get; }

    public bool IsDirty { get; private set; }

    public bool IsStale =>
        Options.TimeToLive.HasValue
        && DateTimeOffset.UtcNow - LastUpdated > Options.TimeToLive.Value;

    public DateTimeOffset LastUpdated { get; private set; }

    public event Action? OnChanged;

    public bool InitializeIfNeeded(T value)
    {
        if (WasRestored && !IsStale)
            return false;

        if (EqualityComparer<T>.Default.Equals(_value, value))
            return false;

        Value = value;
        return true;
    }

    public async Task<bool> InitializeIfNeededAsync(Func<Task<T>> factory)
    {
        if (WasRestored && !IsStale)
            return false;

        var value = await factory();

        if (EqualityComparer<T>.Default.Equals(_value, value))
            return false;

        Value = value;
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OnChanged = null;
    }
}
