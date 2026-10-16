using Grand.Business.Core.Utilities.Catalog;
using Grand.Infrastructure;

namespace Grand.Web.Common.Extensions;

/// <summary>
///     Builds the catalog access of the current request
/// </summary>
public static class CatalogAccessExtensions
{
    /// <summary>
    ///     Gets the catalog access of the current request: what its customer may see in its store
    /// </summary>
    public static CatalogAccess CatalogAccess(this IContextAccessor contextAccessor)
    {
        return Business.Core.Utilities.Catalog.CatalogAccess.For(contextAccessor.WorkContext.CurrentCustomer,
            contextAccessor.StoreContext.CurrentStore.Id);
    }
}
