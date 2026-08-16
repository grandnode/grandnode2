#nullable enable

using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class StoreAdminDataScope<TEntity>(IContextAccessor contextAccessor) : IAdminDataScope<TEntity>
    where TEntity : IStoreLinkEntity
{
    public Task<bool> HasAccess(TEntity entity)
    {
        if (entity is null) return Task.FromResult(false);

        var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        var allowed = !entity.LimitedToStores || entity.Stores.Contains(staffStoreId);
        return Task.FromResult(allowed);
    }

    public IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query)
    {
        var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        if (string.IsNullOrEmpty(staffStoreId)) return query;
        return query.Where(x => !x.LimitedToStores || x.Stores.Contains(staffStoreId));
    }

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public string ResourceKeyPrefix => "Admin";
}
