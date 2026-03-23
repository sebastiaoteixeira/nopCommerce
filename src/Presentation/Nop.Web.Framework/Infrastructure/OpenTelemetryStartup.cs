using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Web.Framework.Mvc.Filters;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Nop.Web.Framework.Infrastructure;

/// <summary>
/// Registers OpenTelemetry SDK, the InstrumentedProductService decorator,
/// and the global TracingActionFilter. Order 2100 ensures this runs after
/// NopStartup (2000) so the decorator DI registration wins.
/// </summary>
public class OpenTelemetryStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("nopcommerce"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddSource(NopTelemetry.ActivitySource.Name)
                .AddProcessor(new HttpPathSpanProcessor())
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter(NopTelemetry.Meter.Name)
                .AddOtlpExporter((exporterOptions, readerOptions) =>
                {
                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                }));

        // Decorators: override registrations from NopStartup (last-wins in .NET DI)
        services.AddScoped<IProductService, InstrumentedProductService>();
        services.AddScoped<IPriceCalculationService, InstrumentedPriceCalculationService>();

        // Global action filter — no change to BaseController needed
        services.Configure<MvcOptions>(options =>
            options.Filters.Add<TracingActionFilter>());
    }

    public void Configure(IApplicationBuilder application)
    {
        // No middleware configuration needed — OTel hooks into the existing pipeline
    }

    public int Order => 2100;
}
