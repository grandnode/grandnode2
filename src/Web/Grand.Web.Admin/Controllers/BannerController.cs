using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Banners;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Banners)]
    public partial class BannerController : BaseAdminController
    {
        private readonly IBannerService _bannerService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        public BannerController(IBannerService bannerService,
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _bannerService = bannerService;
            _translationService = translationService;
            _languageService = languageService;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var banners = await _bannerService.GetAllBanners();
            var gridModel = new DataSourceResult
            {
                Data = banners.Select(x =>
                {
                    var model = x.ToModel();
                    model.Body = "";
                    return model;
                }),
                Total = banners.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new BannerModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(BannerModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var banner = model.ToEntity();
                banner.CreatedOnUtc = DateTime.UtcNow;
                await _bannerService.InsertBanner(banner);

                Success(_translationService.GetResource("admin.marketing.Banners.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = banner.Id }) : RedirectToAction("List");
            }

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var banner = await _bannerService.GetBannerById(id);
            if (banner == null)
                return RedirectToAction("List");

            var model = banner.ToModel();

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = banner.GetTranslation(x => x.Name, languageId, false);
                locale.Body = banner.GetTranslation(x => x.Body, languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(BannerModel model, bool continueEditing)
        {
            var banner = await _bannerService.GetBannerById(model.Id);
            if (banner == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                banner = model.ToEntity(banner);
                await _bannerService.UpdateBanner(banner);
                Success(_translationService.GetResource("admin.marketing.Banners.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = banner.Id }) : RedirectToAction("List");
            }

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var banner = await _bannerService.GetBannerById(id);
            if (banner == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _bannerService.DeleteBanner(banner);
                Success(_translationService.GetResource("admin.marketing.Banners.Deleted"));
                return RedirectToAction("List");
            }
            return RedirectToAction("Edit", new { id = id });
        }
    }
}
