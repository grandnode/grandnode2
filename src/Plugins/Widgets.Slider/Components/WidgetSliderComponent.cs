using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;
using Widgets.Slider.Services;

namespace Widgets.Slider.Components;

[ViewComponent(Name = "WidgetSlider")]
public class WidgetSliderComponent : ViewComponent
{
    private readonly IPictureService _pictureService;
    private readonly ISliderService _sliderService;
    private readonly IContextAccessor _contextAccessor;

    public WidgetSliderComponent(
        IPictureService pictureService,
        ISliderService sliderService,
        IContextAccessor contextAccessor)
    {
        _pictureService = pictureService;
        _sliderService = sliderService;
        _contextAccessor = contextAccessor;
    }

    private async Task<string> GetPictureUrl(string pictureId)
    {
        var url = await _pictureService.GetPictureUrl(pictureId, showDefaultPicture: false) ?? "";

        return url;
    }

    private async Task PrepareModel(IList<PictureSlider> sliders, PublicInfoModel model)
    {
        var i = 1;
        foreach (var item in sliders.OrderBy(x => x.DisplayOrder))
        {
            if ((item.StartDateUtc.HasValue && item.StartDateUtc > DateTime.UtcNow) ||
                (item.EndDateUtc.HasValue && item.EndDateUtc < DateTime.UtcNow))
                continue;

            model.Slide.Add(new PublicInfoModel.Slider {
                Link = item.Link,
                PictureUrl = await GetPictureUrl(item.PictureId),
                Name = item.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id),
                Description = item.GetTranslation(x => x.Description, _contextAccessor.WorkContext.WorkingLanguage.Id),
                FullWidth = item.FullWidth,
                CssClass = i == 1 ? "active" : ""
            });
            i++;
        }
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
    {
        var model = new PublicInfoModel();
        //the category, collection and brand zones show the slides of the page's entity; a zone rendered without one
        //would otherwise show every slide of that type
        var entityId = additionalData?.ToString();
        if (widgetZone == SliderWidgetDefaults.WidgetZoneHomePage)
        {
            var slides = await _sliderService.GetPictureSliders(SliderType.HomePage);
            await PrepareModel(slides, model);
        }

        if (widgetZone == SliderWidgetDefaults.WidgetZoneCategoryPage && !string.IsNullOrEmpty(entityId))
        {
            var slides = await _sliderService.GetPictureSliders(SliderType.Category, entityId);
            await PrepareModel(slides, model);
        }

        if (widgetZone == SliderWidgetDefaults.WidgetZoneCollectionPage && !string.IsNullOrEmpty(entityId))
        {
            var slides = await _sliderService.GetPictureSliders(SliderType.Collection, entityId);
            await PrepareModel(slides, model);
        }

        if (widgetZone == SliderWidgetDefaults.WidgetZoneBrandPage && !string.IsNullOrEmpty(entityId))
        {
            var slides = await _sliderService.GetPictureSliders(SliderType.Brand, entityId);
            await PrepareModel(slides, model);
        }

        if (!model.Slide.Any())
            return Content("");

        return View(model);
    }
}