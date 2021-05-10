using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.ViewComponents
{
    public class PartialViewComponent : BaseViewComponent
    {

        public PartialViewComponent()
        {
        }

        public IViewComponentResult Invoke(string partialName, object additionalData = null)
        {
            return View(partialName, additionalData);
        }
    }
}