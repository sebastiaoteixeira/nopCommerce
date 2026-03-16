# Critique — Observability in nopCommerce

## 1. Architecture Analysis

### Layer Organisation and Dependency Rules

nopCommerce is a monolithic ASP.NET Core app split into four layers under `src/`. Dependencies go one way — downward:

- **Nop.Web** (and **Nop.Web.Framework**) — the ASP.NET Core MVC frontend. Controllers, views, model factories, middleware. Depends on everything below.
- **Nop.Services** — where the business logic lives. 40+ folders covering orders, payments, catalog, customers, shipping, discounts, and more. This is the layer we care about most for instrumentation.
- **Nop.Data** — ORM layer. Uses linq2db for queries and FluentMigrator for schema migrations. Exposes `IRepository<T>` for all data access.
- **Nop.Core** — the foundation. Domain entities, event contracts (`IEventPublisher`), caching interfaces, and the DI engine (`EngineContext`, `INopStartup`). Zero project references — everything else depends on it.

On top of this, there are 30+ **plugins** that reference Core, Services, and Data. They get discovered at runtime through assembly scanning and hook into the same DI container and event pipeline as the rest of the app.

### Core Modules Relevant to Observability

#### IEventPublisher and the Consumer Pipeline

The main way different parts of nopCommerce talk to each other internally is through `IEventPublisher`. It has one method:

```csharp
Task PublishAsync<TEvent>(TEvent @event);
```

The implementation (`Nop.Services/Events/EventPublisher.cs`) grabs all `IConsumer<TEvent>` registered in the DI container and calls them one by one. If a consumer throws, the exception gets logged and swallowed — the next consumer still runs. If the event implements `IStopProcessingEvent` and a consumer sets `StopProcessing = true`, the remaining consumers are skipped.

Consumers are auto-discovered at startup (`Nop.Web.Framework/Infrastructure/NopStartup.cs`). The type finder scans all loaded assemblies — core and plugins — for anything implementing `IConsumer<>` and registers it as scoped. So every event handler runs in the same request scope as the code that published the event.

Events fall into three buckets:

- **Entity lifecycle events** — `EntityInsertedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityDeletedEvent<T>` — fired automatically by the repository layer on every CRUD operation.
- **Domain events** — `OrderPlacedEvent`, `OrderPaidEvent`, `OrderStatusChangedEvent`, etc. — fired explicitly by service methods at specific points in business workflows.
- **UI/pipeline events** — model-prepared events, rendering hooks — fired by the presentation layer.

From an observability standpoint, this event system is the most interesting part of the architecture. Every meaningful state change goes through `PublishAsync`, which means we can instrument the publisher itself and get visibility into all event dispatches without touching business logic.

#### OrderProcessingService — The Order Placement Flow

`OrderProcessingService.PlaceOrderAsync()` is the main entry point for placing an order. It runs through a well-defined sequence of private methods:

1. **PreparePlaceOrderDetailsAsync()** — validates the cart, customer, addresses; calculates totals, discounts, tax, and shipping
2. **GetProcessPaymentResultAsync()** — hands off to `PaymentService.ProcessPaymentAsync()`, which loads the right payment plugin and processes the charge
3. **SaveOrderDetailsAsync()** — writes the `Order` entity to the database, along with billing/shipping addresses
4. **MoveShoppingCartItemsToOrderItemsAsync()** — turns each `ShoppingCartItem` into an `OrderItem`, adjusts inventory through `ProductService.AdjustInventoryAsync()`, and fires a `ShoppingCartItemMovedToOrderItemEvent` per item
5. **SaveDiscountUsageHistoryAsync()** / **SaveGiftCardUsageHistoryAsync()** — bookkeeping
6. **SendNotificationsAndSaveNotesAsync()** — emails (customer, store owner, vendors) and order notes
7. Publishes **OrderPlacedEvent**
8. **CheckOrderStatusAsync()** / **SetOrderStatusAsync()** — evaluates order state and transitions it, publishing `OrderStatusChangedEvent`
9. **ProcessOrderPaidAsync()** — if the payment was captured immediately, fires `OrderPaidEvent`

The constructor takes 40+ dependencies. There is also optional mutex-based locking (`PlaceOrderWithLock` setting) to prevent duplicate orders within a configurable time window, backed by `IStaticCacheManager`.

#### PaymentService

`PaymentService.ProcessPaymentAsync()` has a fast path for zero-amount orders (marks them as Paid immediately) and delegates everything else to the active payment plugin through `IPaymentPluginManager`. There is no logging inside this service — whether a payment succeeded or failed is only visible from the `ProcessPaymentResult` object returned to the caller. This is a blind spot.

#### Logging — DefaultLogger

nopCommerce ships with its own logger (`Nop.Services/Logging/DefaultLogger.cs`) that writes directly to the database through `IRepository<Log>`. Each entry captures the log level, short and full messages, IP address, customer ID, page URL, referrer, and a UTC timestamp. Alongside this, `ICustomerActivityService` records user-facing actions ("placed order", "updated product") as `ActivityLog` rows.

Both are synchronous and database-bound — there is no batching, no structured logging, no correlation IDs. Every log write is a database insert.

#### Caching — Multi-level with Event-driven Invalidation

Two cache layers: `IShortTermCacheManager` (per-request, in-memory) and `IStaticCacheManager` (distributed — Redis, SQL Server, or memory-backed). Invalidation is driven by events: `CacheEventConsumer<T>` listens for entity insert/update/delete events and clears the right keys. The presentation layer adds its own `ModelCacheEventConsumer` covering 30+ entity types for view model caches.

#### Middleware Pipeline

The HTTP pipeline is assembled via ordered `INopStartup` implementations: error handling first (order 0), then static files/compression (99), session/localization (100), routing/rate limiting (400), auth (500/600), MVC (1000), and core service registration (2000). The error handler middleware catches unhandled exceptions, 404s, and 400s, and logs them through `ILogger` with customer context when it can.
