using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ShippingPoint.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shipping.ShippingPoint.Components
{
    [ViewComponent(Name = "ShippingPoint")]
    public class SelectedShippingPointViewComponent : ViewComponent
    {
        private readonly ITranslationService _translationService;
        private readonly IShippingPointService _shippingPointService;
        private readonly IWorkContext _workContext;

        public SelectedShippingPointViewComponent(ITranslationService translationService,
            IShippingPointService shippingPointService, IWorkContext workContext)
        {
            _translationService = translationService;
            _shippingPointService = shippingPointService;
            _workContext = workContext;
        }
        public async Task<IViewComponentResult> InvokeAsync(string shippingOption)
        {
            var parameter = shippingOption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries)[0];

            if (parameter == _translationService.GetResource("Shipping.ShippingPoint.PluginName"))
            {
                var shippingPoints = await _shippingPointService.GetAllStoreShippingPoint(_workContext.CurrentStore.Id);

                var shippingPointsModel = new List<SelectListItem>();
                shippingPointsModel.Add(new SelectListItem() { Value = "", Text = _translationService.GetResource("Shipping.ShippingPoint.SelectShippingOption") });

                foreach (var shippingPoint in shippingPoints)
                {
                    shippingPointsModel.Add(new SelectListItem() { Value = shippingPoint.Id, Text = shippingPoint.ShippingPointName });
                }

                return View(shippingPointsModel);
            }
            return Content("ShippingPointController: given Shipping Option doesn't exist");

        }
    }
}
