namespace BlazorStatePlus.Abstractions;

/// <summary>
/// Configuration for a <see cref="IStateSlice{T}"/>.
/// </summary>
public class StateSliceOptions
{
    /// <summary>
    /// Unique key used to persist/restore this slice.
    /// Defaults to the property or field name if not specified.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// How long a restored value is considered "fresh".
    /// After this duration, <see cref="IStateSlice{T}.IsStale"/> returns true,
    /// signaling that the consumer should re-fetch from the source.
    /// Null means the value never goes stale.
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }
}
