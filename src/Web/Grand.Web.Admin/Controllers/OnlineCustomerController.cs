using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseOnlineCustomerController (ARCH-001). List (GET+POST) lives in the
// shared base; this class only supplies Admin's DI wiring plus the attributes that used to arrive
// transitively via BaseAdminController - BaseOnlineCustomerController can't inherit any single host's
// base controller (it's shared across Admin/Store), so each subclass restates its own host's
// attribute set explicitly. Admin also overrides SalesEmployeeIdFilter to restrict the online-customer
// list to the current Sales-Manager's own customers - Store has no such concept.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class OnlineCustomerController(
    ICustomerService customerService,
    IDateTimeService dateTimeService,
    CustomerSettings customerSettings,
    ITranslationService translationService,
    IContextAccessor contextAccessor)
    : BaseOnlineCustomerController(customerService, dateTimeService, customerSettings, translationService, contextAccessor)
{
    protected override string SalesEmployeeIdFilter => contextAccessor.WorkContext.CurrentCustomer.SeId;
}
