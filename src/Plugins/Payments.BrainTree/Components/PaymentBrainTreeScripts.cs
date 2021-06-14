using Microsoft.AspNetCore.Mvc;

namespace Payments.BrainTree.Components
{
    [ViewComponent(Name = "PaymentBrainTreeScripts")]
    public class PaymentBrainTreeScripts : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
