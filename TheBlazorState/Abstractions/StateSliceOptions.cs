namespace TheBlazorState.Abstractions;

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
    /// <para>
    /// <b>At restore time:</b> If the persisted data is older than this duration
    /// (e.g., due to CDN caching, slow connections, or tab backgrounding),
    /// the restored value is discarded and the default is used instead.
    /// </para>
    /// <para>
    /// <b>At runtime:</b> <see cref="IStateSlice{T}.IsStale"/> returns <c>true</c>
    /// when the time since the value was last updated (or originally persisted)
    /// exceeds this duration, signaling that the consumer should re-fetch.
    /// </para>
    /// <para>Null means the value never goes stale.</para>
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }
}
