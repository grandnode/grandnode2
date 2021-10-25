using Grand.Domain.Stores;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components
{
    public class VoiceNavigationViewComponent : BaseViewComponent
    {
        private readonly StoreInformationSettings _storeInformationSettings;

        public VoiceNavigationViewComponent(
            StoreInformationSettings storeInformationSettings)
        {
            _storeInformationSettings = storeInformationSettings;
        }

        public IViewComponentResult Invoke()
        {
            if (!_storeInformationSettings.VoiceNavigation)
                return Content("");

            return View();
        }
    }
}
