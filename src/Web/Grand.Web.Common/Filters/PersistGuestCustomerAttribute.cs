using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Saves the request's unsaved guest before a GET action that writes to the current customer. A guest of a
///     GET request is not in the database yet, and a customer write is a partial update by id that silently does
///     nothing on a document that does not exist. Requests of other methods save the guest when it is created
/// </summary>
public class PersistGuestCustomerAttribute : TypeFilterAttribute
{
    public PersistGuestCustomerAttribute() : base(typeof(PersistGuestCustomerFilter))
    {
    }

    #region Filter

    internal class PersistGuestCustomerFilter(IGuestCustomerPersistence guestCustomerPersistence)
        : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await guestCustomerPersistence.EnsurePersisted();
            await next();
        }
    }

    #endregion
}
