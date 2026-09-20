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
