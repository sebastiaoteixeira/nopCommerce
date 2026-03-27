# Critique  - Observability in nopCommerce

## What in nopCommerce's design helped or hindered instrumentation?

**Helped:**
- `virtual` methods on service classes (ProductService.SearchProductsAsync, PriceCalculationService.GetFinalPriceAsync) and on `EntityRepository<T>` (GetByIdAsync, GetAllPagedAsync, etc.)  - enabled the decorator pattern at both the service and repository layers without modifying original classes
- `INopStartup` ordered startup pattern  - allowed registering our OpenTelemetryStartup at Order 2100, after NopStartup (2000), so our decorator DI registrations win via last-registration-wins
- `IProductService` / `IPriceCalculationService` abstractions  - all consumers depend on interfaces, so swapping implementations is invisible to controllers and factories
- `protected readonly` fields in service classes  - inheritance works cleanly because all dependencies are accessible to subclasses

**Hindered:**
- `IRepository<T>` exposes `IQueryable<T>` via `.Table`  - there's no discrete "query" method to intercept. However, `EntityRepository<T>` methods are `virtual`, allowing us to create `InstrumentedProductRepository` as a closed-generic decorator that traces `GetByIdAsync`, `GetAllPagedAsync`, and other operations. The `.Table` property itself remains a blind spot since LINQ materialisation happens lazily inside these methods
- No structured logging  - nopCommerce's DefaultLogger writes to the database synchronously. There's no correlation ID, no structured fields, no way to link logs to traces without replacing the entire logging infrastructure
- `IEventPublisher` is fire-and-forget with swallowed exceptions  - events are a natural instrumentation boundary, but the current implementation silently catches consumer exceptions, making it hard to detect event processing failures
- Static `EngineContext.Current` service locator pattern used in some places  - makes it harder to reason about dependencies and test instrumented code in isolation

## Architectural changes for better observability

1. **Replace DefaultLogger with ILogger<T> + OpenTelemetry logging exporter**  - This would give structured logging with automatic trace correlation. Cost: moderate refactor across all services that use ILogger directly, but the DefaultLogger's database-per-log-entry approach is a performance liability anyway.

2. **Make IEventPublisher observable**  - Wrap PublishAsync with an Activity span per event type. This would give visibility into event dispatch timing and consumer failures. Could be done with a decorator (same pattern we used for ProductService). Cost: minimal  - single decorator class.

3. **Replace IRepository<T>.Table with explicit query methods**  - Instead of exposing raw IQueryable, define named operations (SearchAsync, GetByIdAsync, etc.) that can be individually instrumented. Cost: high  - would require rewriting every service's data access patterns. Not worth it for observability alone, but would improve testability and encapsulation.

## Surgical changes and minimising impact

**Zero business logic files were modified.** All instrumentation lives in `Nop.Web.Framework/Infrastructure/` and `Nop.Web.Framework/Mvc/Filters/`. This was possible because:

1. **InstrumentedProductService** inherits from `ProductService` and overrides only `SearchProductsAsync`. Registered via DI after the original  - no change to ProductService.cs, CatalogModelFactory.cs, or any controller.

2. **InstrumentedPriceCalculationService** follows the same pattern for `GetFinalPriceAsync`. No change to PriceCalculationService.cs or ProductModelFactory.cs.

3. **InstrumentedProductRepository** inherits from `EntityRepository<Product>` and overrides key operations (`GetByIdAsync`, `GetAllPagedAsync`, `GetAllAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`). Registered as `IRepository<Product>`  - the closed-generic registration overrides the open-generic `IRepository<>` → `EntityRepository<>` from NopDbStartup. No change to EntityRepository.cs or any service that injects `IRepository<Product>`.

4. **TracingActionFilter** is registered globally via `MvcOptions.Filters`  - no change to BaseController or any controller class. For product page view tracking, the filter publishes a `ProductDetailPageViewedEvent` through nopCommerce's own `IEventPublisher` rather than injecting `IProductService` directly. A separate `ProductPageViewTelemetryConsumer` handles the metric recording  - keeping the filter decoupled from the service layer and following the same event-driven pattern nopCommerce uses internally.

5. **HttpPathSpanProcessor** is a custom `BaseProcessor<Activity>` that runs in the OTel pipeline  - no change to ASP.NET Core middleware or routing.

6. **OTel Collector PII sanitization** is configured in `infra/otel-collector/config.yaml`  - no application code changes needed for sensitive data exclusion. The collector strips `client.address`, `net.peer.ip`, and cookie headers before export. Non-PII attributes like `url.query` and `user_agent.original` are deliberately kept  - they carry debugging value (search terms, browser context) without being personally identifiable.

The only "infrastructure boundary" change was adding three NuGet packages to `Nop.Web.Framework.csproj` and creating `OpenTelemetryStartup.cs` as an `INopStartup`  - the same pattern nopCommerce uses for all its own startup configuration. This is the minimal footprint possible for adding OpenTelemetry to a .NET application.
