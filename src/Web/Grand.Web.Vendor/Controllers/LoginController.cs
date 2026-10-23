using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Vendor.Extensions;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Vendor.Controllers;

[Area(Constants.AreaVendor)]
public class LoginController : BaseLoginController
{
    public LoginController(CustomerSettings customerSettings, CaptchaSettings captchaSettings, IRequestTranslationService translationService, ICustomerManagerService customerManagerService, ICustomerService customerService, IGrandAuthenticationService authenticationService, ICustomerMessageProviderService customerMessageProviderService, IContextAccessor contextAccessor, IMediator mediator) : base(customerSettings, captchaSettings, translationService, customerManagerService, customerService, authenticationService, customerMessageProviderService, contextAccessor, mediator)
    {
    }
}
