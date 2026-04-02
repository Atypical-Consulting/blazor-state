using TheBlazorState.Attributes;
using TheBlazorState.Demo.Services;
using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Pages;

public partial class ProductDetail : ComponentBase
{
    [Inject] private ProductService Products { get; set; } = null!;
    [Inject] private ReviewService Reviews { get; set; } = null!;

    [Parameter]
    public int ProductId { get; set; }

    [Persist(TimeToLive = "00:05:00")]
    public partial ProductPageState? Page { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.Page
           .KeySuffix(ProductId)
           .LoadFrom(async () => new ProductPageState
           {
               Product = await Products.GetAsync(ProductId),
               Reviews = await Reviews.GetSummaryAsync(ProductId),
               IsInWishlist = await Products.IsInWishlistAsync(ProductId)
           });
    }

    private void ToggleWishlist()
    {
        if (Page is null) return;
        Page = Page with { IsInWishlist = !Page.IsInWishlist };
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
