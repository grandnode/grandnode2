using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Controllers;

/// <summary>
///     Base controller for plugins
/// </summary>
[AuthorizeAdmin]
[Area("Admin")]
[AuthorizeMenu]
public abstract class BaseAdminPluginController : BaseController
{
}