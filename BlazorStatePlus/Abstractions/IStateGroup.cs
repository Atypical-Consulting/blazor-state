namespace BlazorStatePlus.Abstractions;

/// <summary>
/// Marker interface for a state group — a plain class whose public properties
/// are treated as a single serialization unit for prerender persistence.
/// 
/// Instead of persisting 5 separate keys, you persist one object.
/// This mirrors the "combine related data into single keys" principle.
/// </summary>
/// <example>
/// <code>
/// public class ProductPageState : IStateGroup
/// {
///     public ProductDetail? Product { get; set; }
///     public ReviewSummary? Reviews { get; set; }
///     public bool IsInWishlist { get; set; }
/// }
/// </code>
/// </example>
public interface IStateGroup;
