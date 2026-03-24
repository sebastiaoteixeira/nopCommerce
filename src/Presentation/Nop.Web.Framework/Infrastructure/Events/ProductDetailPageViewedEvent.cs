namespace Nop.Web.Framework.Infrastructure.Events;

/// <summary>
/// Event published when a product detail page is successfully viewed
/// </summary>
public class ProductDetailPageViewedEvent
{
    public ProductDetailPageViewedEvent(int productId)
    {
        ProductId = productId;
    }

    public int ProductId { get; }
}
