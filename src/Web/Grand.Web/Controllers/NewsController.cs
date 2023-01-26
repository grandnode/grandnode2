using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.News;
using Grand.Web.Common.Filters;
using Grand.Web.Events;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.News;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    public class NewsController : BasePublicController
    {
        #region Fields

        private readonly INewsService _newsService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IAclService _aclService;
        private readonly IPermissionService _permissionService;
        private readonly IMediator _mediator;
        private readonly NewsSettings _newsSettings;

        #endregion

        #region Constructors

        public NewsController(INewsService newsService,
            IWorkContext workContext,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            IAclService aclService,
            IPermissionService permissionService,
            IMediator mediator,
            NewsSettings newsSettings)
        {
            _newsService = newsService;
            _workContext = workContext;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _aclService = aclService;
            _permissionService = permissionService;
            _mediator = mediator;
            _newsSettings = newsSettings;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> List(NewsPagingFilteringModel command)
        {
            if (!_newsSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetNewsItemList { Command = command });
            return View(model);
        }
        public virtual async Task<IActionResult> NewsItem(string newsItemId)
        {
            if (!_newsSettings.Enabled)
                return RedirectToRoute("HomePage");

            var newsItem = await _newsService.GetNewsById(newsItemId);
            if (newsItem is not { Published: true } ||
                (newsItem.StartDateUtc.HasValue && newsItem.StartDateUtc.Value >= DateTime.UtcNow) ||
                (newsItem.EndDateUtc.HasValue && newsItem.EndDateUtc.Value <= DateTime.UtcNow) ||
                //Store acl
                !_aclService.Authorize(newsItem, _workContext.CurrentStore.Id))
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetNewsItem { NewsItem = newsItem });

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel) && await _permissionService.Authorize(StandardPermission.ManageNews))
                DisplayEditLink(Url.Action("Edit", "News", new { id = newsItem.Id, area = "Admin" }));

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [DenySystemAccount]
        public virtual async Task<IActionResult> NewsCommentAdd(NewsItemModel model)
        {
            if (!_newsSettings.Enabled)
                return RedirectToRoute("HomePage");

            var newsItem = await _newsService.GetNewsById(model.NewsItemId);
            if (newsItem is not { Published: true } || !newsItem.AllowComments)
                return RedirectToRoute("HomePage");

            if (ModelState.IsValid)
            {
                await _mediator.Send(new InsertNewsCommentCommand { NewsItem = newsItem, Model = model });

                //notification
                await _mediator.Publish(new NewsCommentEvent(newsItem, model.AddNewComment));

                //activity log
                _ = _customerActivityService.InsertActivity("PublicStore.AddNewsComment", newsItem.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.PublicStore.AddNewsComment"));

                //The text boxes should be cleared after a comment has been posted
                TempData["Grand.news.addcomment.result"] = _translationService.GetResource("News.Comments.SuccessfullyAdded");
                return RedirectToRoute("NewsItem", new { SeName = newsItem.GetSeName(_workContext.WorkingLanguage.Id) });
            }

            model = await _mediator.Send(new GetNewsItem { NewsItem = newsItem });
            return View("NewsItem", model);
        }
        #endregion
    }
}
