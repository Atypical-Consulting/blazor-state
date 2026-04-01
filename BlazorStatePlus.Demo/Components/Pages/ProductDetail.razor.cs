using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;
using BlazorStatePlus.Demo.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorStatePlus.Demo.Components.Pages;

public partial class ProductDetail : ComponentBase
{
    [Inject] private ProductService Products { get; set; } = null!;
    [Inject] private ReviewService Reviews { get; set; } = null!;

    [Parameter]
    public int ProductId { get; set; }

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<ProductPageState> _page;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Page
           .KeySuffix(ProductId)
           .InitializeFrom(async () => new ProductPageState
           {
               Product = await Products.GetAsync(ProductId),
               Reviews = await Reviews.GetSummaryAsync(ProductId),
               IsInWishlist = await Products.IsInWishlistAsync(ProductId)
           });
    }

    private void ToggleWishlist()
    {
        var state = _page.Value;
        _page.Value = state with { IsInWishlist = !state.IsInWishlist };
    }

    public record ProductPageState
    {
        public ProductDetailDto? Product { get; init; }
        public ReviewSummary? Reviews { get; init; }
        public bool IsInWishlist { get; init; }
    }

    public record ProductDetailDto(int Id, string Name, string Description, decimal Price);
    public record ReviewSummary(int TotalCount, double AverageRating);
}
