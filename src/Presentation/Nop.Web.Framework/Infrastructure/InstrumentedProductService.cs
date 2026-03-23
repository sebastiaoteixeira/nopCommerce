using System.Diagnostics;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;

namespace Nop.Web.Framework.Infrastructure;

/// <summary>
/// Decorator over ProductService that adds OpenTelemetry tracing and metrics
/// to SearchProductsAsync without modifying the original service.
/// </summary>
public class InstrumentedProductService : ProductService
{
    public InstrumentedProductService(
        CatalogSettings catalogSettings,
        IAclService aclService,
        ICustomerService customerService,
        IDateRangeService dateRangeService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IRepository<Category> categoryRepository,
        IRepository<CrossSellProduct> crossSellProductRepository,
        IRepository<DiscountProductMapping> discountProductMappingRepository,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IRepository<Manufacturer> manufacturerRepository,
        IRepository<Product> productRepository,
        IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<ProductPicture> productPictureRepository,
        IRepository<ProductProductTagMapping> productTagMappingRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
        IRepository<ProductTag> productTagRepository,
        IRepository<ProductVideo> productVideoRepository,
        IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
        IRepository<RelatedProduct> relatedProductRepository,
        IRepository<Shipment> shipmentRepository,
        IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
        IRepository<TierPrice> tierPriceRepository,
        ISearchPluginManager searchPluginManager,
        IStaticCacheManager staticCacheManager,
        IVendorService vendorService,
        IStoreMappingService storeMappingService,
        IWorkContext workContext,
        LocalizationSettings localizationSettings)
        : base(catalogSettings, aclService, customerService, dateRangeService,
            languageService, localizationService, productAttributeParser, productAttributeService,
            categoryRepository, crossSellProductRepository, discountProductMappingRepository,
            localizedPropertyRepository, manufacturerRepository, productRepository,
            productAttributeCombinationRepository, productAttributeMappingRepository,
            productCategoryRepository, productManufacturerRepository, productPictureRepository,
            productTagMappingRepository, productSpecificationAttributeRepository,
            productTagRepository, productVideoRepository, productWarehouseInventoryRepository,
            relatedProductRepository, shipmentRepository, stockQuantityHistoryRepository,
            tierPriceRepository, searchPluginManager, staticCacheManager,
            vendorService, storeMappingService, workContext, localizationSettings)
    {
    }

    public override async Task<IPagedList<Product>> SearchProductsAsync(
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        IList<int> categoryIds = null,
        IList<int> manufacturerIds = null,
        int storeId = 0,
        int vendorId = 0,
        int warehouseId = 0,
        ProductType? productType = null,
        bool visibleIndividuallyOnly = false,
        bool excludeFeaturedProducts = false,
        decimal? priceMin = null,
        decimal? priceMax = null,
        int productTagId = 0,
        string keywords = null,
        bool searchDescriptions = false,
        bool searchManufacturerPartNumber = true,
        bool searchSku = true,
        bool searchProductTags = false,
        int languageId = 0,
        IList<SpecificationAttributeOption> filteredSpecOptions = null,
        ProductSortingEnum orderBy = ProductSortingEnum.Position,
        bool showHidden = false,
        bool? overridePublished = null)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductService.SearchProducts");
        var stopwatch = Stopwatch.StartNew();

        activity?.SetTag("db.system", "mssql");

        try
        {
            var result = await base.SearchProductsAsync(
                pageIndex, pageSize, categoryIds, manufacturerIds,
                storeId, vendorId, warehouseId, productType,
                visibleIndividuallyOnly, excludeFeaturedProducts,
                priceMin, priceMax, productTagId, keywords,
                searchDescriptions, searchManufacturerPartNumber,
                searchSku, searchProductTags, languageId,
                filteredSpecOptions, orderBy, showHidden, overridePublished);

            stopwatch.Stop();

            // Structural metadata only — no PII
            activity?.SetTag("search.keywords_present", !string.IsNullOrEmpty(keywords));
            activity?.SetTag("search.category_filter", categoryIds?.Count > 0);
            activity?.SetTag("search.page_size", pageSize);
            activity?.SetTag("search.result_count", result.TotalCount);

            NopTelemetry.SearchDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object>("search.has_keywords", !string.IsNullOrEmpty(keywords)));

            NopTelemetry.SearchResultCount.Record(result.TotalCount,
                new KeyValuePair<string, object>("search.has_keywords", !string.IsNullOrEmpty(keywords)));

            if (result.TotalCount == 0)
            {
                NopTelemetry.SearchEmptyResults.Add(1,
                    new KeyValuePair<string, object>("search.has_keywords", !string.IsNullOrEmpty(keywords)));
            }

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
