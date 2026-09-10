using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Grand.Web.Common.Infrastructure;

/// <summary>
///     Renames a controller's MVC <c>ControllerName</c> (which Razor's default view-location
///     convention uses to pick the view-search folder) to whatever <see cref="SharedViewFolderAttribute"/>
///     declares, for any controller type carrying that attribute. URL routing is unaffected — this
///     only changes where the view engine looks for a view when an action calls <c>View()</c>.
/// </summary>
public class SharedViewFolderControllerNameConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var attr = controller.ControllerType.GetCustomAttributes(typeof(SharedViewFolderAttribute), true)
            .OfType<SharedViewFolderAttribute>()
            .FirstOrDefault();
        if (attr is not null)
            controller.ControllerName = attr.ControllerName;
    }
}
