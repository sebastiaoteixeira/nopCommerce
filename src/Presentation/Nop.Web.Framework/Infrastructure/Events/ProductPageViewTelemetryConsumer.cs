using Nop.Services.Catalog;
using Nop.Services.Events;

namespace Nop.Web.Framework.Infrastructure.Events;

/// <summary>
/// Records product page view telemetry when ProductDetailPageViewedEvent is published
/// </summary>
public class ProductPageViewTelemetryConsumer : IConsumer<ProductDetailPageViewedEvent>
{
    private readonly IProductService _productService;

    public ProductPageViewTelemetryConsumer(IProductService productService)
    {
        _productService = productService;
    }

    public async Task HandleEventAsync(ProductDetailPageViewedEvent eventMessage)
    {
        var product = await _productService.GetProductByIdAsync(eventMessage.ProductId);
        if (product != null)
        {
            NopTelemetry.ProductPageViews.Add(1,
                new KeyValuePair<string, object>("product.name", product.Name));
        }
    }
}
