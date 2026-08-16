#nullable enable

using Grand.Domain;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class StoreAdminDataScope<TEntity>(IContextAccessor contextAccessor) : IAdminDataScope<TEntity>
    where TEntity : BaseEntity, IStoreLinkEntity
{
    public Task<bool> HasAccess(TEntity entity)
    {
        var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        return Task.FromResult(entity != null && entity.AccessToEntityByStore(staffStoreId));
    }

    public IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query)
    {
        var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        if (string.IsNullOrEmpty(staffStoreId)) return query;
        return query.Where(x => x.LimitedToStores && x.Stores.Contains(staffStoreId) && x.Stores.Count == 1);
    }

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public string ResourceKeyPrefix => "Admin";
}
