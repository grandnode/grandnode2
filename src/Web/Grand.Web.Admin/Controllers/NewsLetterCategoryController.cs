using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.NewsletterCategories)]
    public partial class NewsletterCategoryController : BaseAdminController
    {
        #region Fields 

        private readonly INewsletterCategoryService _newsletterCategoryService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;

        #endregion

        #region Ctor

        public NewsletterCategoryController(
            INewsletterCategoryService newsletterCategoryService,
            ILanguageService languageService,
            ITranslationService translationService)
        {
            _newsletterCategoryService = newsletterCategoryService;
            _languageService = languageService;
            _translationService = translationService;
        }

        #endregion

        #region Methods

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var newslettercategories = await _newsletterCategoryService.GetAllNewsletterCategory();
            var gridModel = new DataSourceResult
            {
                Data = newslettercategories.Select(x =>
                {
                    return new
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Selected = x.Selected,
                        DisplayOrder = x.DisplayOrder
                    };
                }).OrderBy(x => x.DisplayOrder),
                Total = newslettercategories.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new NewsletterCategoryModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(NewsletterCategoryModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var newsletterCategory = model.ToEntity();
                await _newsletterCategoryService.InsertNewsletterCategory(newsletterCategory);
                Success(_translationService.GetResource("admin.marketing.NewsletterCategory.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = newsletterCategory.Id }) : RedirectToAction("List");
            }

            return View(model);
        }
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var newsletterCategory = await _newsletterCategoryService.GetNewsletterCategoryById(id);
            if (newsletterCategory == null)
                return RedirectToAction("List");

            var model = newsletterCategory.ToModel();

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = newsletterCategory.GetTranslation(x => x.Name, languageId, false);
                locale.Description = newsletterCategory.GetTranslation(x => x.Description, languageId, false);
            });

            return View(model);
        }

        [HttpPost]
        [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(NewsletterCategoryModel model, bool continueEditing)
        {
            var newsletterCategory = await _newsletterCategoryService.GetNewsletterCategoryById(model.Id);
            if (newsletterCategory == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                newsletterCategory = model.ToEntity(newsletterCategory);
                await _newsletterCategoryService.UpdateNewsletterCategory(newsletterCategory);

                Success(_translationService.GetResource("admin.marketing.NewsletterCategory.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = newsletterCategory.Id }) : RedirectToAction("List");
            }
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var newsletterCategory = await _newsletterCategoryService.GetNewsletterCategoryById(id);
            if (newsletterCategory == null)
                return RedirectToAction("List");

            await _newsletterCategoryService.DeleteNewsletterCategory(newsletterCategory);

            Success(_translationService.GetResource("admin.marketing.NewsletterCategory.Deleted"));
            return RedirectToAction("List");
        }

        #endregion

    }
}