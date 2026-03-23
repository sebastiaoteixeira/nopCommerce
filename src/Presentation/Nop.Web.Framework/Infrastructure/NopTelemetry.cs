using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nop.Web.Framework.Infrastructure;

/// <summary>
/// Central OpenTelemetry definitions for the catalog search flow.
/// All spans and metrics are created from these shared instances.
/// </summary>
public static class NopTelemetry
{
    public static readonly ActivitySource ActivitySource = new("NopCommerce.Catalog");
    public static readonly Meter Meter = new("NopCommerce.Catalog");

    /// <summary>Search operation duration in milliseconds</summary>
    public static readonly Histogram<double> SearchDuration =
        Meter.CreateHistogram<double>(
            "nopcommerce.catalog.search.duration",
            unit: "ms",
            description: "Duration of product search operations");

    /// <summary>Count of searches that returned zero results</summary>
    public static readonly Counter<long> SearchEmptyResults =
        Meter.CreateCounter<long>(
            "nopcommerce.catalog.search.empty_results",
            description: "Number of product searches returning zero results");

    /// <summary>Number of results returned per search</summary>
    public static readonly Histogram<double> SearchResultCount =
        Meter.CreateHistogram<double>(
            "nopcommerce.catalog.search.result_count",
            unit: "results",
            description: "Number of results returned per search");

    /// <summary>Price calculation duration in milliseconds</summary>
    public static readonly Histogram<double> PricingDuration =
        Meter.CreateHistogram<double>(
            "nopcommerce.catalog.pricing.duration",
            unit: "ms",
            description: "Duration of price calculation operations");
}
