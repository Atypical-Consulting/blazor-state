namespace BlazorStatePlus.Attributes;

/// <summary>
/// Marks a field of type <c>IStateSlice&lt;T&gt;</c> for automatic wiring
/// by the BlazorStatePlus source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SliceAttribute : Attribute
{
    /// <summary>
    /// How long a restored value is considered fresh.
    /// Format: TimeSpan string (e.g., "00:05:00" for 5 minutes).
    /// Null means the value never goes stale.
    /// </summary>
    public string? TimeToLive { get; set; }
}
