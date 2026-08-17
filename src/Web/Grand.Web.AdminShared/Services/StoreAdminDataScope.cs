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

    /// <summary>Looser than <see cref="HasAccess"/>: a global entity or one shared across multiple
    /// stores (including the staff member's) may be viewed; only an entity limited to stores that
    /// exclude the staff member's store is denied. Mirrors Store's original Edit(GET)/CopyProduct
    /// behavior (see Grand.Web.Store's ProductController pre-consolidation).</summary>
    public Task<bool> CanView(TEntity entity)
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
        return query.Where(x => x.LimitedToStores && x.Stores.Contains(staffStoreId) && x.Stores.Count == 1);
    }

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public string ResourceKeyPrefix => "Admin";

    public bool ShowStoreSelector => true;

    public string? DefaultVendorId => null;
}
