using BlazorStatePlus.Abstractions;

namespace BlazorStatePlus.Examples;

public partial class ProductDetail : Components.PersistentComponentBase
{
    [Inject] private ProductService Products { get; set; } = null!;
    [Inject] private ReviewService Reviews { get; set; } = null!;

    [Parameter]
    public int ProductId { get; set; }

    private IStateSlice<ProductPageState> _page = null!;

    protected override async Task OnInitializedAsync()
    {
        base.OnInitialized();

        // One key, one JSON blob, three related data points restored atomically.
        _page = UseGroup<ProductPageState>($"product-{ProductId}");

        await _page.InitializeIfNeededAsync(async () => new ProductPageState
        {
            Product = await Products.GetAsync(ProductId),
            Reviews = await Reviews.GetSummaryAsync(ProductId),
            IsInWishlist = await Products.IsInWishlistAsync(ProductId)
        });
    }

    private void ToggleWishlist()
    {
        var state = _page.Value;
        // Mutate then reassign to trigger change notification.
        _page.Value = state with { IsInWishlist = !state.IsInWishlist };
    }

    // State group: all properties serialized/deserialized as one JSON object.
    public record ProductPageState : IStateGroup
    {
        public ProductDetailDto? Product { get; init; }
        public ReviewSummary? Reviews { get; init; }
        public bool IsInWishlist { get; init; }
    }

    // Domain types (would normally live in a shared project)
    public record ProductDetailDto(int Id, string Name, string Description, decimal Price);
    public record ReviewSummary(int TotalCount, double AverageRating);
}