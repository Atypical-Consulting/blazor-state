namespace TheBlazorState.Abstractions;

/// <summary>
/// Metadata companion for a [Persist] or [Shared] property.
/// Generated code creates one instance per annotated property.
/// </summary>
public sealed class StateMeta
{
    private readonly TimeSpan? _ttl;

    public StateMeta(TimeSpan? ttl)
    {
        _ttl = ttl;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>True if the value was restored from persistence.</summary>
    public bool WasRestored { get; private set; }

    /// <summary>True if the value has been modified since initialization.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>True if the value has exceeded its configured TTL.</summary>
    public bool IsStale =>
        _ttl.HasValue && DateTimeOffset.UtcNow - LastUpdated > _ttl.Value;

    /// <summary>The configured time-to-live, or null if none.</summary>
    public TimeSpan? TimeToLive => _ttl;

    /// <summary>UTC timestamp of last value change or restore.</summary>
    public DateTimeOffset LastUpdated { get; private set; }

    /// <summary>
    /// When true, the next <see cref="RaiseChanged"/> call should NOT trigger
    /// persistence side-effects (e.g. writing back to browser storage).
    /// Used by cross-tab sync to prevent infinite feedback loops.
    /// Reset automatically after <see cref="RaiseChanged"/> fires.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public bool SuppressPersist { get; set; }

    /// <summary>Fires whenever the property value changes.</summary>
    public event Action? OnChanged;

    /// <summary>Called by generated code to record that a value was restored from persistence.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void MarkRestored(DateTimeOffset persistedAt)
    {
        WasRestored = true;
        LastUpdated = persistedAt;
    }

    /// <summary>Called by generated code when the property value changes.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void MarkDirty()
    {
        IsDirty = true;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>Called by generated code to raise the OnChanged event.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void RaiseChanged()
    {
        OnChanged?.Invoke();
        SuppressPersist = false;
    }

    /// <summary>Called by generated code during Dispose to detach all handlers.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void ClearHandlers() => OnChanged = null;
}
