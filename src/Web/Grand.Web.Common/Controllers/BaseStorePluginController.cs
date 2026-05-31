using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Controllers;

/// <summary>
///     Base controller for plugins in the Store area
/// </summary>
[AuthorizeStore]
[Area("Store")]
[AuthorizeMenu]
public abstract class BaseStorePluginController : BaseController;
