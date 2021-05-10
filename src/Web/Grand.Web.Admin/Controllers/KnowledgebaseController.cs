using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Knowledgebase;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Knowledgebase)]
    public class KnowledgebaseController : BaseAdminController
    {
        private readonly IKnowledgebaseViewModelService _knowledgebaseViewModelService;
        private readonly ITranslationService _translationService;
        private readonly IKnowledgebaseService _knowledgebaseService;
        private readonly ILanguageService _languageService;
        private readonly IGroupService _groupService;
        private readonly IStoreService _storeService;

        public KnowledgebaseController(IKnowledgebaseViewModelService knowledgebaseViewModelService,
            ITranslationService translationService,
            IKnowledgebaseService knowledgebaseService,
            ILanguageService languageService,
            IGroupService groupService,
            IStoreService storeService)
        {
            _knowledgebaseViewModelService = knowledgebaseViewModelService;
            _translationService = translationService;
            _knowledgebaseService = knowledgebaseService;
            _languageService = languageService;
            _groupService = groupService;
            _storeService = storeService;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        public async Task<IActionResult> NodeList()
        {
            var model = await _knowledgebaseViewModelService.PrepareTreeNode();
            return Json(model);
        }

        public async Task<IActionResult> ArticleList(DataSourceRequest command, string parentCategoryId)
        {
            var (knowledgebaseArticleGridModels, totalCount) = await _knowledgebaseViewModelService.PrepareKnowledgebaseArticleGridModel(parentCategoryId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = knowledgebaseArticleGridModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> ListCategoryActivityLog(DataSourceRequest command, string categoryId)
        {
            var (activityLogModels, totalCount) = await _knowledgebaseViewModelService.PrepareCategoryActivityLogModels(categoryId, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = activityLogModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> ListArticleActivityLog(DataSourceRequest command, string articleId)
        {
            var (activityLogModels, totalCount) = await _knowledgebaseViewModelService.PrepareArticleActivityLogModels(articleId, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = activityLogModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> CreateCategory()
        {
            var model = await _knowledgebaseViewModelService.PrepareKnowledgebaseCategoryModel();

            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateCategory(KnowledgebaseCategoryModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var knowledgebaseCategory = await _knowledgebaseViewModelService.InsertKnowledgebaseCategoryModel(model);
                Success(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Added"));
                return continueEditing ? RedirectToAction("EditCategory", new { knowledgebaseCategory.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> EditCategory(string id)
        {
            var knowledgebaseCategory = await _knowledgebaseService.GetKnowledgebaseCategory(id);
            if (knowledgebaseCategory == null)
                return RedirectToAction("List");

            var model = knowledgebaseCategory.ToModel();
            await _knowledgebaseViewModelService.PrepareCategory(model);

            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = knowledgebaseCategory.GetTranslation(x => x.Name, languageId, false);
                locale.Description = knowledgebaseCategory.GetTranslation(x => x.Description, languageId, false);
                locale.MetaDescription = knowledgebaseCategory.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaKeywords = knowledgebaseCategory.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaTitle = knowledgebaseCategory.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = knowledgebaseCategory.GetSeName(languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditCategory(KnowledgebaseCategoryModel model, bool continueEditing)
        {
            var knowledgebaseCategory = await _knowledgebaseService.GetKnowledgebaseCategory(model.Id);
            if (knowledgebaseCategory == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                knowledgebaseCategory = await _knowledgebaseViewModelService.UpdateKnowledgebaseCategoryModel(knowledgebaseCategory, model);
                Success(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Updated"));
                return continueEditing ? RedirectToAction("EditCategory", new { id = knowledgebaseCategory.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            await _knowledgebaseViewModelService.PrepareCategory(model);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            var knowledgebaseCategory = await _knowledgebaseService.GetKnowledgebaseCategory(id);
            if (knowledgebaseCategory == null)
                return RedirectToAction("List");

            if ((await _knowledgebaseService.GetKnowledgebaseArticlesByCategoryId(id)).Any())
            {
                Error(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Cannotdeletewitharticles"));
                return RedirectToAction("EditCategory", new { id });
            }

            if (ModelState.IsValid)
            {
                await _knowledgebaseViewModelService.DeleteKnowledgebaseCategoryModel(knowledgebaseCategory);
                Success(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("EditCategory", new { id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> CreateArticle(string parentCategoryId)
        {
            var model = await _knowledgebaseViewModelService.PrepareKnowledgebaseArticleModel();

            if (!string.IsNullOrEmpty(parentCategoryId))
                model.ParentCategoryId = parentCategoryId;

            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateArticle(KnowledgebaseArticleModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var knowledgebaseArticle = await _knowledgebaseViewModelService.InsertKnowledgebaseArticleModel(model);
                Success(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseArticle.Added"));
                return continueEditing ? RedirectToAction("EditArticle", new { knowledgebaseArticle.Id }) : RedirectToAction("EditCategory", new { id = model.ParentCategoryId });
            }

            //If we got this far, something failed, redisplay form
            await _knowledgebaseViewModelService.PrepareCategory(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> EditArticle(string id)
        {
            var knowledgebaseArticle = await _knowledgebaseService.GetKnowledgebaseArticle(id);
            if (knowledgebaseArticle == null)
                return RedirectToAction("List");

            var model = knowledgebaseArticle.ToModel();
            await _knowledgebaseViewModelService.PrepareCategory(model);
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = knowledgebaseArticle.GetTranslation(x => x.Name, languageId, false);
                locale.Content = knowledgebaseArticle.GetTranslation(x => x.Content, languageId, false);
                locale.MetaDescription = knowledgebaseArticle.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaKeywords = knowledgebaseArticle.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaTitle = knowledgebaseArticle.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = knowledgebaseArticle.GetSeName(languageId, false);
            });

            model.AllowComments = knowledgebaseArticle.AllowComments;

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditArticle(KnowledgebaseArticleModel model, bool continueEditing)
        {
            var knowledgebaseArticle = await _knowledgebaseService.GetKnowledgebaseArticle(model.Id);
            if (knowledgebaseArticle == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                knowledgebaseArticle = await _knowledgebaseViewModelService.UpdateKnowledgebaseArticleModel(knowledgebaseArticle, model);
                Success(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseArticle.Updated"));
                return continueEditing ? RedirectToAction("EditArticle", new { knowledgebaseArticle.Id }) : RedirectToAction("EditCategory", new { id = model.ParentCategoryId });
            }

            //If we got this far, something failed, redisplay form
            await _knowledgebaseViewModelService.PrepareCategory(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteArticle(string id)
        {
            var knowledgebaseArticle = await _knowledgebaseService.GetKnowledgebaseArticle(id);
            if (knowledgebaseArticle == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _knowledgebaseViewModelService.DeleteKnowledgebaseArticle(knowledgebaseArticle);
                Success(_translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseArticle.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("EditArticle", new { knowledgebaseArticle.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ArticlesPopup(string articleId)
        {
            var model = new KnowledgebaseArticleModel.AddRelatedArticleModel
            {
                ArticleId = articleId
            };
            model.AvailableArticles.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            var articles = await _knowledgebaseService.GetKnowledgebaseArticles();
            foreach (var a in articles)
                model.AvailableArticles.Add(new SelectListItem { Text = a.Name, Value = a.Id.ToString() });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> RelatedArticlesAddPopupList(DataSourceRequest command, KnowledgebaseArticleModel.AddRelatedArticleModel model)
        {
            var articles = await _knowledgebaseService.GetKnowledgebaseArticlesByName(model.SearchArticleName, command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = articles.Select(x => x.ToModel()),
                Total = articles.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> RelatedArticlesList(DataSourceRequest command, string articleId)
        {
            var articles = await _knowledgebaseService.GetRelatedKnowledgebaseArticles(articleId, command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = articles.Select(x => new KnowledgebaseRelatedArticleGridModel
                {
                    Article2Id = x.Id,
                    DisplayOrder = x.DisplayOrder,
                    Published = x.Published,
                    Article2Name = x.Name,
                    Id = x.Id
                }),
                Total = articles.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ArticlesPopup(KnowledgebaseArticleModel.AddRelatedArticleModel model)
        {
            if (model.SelectedArticlesIds != null)
            {
                await _knowledgebaseViewModelService.InsertKnowledgebaseRelatedArticle(model);
            }
            ViewBag.RefreshPage = true;
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> RelatedArticleDelete(KnowledgebaseArticleModel.AddRelatedArticleModel model)
        {
            if (model.ArticleId == null || model.Id == null)
                throw new ArgumentNullException("Article id expected ");

            await _knowledgebaseViewModelService.DeleteKnowledgebaseRelatedArticle(model);
            return new JsonResult("");
        }
    }
}
