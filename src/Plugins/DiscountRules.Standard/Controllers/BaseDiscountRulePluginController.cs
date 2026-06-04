using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DiscountRules.Standard.Controllers;

[AuthorizeAdminOrStore]
[AutoValidateAntiforgeryToken]
public abstract class BaseDiscountRulePluginController : BaseController;
