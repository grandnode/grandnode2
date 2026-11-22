using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Sets <see cref="IStoreAccessModel.StoreAccess" /> on the model of a view result. A result filter rather than
///     code in the actions, so every path that renders a view - a failed POST re-render included - carries the value
/// </summary>
public class StoreAccessAttribute : TypeFilterAttribute
{
    public StoreAccessAttribute() : base(typeof(StoreAccessFilter))
    {
    }

    #region Filter

    internal class StoreAccessFilter(
        IRequestPermissionService permissionService,
        StoreInformationSettings storeInformationSettings) : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ViewResult { Model: IStoreAccessModel model })
                model.StoreAccess = await GetStoreAccess();

            await next();
        }

        private async Task<StoreAccess> GetStoreAccess()
        {
            if (storeInformationSettings.StoreClosed &&
                !await permissionService.Authorize(StandardPermission.AccessClosedStore))
                return StoreAccess.Closed;

            return await permissionService.Authorize(StandardPermission.PublicStoreAllowNavigation)
                ? StoreAccess.Open
                : StoreAccess.RegisteredOnly;
        }
    }

    #endregion
}
