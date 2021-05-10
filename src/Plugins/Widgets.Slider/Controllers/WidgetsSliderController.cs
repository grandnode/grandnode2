using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Widgets.Slider.Models;
using Widgets.Slider.Services;

namespace Widgets.Slider.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Widgets)]
    public class WidgetsSliderController : BasePluginController
    {
        private readonly IStoreService _storeService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ISliderService _sliderService;
        private readonly ILanguageService _languageService;
        private readonly ICategoryService _categoryService;
        private readonly ICollectionService _collectionService;

        public WidgetsSliderController(
            IStoreService storeService,
            IPictureService pictureService,
            ITranslationService translationService,
            ISliderService sliderService,
            ILanguageService languageService,
            ICategoryService categoryService,
            ICollectionService collectionService)
        {
            _storeService = storeService;
            _pictureService = pictureService;
            _translationService = translationService;
            _sliderService = sliderService;
            _languageService = languageService;
            _categoryService = categoryService;
            _collectionService = collectionService;
        }        
        public IActionResult Configure()
        {
            return View("~/Plugins/Widgets.Slider/Views/List.cshtml");
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
            var gridModel = new DataSourceResult
            {
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
            
            return View("~/Plugins/Widgets.Slider/Views/Create.cshtml", model);
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

            return View("~/Plugins/Widgets.Slider/Views/Create.cshtml", model);
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
            
            return View("~/Plugins/Widgets.Slider/Views/Edit.cshtml", model);
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
            return View("~/Plugins/Widgets.Slider/Views/Edit.cshtml", model);
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
