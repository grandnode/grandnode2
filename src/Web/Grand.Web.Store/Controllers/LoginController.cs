using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Store.Extensions;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Store.Controllers;

[Area(Constants.AreaStore)]
public class LoginController : BaseLoginController
{
    public LoginController(CustomerSettings customerSettings, CaptchaSettings captchaSettings, IRequestTranslationService translationService, ICustomerManagerService customerManagerService, ICustomerService customerService, IGrandAuthenticationService authenticationService, IMessageProviderService messageProviderService, IContextAccessor contextAccessor, IMediator mediator) : base(customerSettings, captchaSettings, translationService, customerManagerService, customerService, authenticationService, messageProviderService, contextAccessor, mediator)
    {
    }
}
