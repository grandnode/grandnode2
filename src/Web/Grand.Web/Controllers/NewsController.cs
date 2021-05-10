using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.News;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Events;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.News;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class NewsController : BasePublicController
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
        private readonly CaptchaSettings _captchaSettings;

        #endregion

        #region Constructors

        public NewsController(INewsService newsService,
            IWorkContext workContext,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            IAclService aclService,
            IPermissionService permissionService,
            IMediator mediator,
            NewsSettings newsSettings,
            CaptchaSettings captchaSettings)
        {
            _newsService = newsService;
            _workContext = workContext;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _aclService = aclService;
            _permissionService = permissionService;
            _mediator = mediator;
            _newsSettings = newsSettings;
            _captchaSettings = captchaSettings;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> List(NewsPagingFilteringModel command)
        {
            if (!_newsSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetNewsItemList() { Command = command });
            return View(model);
        }
        public virtual async Task<IActionResult> NewsItem(string newsItemId)
        {
            if (!_newsSettings.Enabled)
                return RedirectToRoute("HomePage");

            var newsItem = await _newsService.GetNewsById(newsItemId);
            if (newsItem == null ||
                !newsItem.Published ||
                (newsItem.StartDateUtc.HasValue && newsItem.StartDateUtc.Value >= DateTime.UtcNow) ||
                (newsItem.EndDateUtc.HasValue && newsItem.EndDateUtc.Value <= DateTime.UtcNow) ||
                //Store acl
                !_aclService.Authorize(newsItem, _workContext.CurrentStore.Id))
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetNewsItem() { NewsItem = newsItem });

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel) && await _permissionService.Authorize(StandardPermission.ManageNews))
                DisplayEditLink(Url.Action("Edit", "News", new { id = newsItem.Id, area = "Admin" }));

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> NewsCommentAdd(string newsItemId,
            NewsItemModel model, bool captchaValid,
            [FromServices] IGroupService groupService
            )
        {
            if (!_newsSettings.Enabled)
                return RedirectToRoute("HomePage");

            var newsItem = await _newsService.GetNewsById(newsItemId);
            if (newsItem == null || !newsItem.Published || !newsItem.AllowComments)
                return RedirectToRoute("HomePage");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnNewsCommentPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (await groupService.IsGuest(_workContext.CurrentCustomer) && !_newsSettings.AllowNotRegisteredUsersToLeaveComments)
            {
                ModelState.AddModelError("", _translationService.GetResource("News.Comments.OnlyRegisteredUsersLeaveComments"));
            }

            if (ModelState.IsValid)
            {
                await _mediator.Send(new InsertNewsCommentCommand() { NewsItem = newsItem, Model = model });

                //notification
                await _mediator.Publish(new NewsCommentEvent(newsItem, model.AddNewComment));

                //activity log
                await _customerActivityService.InsertActivity("PublicStore.AddNewsComment", newsItem.Id, _translationService.GetResource("ActivityLog.PublicStore.AddNewsComment"));

                //The text boxes should be cleared after a comment has been posted
                TempData["Grand.news.addcomment.result"] = _translationService.GetResource("News.Comments.SuccessfullyAdded");
                return RedirectToRoute("NewsItem", new { SeName = newsItem.GetSeName(_workContext.WorkingLanguage.Id) });
            }

            model = await _mediator.Send(new GetNewsItem() { NewsItem = newsItem });
            return View("NewsItem", model);
        }
        #endregion
    }
}
