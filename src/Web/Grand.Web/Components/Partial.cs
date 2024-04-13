using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class PartialViewComponent : BaseViewComponent
{
    public IViewComponentResult Invoke(string partialName, object additionalData = null)
    {
        return View(partialName, additionalData);
    }
}