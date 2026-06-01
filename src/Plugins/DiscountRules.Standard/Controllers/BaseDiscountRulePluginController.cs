using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DiscountRules.Standard.Controllers;

/// <summary>
///     Base controller for discount rule plugins.
///     Bypasses the Admin-only gate so that store managers can also access the
///     requirement-configuration actions (which live at both Admin/… and Store/…
///     routes).  Authorization is enforced per-action via IPermissionService.
/// </summary>
[AuthorizeAdmin(ignore: true)]
[Area("Admin")]
[AuthorizeMenu]
public abstract class BaseDiscountRulePluginController : BaseController;
