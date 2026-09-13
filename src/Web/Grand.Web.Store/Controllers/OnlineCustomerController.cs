using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

/// <summary>
///     Lets a store manager see the customers of his own store who are currently online.
/// </summary>
// Reduced to a thin subclass of BaseOnlineCustomerController (ARCH-001). List (GET+POST) lives in the
// shared base; this class only supplies Store's DI wiring plus the attributes that used to arrive
// transitively via BaseStoreController - BaseOnlineCustomerController can't inherit any single host's
// base controller (it's shared across Admin/Store), so each subclass restates its own host's
// attribute set explicitly. Store has no Sales-Manager concept, so it keeps the base's default
// (unrestricted) SalesEmployeeIdFilter.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class OnlineCustomerController(
    ICustomerService customerService,
    IDateTimeService dateTimeService,
    CustomerSettings customerSettings,
    ITranslationService translationService,
    IContextAccessor contextAccessor)
    : BaseOnlineCustomerController(customerService, dateTimeService, customerSettings, translationService, contextAccessor)
{
}
