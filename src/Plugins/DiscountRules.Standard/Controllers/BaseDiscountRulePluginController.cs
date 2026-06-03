using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;

namespace DiscountRules.Standard.Controllers;

[AuthorizeMenu]
public abstract class BaseDiscountRulePluginController : BaseController;
