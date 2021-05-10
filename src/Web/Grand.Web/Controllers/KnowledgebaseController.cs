using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Extensions;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Knowledgebase;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public class KnowledgebaseController : BasePublicController
    {
        private readonly KnowledgebaseSettings _knowledgebaseSettings;
        private readonly IKnowledgebaseService _knowledgebaseService;
        private readonly IWorkContext _workContext;
        private readonly ICacheBase _cacheBase;
        private readonly IAclService _aclService;
        private readonly ITranslationService _translationService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPermissionService _permissionService;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly LanguageSettings _languageSettings;

        public KnowledgebaseController(
            KnowledgebaseSettings knowledgebaseSettings,
            IKnowledgebaseService knowledgebaseService,
            IWorkContext workContext,
            ICacheBase cacheBase,
            IAclService aclService,
            ITranslationService translationService,
            IMessageProviderService messageProviderService,
            ICustomerActivityService customerActivityService,
            IDateTimeService dateTimeService,
            IPermissionService permissionService,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            LanguageSettings languageSettings)
        {
            _knowledgebaseSettings = knowledgebaseSettings;
            _knowledgebaseService = knowledgebaseService;
            _workContext = workContext;
            _cacheBase = cacheBase;
            _aclService = aclService;
            _translationService = translationService;
            _captchaSettings = captchaSettings;
            _languageSettings = languageSettings;
            _messageProviderService = messageProviderService;
            _customerActivityService = customerActivityService;
            _dateTimeService = dateTimeService;
            _customerSettings = customerSettings;
            _permissionService = permissionService;
        }

        public virtual IActionResult List()
        {
            if (!_knowledgebaseSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = new KnowledgebaseHomePageModel();

            return View("List", model);
        }

        public virtual async Task<IActionResult> ArticlesByCategory(string categoryId)
        {
            if (!_knowledgebaseSettings.Enabled)
                return RedirectToRoute("HomePage");

            var category = await _knowledgebaseService.GetPublicKnowledgebaseCategory(categoryId);
            if (category == null)
                return RedirectToAction("List");

            var model = new KnowledgebaseHomePageModel();
            var articles = await _knowledgebaseService.GetPublicKnowledgebaseArticlesByCategory(categoryId);
            var allCategories = _knowledgebaseService.GetPublicKnowledgebaseCategories();
            articles.ForEach(x => model.Items.Add(new KnowledgebaseItemModel
            {
                Name = x.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id),
                Id = x.Id,
                SeName = x.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id),
                IsArticle = true
            }));

            //display "edit" (manage) link
            var customer = _workContext.CurrentCustomer;
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageKnowledgebase, customer))
                DisplayEditLink(Url.Action("EditCategory", "Knowledgebase", new { id = categoryId, area = "Admin" }));

            model.CurrentCategoryId = categoryId;

            model.CurrentCategoryDescription = category.GetTranslation(y => y.Description, _workContext.WorkingLanguage.Id);
            model.CurrentCategoryMetaDescription = category.GetTranslation(y => y.MetaDescription, _workContext.WorkingLanguage.Id);
            model.CurrentCategoryMetaKeywords = category.GetTranslation(y => y.MetaKeywords, _workContext.WorkingLanguage.Id);
            model.CurrentCategoryMetaTitle = category.GetTranslation(y => y.MetaTitle, _workContext.WorkingLanguage.Id);
            model.CurrentCategoryName = category.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id);
            model.CurrentCategorySeName = category.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id);

            string breadcrumbCacheKey = string.Format(CacheKeyConst.KNOWLEDGEBASE_CATEGORY_BREADCRUMB_KEY, category.Id,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()), _workContext.CurrentStore.Id, _workContext.WorkingLanguage.Id);
            model.CategoryBreadcrumb = await _cacheBase.GetAsync(breadcrumbCacheKey, async () =>
                (await category.GetCategoryBreadCrumb(_knowledgebaseService, _aclService, _workContext))
                .Select(catBr => new KnowledgebaseCategoryModel
                {
                    Id = catBr.Id,
                    Name = catBr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    SeName = catBr.GetSeName(_workContext.WorkingLanguage.Id)
                })
                .ToList()
            );

            return View("List", model);
        }

        public virtual async Task<IActionResult> ItemsByKeyword(string keyword)
        {
            if (!_knowledgebaseSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = new KnowledgebaseHomePageModel();

            if (!string.IsNullOrEmpty(keyword))
            {
                var categories = await _knowledgebaseService.GetPublicKnowledgebaseCategoriesByKeyword(keyword);
                var allCategories = await _knowledgebaseService.GetPublicKnowledgebaseCategories();
                categories.ForEach(x => model.Items.Add(new KnowledgebaseItemModel
                {
                    Name = x.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id),
                    Id = x.Id,
                    SeName = x.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id),
                    IsArticle = false,
                    FormattedBreadcrumbs = x.GetFormattedBreadCrumb(allCategories, ">")
                }));

                var articles = await _knowledgebaseService.GetPublicKnowledgebaseArticlesByKeyword(keyword);
                foreach (var item in articles)
                {
                    var kbm = new KnowledgebaseItemModel
                    {
                        Name = item.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id),
                        Id = item.Id,
                        SeName = item.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id),
                        IsArticle = true,
                        FormattedBreadcrumbs = (await _knowledgebaseService.GetPublicKnowledgebaseCategory(item.ParentCategoryId))?.GetFormattedBreadCrumb(allCategories, ">") +
                                        " > " + item.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id)
                    };
                    model.Items.Add(kbm);
                }
            }
            model.CurrentCategoryId = "[NONE]";
            model.SearchKeyword = keyword;

            return View("List", model);
        }

        public virtual async Task<IActionResult> KnowledgebaseArticle(string articleId, [FromServices] ICustomerService customerService)
        {
            if (!_knowledgebaseSettings.Enabled)
                return RedirectToRoute("HomePage");

            var customer = _workContext.CurrentCustomer;
            var article = await _knowledgebaseService.GetKnowledgebaseArticle(articleId);
            if (article == null)
                return RedirectToAction("List");

            //ACL (access control list)
            if (!_aclService.Authorize(article, customer))
                return InvokeHttp404();

            //Store acl
            if (!_aclService.Authorize(article, _workContext.CurrentStore.Id))
                return InvokeHttp404();

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageKnowledgebase, customer))
                DisplayEditLink(Url.Action("EditArticle", "Knowledgebase", new { id = article.Id, area = "Admin" }));

            var model = new KnowledgebaseArticleModel();

            await PrepareKnowledgebaseArticleModel(model, article, customerService);
            return View("Article", model);
        }

        private async Task PrepareKnowledgebaseArticleModel(KnowledgebaseArticleModel model, KnowledgebaseArticle article, ICustomerService customerService)
        {
            model.Content = article.GetTranslation(y => y.Content, _workContext.WorkingLanguage.Id);
            model.Name = article.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id);
            model.Id = article.Id;
            model.ParentCategoryId = article.ParentCategoryId;
            model.SeName = article.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id);
            model.AllowComments = article.AllowComments;
            model.AddNewComment.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnArticleCommentPage;

            model.MetaTitle = article.GetTranslation(y => y.MetaTitle, _workContext.WorkingLanguage.Id);
            model.MetaDescription = article.GetTranslation(y => y.MetaDescription, _workContext.WorkingLanguage.Id);
            model.MetaKeywords = article.GetTranslation(y => y.MetaKeywords, _workContext.WorkingLanguage.Id);

            var articleComments = await _knowledgebaseService.GetArticleCommentsByArticleId(article.Id);
            foreach (var ac in articleComments)
            {
                var customer = await customerService.GetCustomerById(ac.CustomerId);
                var commentModel = new KnowledgebaseArticleCommentModel
                {
                    Id = ac.Id,
                    CustomerId = ac.CustomerId,
                    CustomerName = customer.FormatUserName(_customerSettings.CustomerNameFormat),
                    CommentText = ac.CommentText,
                    CreatedOn = _dateTimeService.ConvertToUserTime(ac.CreatedOnUtc, DateTimeKind.Utc),
                };
                model.Comments.Add(commentModel);
            }

            foreach (var id in article.RelatedArticles)
            {
                var a = await _knowledgebaseService.GetPublicKnowledgebaseArticle(id);
                if (a != null)
                    model.RelatedArticles.Add(new KnowledgebaseArticleModel
                    {
                        SeName = a.SeName,
                        Id = a.Id,
                        Name = a.Name
                    });
            }

            var category = await _knowledgebaseService.GetKnowledgebaseCategory(article.ParentCategoryId);
            if (category != null)
            {
                string breadcrumbCacheKey = string.Format(CacheKeyConst.KNOWLEDGEBASE_CATEGORY_BREADCRUMB_KEY,
                article.ParentCategoryId,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id,
                _workContext.WorkingLanguage.Id);
                model.CategoryBreadcrumb = await _cacheBase.GetAsync(breadcrumbCacheKey, async () =>
                    (await category.GetCategoryBreadCrumb(_knowledgebaseService, _aclService, _workContext))
                    .Select(catBr => new KnowledgebaseCategoryModel
                    {
                        Id = catBr.Id,
                        Name = catBr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        SeName = catBr.GetSeName(_workContext.WorkingLanguage.Id)
                    })
                    .ToList()
                );
            }
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> ArticleCommentAdd(string articleId, KnowledgebaseArticleModel model, bool captchaValid,
               [FromServices] IWorkContext workContext,
               [FromServices] IGroupService groupService,
               [FromServices] ICustomerService customerService)
        {
            if (!_knowledgebaseSettings.Enabled)
                return RedirectToRoute("HomePage");

            var article = await _knowledgebaseService.GetPublicKnowledgebaseArticle(articleId);
            if (article == null || !article.AllowComments)
                return RedirectToRoute("HomePage");

            if (await groupService.IsGuest(workContext.CurrentCustomer) && !_knowledgebaseSettings.AllowNotRegisteredUsersToLeaveComments)
            {
                ModelState.AddModelError("", _translationService.GetResource("Knowledgebase.Article.Comments.OnlyRegisteredUsersLeaveComments"));
            }

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnArticleCommentPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (ModelState.IsValid)
            {
                var customer = _workContext.CurrentCustomer;
                var comment = new KnowledgebaseArticleComment
                {
                    ArticleId = article.Id,
                    CustomerId = customer.Id,
                    CommentText = model.AddNewComment.CommentText,
                    CreatedOnUtc = DateTime.UtcNow,
                    ArticleTitle = article.Name,
                };
                await _knowledgebaseService.InsertArticleComment(comment);

                if (!customer.HasContributions)
                {
                    await customerService.UpdateContributions(customer);
                }

                //notify a store owner
                if (_knowledgebaseSettings.NotifyAboutNewArticleComments)
                    await _messageProviderService.SendArticleCommentMessage(article, comment, _languageSettings.DefaultAdminLanguageId);

                //activity log
                await _customerActivityService.InsertActivity("PublicStore.AddArticleComment", comment.Id, _translationService.GetResource("ActivityLog.PublicStore.AddArticleComment"));

                //The text boxes should be cleared after a comment has been posted
                //That' why we reload the page
                TempData["Grand.knowledgebase.addarticlecomment.result"] = _translationService.GetResource("Knowledgebase.Article.Comments.SuccessfullyAdded");
                return RedirectToRoute("KnowledgebaseArticle", new { SeName = article.GetSeName(_workContext.WorkingLanguage.Id) });
            }

            //If we got this far, something failed, redisplay form
            await PrepareKnowledgebaseArticleModel(model, article, customerService);
            return View("Article", model);
        }
    }
}
