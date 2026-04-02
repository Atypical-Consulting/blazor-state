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

    /// <summary>Fires whenever the property value changes.</summary>
    public event Action? OnChanged;

    internal void MarkRestored(DateTimeOffset persistedAt)
    {
        WasRestored = true;
        LastUpdated = persistedAt;
    }

    internal void MarkDirty()
    {
        IsDirty = true;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    internal void RaiseChanged() => OnChanged?.Invoke();

    internal void ClearHandlers() => OnChanged = null;
}
