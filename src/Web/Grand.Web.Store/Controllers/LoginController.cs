using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Store.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[Area(Constants.AreaStore)]
public class LoginController : BaseLoginController
{
    public LoginController(CustomerSettings customerSettings, CaptchaSettings captchaSettings, ITranslationService translationService, ICustomerManagerService customerManagerService, ICustomerService customerService, IGrandAuthenticationService authenticationService, IMessageProviderService messageProviderService, IContextAccessor contextAccessor, IMediator mediator) : base(customerSettings, captchaSettings, translationService, customerManagerService, customerService, authenticationService, messageProviderService, contextAccessor, mediator)
    {
    }
}
