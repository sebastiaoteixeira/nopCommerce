using System.Diagnostics;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Domain.Catalog;
using Nop.Core.Events;
using Nop.Data;

namespace Nop.Web.Framework.Infrastructure;

/// <summary>
/// Decorator over EntityRepository&lt;Product&gt; that adds OpenTelemetry tracing spans around repository operations.
/// </summary>
public class InstrumentedProductRepository : EntityRepository<Product>
{
    public InstrumentedProductRepository(
        IEventPublisher eventPublisher,
        INopDataProvider dataProvider,
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager,
        AppSettings appSettings)
        : base(eventPublisher, dataProvider, shortTermCacheManager, staticCacheManager, appSettings)
    {
    }

    public override async Task<Product> GetByIdAsync(int? id, Func<ICacheKeyService, CacheKey> getCacheKey = null, bool includeDeleted = true, bool useShortTermCache = false)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.GetById");
        var stopwatch = Stopwatch.StartNew();
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "GetById");
        if (id.HasValue)
            activity?.SetTag("db.product_id", id.Value);

        try
        {
            var result = await base.GetByIdAsync(id, getCacheKey, includeDeleted, useShortTermCache);
            stopwatch.Stop();
            NopTelemetry.RepositoryDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object>("db.operation", "GetById"));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public override async Task<IPagedList<Product>> GetAllPagedAsync(Func<IQueryable<Product>, IQueryable<Product>> func = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false, bool includeDeleted = true)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.GetAllPaged");
        var stopwatch = Stopwatch.StartNew();
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "GetAllPaged");
        activity?.SetTag("db.page_index", pageIndex);
        activity?.SetTag("db.page_size", pageSize);

        try
        {
            var result = await base.GetAllPagedAsync(func, pageIndex, pageSize, getOnlyTotalCount, includeDeleted);
            stopwatch.Stop();
            NopTelemetry.RepositoryDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object>("db.operation", "GetAllPaged"));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public override async Task<IPagedList<Product>> GetAllPagedAsync(Func<IQueryable<Product>, Task<IQueryable<Product>>> func = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false, bool includeDeleted = true)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.GetAllPaged");
        var stopwatch = Stopwatch.StartNew();
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "GetAllPaged");
        activity?.SetTag("db.page_index", pageIndex);
        activity?.SetTag("db.page_size", pageSize);

        try
        {
            var result = await base.GetAllPagedAsync(func, pageIndex, pageSize, getOnlyTotalCount, includeDeleted);
            stopwatch.Stop();
            NopTelemetry.RepositoryDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object>("db.operation", "GetAllPaged"));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public override async Task<IList<Product>> GetAllAsync(Func<IQueryable<Product>, IQueryable<Product>> func = null,
        Func<ICacheKeyService, CacheKey> getCacheKey = null, bool includeDeleted = true)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.GetAll");
        var stopwatch = Stopwatch.StartNew();
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "GetAll");

        try
        {
            var result = await base.GetAllAsync(func, getCacheKey, includeDeleted);
            stopwatch.Stop();
            NopTelemetry.RepositoryDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object>("db.operation", "GetAll"));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public override async Task InsertAsync(Product entity, bool publishEvent = true)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.Insert");
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "Insert");

        try
        {
            await base.InsertAsync(entity, publishEvent);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public override async Task UpdateAsync(Product entity, bool publishEvent = true)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.Update");
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "Update");

        try
        {
            await base.UpdateAsync(entity, publishEvent);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public override async Task DeleteAsync(Product entity, bool publishEvent = true)
    {
        using var activity = NopTelemetry.ActivitySource.StartActivity("ProductRepository.Delete");
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "Delete");

        try
        {
            await base.DeleteAsync(entity, publishEvent);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
