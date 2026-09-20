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
    protected override string SalesEmployeeIdFilter => ContextAccessor.WorkContext.CurrentCustomer.SeId;
}
