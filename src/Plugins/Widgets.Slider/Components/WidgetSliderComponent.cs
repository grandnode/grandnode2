using Grand.Business.Common.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;
using Widgets.Slider.Services;

namespace Widgets.Slider.ViewComponents
{
    [ViewComponent(Name = "WidgetSlider")]
    public class WidgetSliderComponent : ViewComponent
    {
        private readonly IPictureService _pictureService;
        private readonly ISliderService _sliderService;
        private readonly IWorkContext _workContext;

        public WidgetSliderComponent(
            IPictureService pictureService,
            ISliderService sliderService,
            IWorkContext workContext)
        {
            _pictureService = pictureService;
            _sliderService = sliderService;
            _workContext = workContext;
        }

        protected async Task<string> GetPictureUrl(string pictureId)
        {
            var url = await _pictureService.GetPictureUrl(pictureId, showDefaultPicture: false);
            if (url == null)
                url = "";

            return url;
        }

        protected async Task PrepareModel(IList<PictureSlider> sliders, PublicInfoModel model)
        {
            int i = 1;
            foreach (var item in sliders.OrderBy(x => x.DisplayOrder))
            {
                model.Slide.Add(new PublicInfoModel.Slider() {
                    Link = item.Link,
                    PictureUrl = await GetPictureUrl(item.PictureId),
                    Name = item.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    Description = item.GetTranslation(x => x.Description, _workContext.WorkingLanguage.Id),
                    FullWidth = item.FullWidth,
                    CssClass = i == 1 ? "active" : ""
                });
                i++;
            }

        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
        {

            var model = new PublicInfoModel();
            if (widgetZone == SliderWidgetDefaults.WidgetZoneHomePage)
            {
                var slides = await _sliderService.GetPictureSliders(SliderType.HomePage);
                await PrepareModel(slides, model);
            }
            if (widgetZone == SliderWidgetDefaults.WidgetZoneCategoryPage)
            {
                var slides = await _sliderService.GetPictureSliders(SliderType.Category, additionalData.ToString());
                await PrepareModel(slides, model);
            }
            if (widgetZone == SliderWidgetDefaults.WidgetZoneCollectionPage)
            {
                var slides = await _sliderService.GetPictureSliders(SliderType.Collection, additionalData.ToString());
                await PrepareModel(slides, model);
            }
            if (widgetZone == SliderWidgetDefaults.WidgetZoneBrandPage)
            {
                var slides = await _sliderService.GetPictureSliders(SliderType.Brand, additionalData.ToString());
                await PrepareModel(slides, model);
            }

            if (!model.Slide.Any())
                return Content("");

            return View(model);
        }
    }
}