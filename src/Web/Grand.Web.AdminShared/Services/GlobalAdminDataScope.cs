#nullable enable

using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class GlobalAdminDataScope<TEntity> : IAdminDataScope<TEntity>
{
    public Task<bool> HasAccess(TEntity entity) => Task.FromResult(true);

    public IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query) => query;

    public string? DefaultStoreId => null;

    public string ResourceKeyPrefix => "Admin";

    public bool ShowStoreSelector => true;

    public string? DefaultVendorId => null;

    public bool CanFeatureOnHomepage => true;
}
