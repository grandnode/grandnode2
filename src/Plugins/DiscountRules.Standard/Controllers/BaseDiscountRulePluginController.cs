using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;

namespace DiscountRules.Standard.Controllers;

[AuthorizeAdminOrStore]
public abstract class BaseDiscountRulePluginController : BaseController;
