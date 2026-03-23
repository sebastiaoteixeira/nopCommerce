using System.Diagnostics;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Stores;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;

namespace Nop.Web.Framework.Infrastructure;

/// <summary>
/// Decorator over PriceCalculationService that adds OpenTelemetry tracing
/// to GetFinalPriceAsync without modifying the original service.
/// </summary>
public class InstrumentedPriceCalculationService : PriceCalculationService
{
    public InstrumentedPriceCalculationService(
        CatalogSettings catalogSettings,
        CurrencySettings currencySettings,
        ICategoryService categoryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDiscountService discountService,
        IManufacturerService manufacturerService,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        IStaticCacheManager staticCacheManager)
        : base(catalogSettings, currencySettings, categoryService, currencyService,
            customerService, discountService, manufacturerService, productAttributeParser,
            productService, staticCacheManager)
    {
    }

    public override async Task<(decimal priceWithoutDiscounts, decimal finalPrice,
        decimal appliedDiscountAmount, List<Discount> appliedDiscounts)> GetFinalPriceAsync(
        Product product,
        Customer customer,
        Store store,
        decimal? overriddenProductPrice,
        decimal additionalCharge,
        bool includeDiscounts,
        int quantity,
        DateTime? rentalStartDate,
        DateTime? rentalEndDate)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("PriceCalculationService.GetFinalPrice");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await base.GetFinalPriceAsync(product, customer, store,
                overriddenProductPrice, additionalCharge, includeDiscounts,
                quantity, rentalStartDate, rentalEndDate);

            stopwatch.Stop();

            activity?.SetTag("pricing.product_id", product.Id);
            activity?.SetTag("pricing.includes_discounts", includeDiscounts);
            activity?.SetTag("pricing.quantity", quantity);
            activity?.SetTag("pricing.has_discount", result.appliedDiscountAmount > 0);
            activity?.SetTag("pricing.is_rental", rentalStartDate.HasValue);

            NopTelemetry.PricingDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object>("pricing.has_discount", result.appliedDiscountAmount > 0));

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
