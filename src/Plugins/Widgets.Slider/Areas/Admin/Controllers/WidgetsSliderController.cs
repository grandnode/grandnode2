using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Widgets.Slider.Models;
using Widgets.Slider.Services;

namespace Widgets.Slider.Areas.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Widgets)]
    public class WidgetsSliderController : BasePluginController
    {
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ISliderService _sliderService;
        private readonly ILanguageService _languageService;
        private readonly SliderWidgetSettings _sliderWidgetSettings;
        private readonly ISettingService _settingService;

        public WidgetsSliderController(
            IPictureService pictureService,
            ITranslationService translationService,
            ISliderService sliderService,
            ILanguageService languageService,
            ISettingService settingService,
            SliderWidgetSettings sliderWidgetSettings)
        {
            _pictureService = pictureService;
            _translationService = translationService;
            _sliderService = sliderService;
            _languageService = languageService;
            _settingService = settingService;
            _sliderWidgetSettings = sliderWidgetSettings;
        }
        public IActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.DisplayOrder = _sliderWidgetSettings.DisplayOrder;
            model.CustomerGroups = _sliderWidgetSettings.LimitedToGroups?.ToArray();
            model.Stores = _sliderWidgetSettings.LimitedToStores?.ToArray();
            return View(model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            _sliderWidgetSettings.DisplayOrder = model.DisplayOrder;
            _sliderWidgetSettings.LimitedToGroups = model.CustomerGroups == null ? new List<string>() : model.CustomerGroups.ToList();
            _sliderWidgetSettings.LimitedToStores = model.Stores == null ? new List<string>() : model.Stores.ToList();
            _settingService.SaveSetting(_sliderWidgetSettings);
            return Json("Ok");
        }

        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var sliders = await _sliderService.GetPictureSliders();

            var items = new List<SlideListModel>();
            foreach (var x in sliders)
            {
                var model = x.ToListModel();
                var picture = await _pictureService.GetPictureById(x.PictureId);
                if (picture != null)
                {
                    model.PictureUrl = await _pictureService.GetPictureUrl(picture, 150);
                }
                items.Add(model);
            }
            var gridModel = new DataSourceResult {
                Data = items,
                Total = sliders.Count
            };
            return Json(gridModel);
        }

        public async Task<IActionResult> Create()
        {
            var model = new SlideModel();
            //locales
            await AddLocales(_languageService, model.Locales);

            return View(model);
        }
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(SlideModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var pictureSlider = model.ToEntity();
                pictureSlider.Locales = model.Locales.ToLocalizedProperty();

                await _sliderService.InsertPictureSlider(pictureSlider);

                Success(_translationService.GetResource("Widgets.Slider.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = pictureSlider.Id }) : RedirectToAction("Configure");

            }
            return View(model);
        }
        public async Task<IActionResult> Edit(string id)
        {
            var slide = await _sliderService.GetById(id);
            if (slide == null)
                return RedirectToAction("Configure");

            var model = slide.ToModel();

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = slide.GetTranslation(x => x.Name, languageId, false);
                locale.Description = slide.GetTranslation(x => x.Description, languageId, false);
            });

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(SlideModel model, bool continueEditing)
        {
            var pictureSlider = await _sliderService.GetById(model.Id);
            if (pictureSlider == null)
                return RedirectToAction("Configure");

            if (ModelState.IsValid)
            {
                pictureSlider = model.ToEntity();
                pictureSlider.Locales = model.Locales.ToLocalizedProperty();
                await _sliderService.UpdatePictureSlider(pictureSlider);
                Success(_translationService.GetResource("Widgets.Slider.Edited"));
                return continueEditing ? RedirectToAction("Edit", new { id = pictureSlider.Id }) : RedirectToAction("Configure");

            }
            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var pictureSlider = await _sliderService.GetById(id);
            if (pictureSlider == null)
                return Json(new DataSourceResult { Errors = "This pictureSlider not exists" });

            await _sliderService.DeleteSlider(pictureSlider);

            return new JsonResult("");
        }
    }
}
