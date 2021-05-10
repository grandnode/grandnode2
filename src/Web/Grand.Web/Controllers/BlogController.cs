using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Blogs;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Blogs;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Events;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class BlogController : BasePublicController
    {
        #region Fields

        private readonly IMediator _mediator;
        private readonly IBlogService _blogService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly BlogSettings _blogSettings;
        private readonly CaptchaSettings _captchaSettings;

        #endregion

        #region Constructors

        public BlogController(
            IMediator mediator,
            IBlogService blogService,
            ITranslationService translationService,
            IGroupService groupService,
            IWorkContext workContext,
            BlogSettings blogSettings,
            CaptchaSettings captchaSettings)
        {
            _mediator = mediator;
            _blogService = blogService;
            _translationService = translationService;
            _blogSettings = blogSettings;
            _captchaSettings = captchaSettings;
            _groupService = groupService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> List(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetBlogPostList() { Command = command });
            return View("List", model);
        }
        public virtual async Task<IActionResult> BlogByTag(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetBlogPostList() { Command = command });
            return View("List", model);
        }
        public virtual async Task<IActionResult> BlogByMonth(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetBlogPostList() { Command = command });
            return View("List", model);
        }
        public virtual async Task<IActionResult> BlogByCategory(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetBlogPostList() { Command = command });
            return View("List", model);
        }
        public virtual async Task<IActionResult> BlogByKeyword(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetBlogPostList() { Command = command });
            return View("List", model);
        }
        public virtual async Task<IActionResult> BlogPost(string blogPostId,
            [FromServices] IAclService aclService,
            [FromServices] IPermissionService permissionService)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var blogPost = await _blogService.GetBlogPostById(blogPostId);
            if (blogPost == null ||
                (blogPost.StartDateUtc.HasValue && blogPost.StartDateUtc.Value >= DateTime.UtcNow) ||
                (blogPost.EndDateUtc.HasValue && blogPost.EndDateUtc.Value <= DateTime.UtcNow))
                return RedirectToRoute("HomePage");

            //Store acl
            if (!aclService.Authorize(blogPost, _workContext.CurrentStore.Id))
                return InvokeHttp404();

            var model = await _mediator.Send(new GetBlogPost() { BlogPost = blogPost });

            //display "edit" (manage) link
            if (await permissionService.Authorize(StandardPermission.AccessAdminPanel) && await permissionService.Authorize(StandardPermission.ManageBlog))
                DisplayEditLink(Url.Action("Edit", "Blog", new { id = blogPost.Id, area = "Admin" }));

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> BlogCommentAdd(string blogPostId, BlogPostModel model, bool captchaValid)
        {
            if (!_blogSettings.Enabled)
                return RedirectToRoute("HomePage");

            var blogPost = await _blogService.GetBlogPostById(blogPostId);
            if (blogPost == null || !blogPost.AllowComments)
                return RedirectToRoute("HomePage");

            if (await _groupService.IsGuest(_workContext.CurrentCustomer) && !_blogSettings.AllowNotRegisteredUsersToLeaveComments)
            {
                ModelState.AddModelError("", _translationService.GetResource("Blog.Comments.OnlyRegisteredUsersLeaveComments"));
            }

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnBlogCommentPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (ModelState.IsValid)
            {
                await _mediator.Send(new InsertBlogCommentCommand() { Model = model, BlogPost = blogPost });

                //notification
                await _mediator.Publish(new BlogCommentEvent(blogPost, model.AddNewComment));

                //The text boxes should be cleared after a comment has been posted
                TempData["Grand.blog.addcomment.result"] = _translationService.GetResource("Blog.Comments.SuccessfullyAdded");
                return RedirectToRoute("BlogPost", new { SeName = blogPost.GetSeName(_workContext.WorkingLanguage.Id) });
            }

            //If we got this far, something failed, redisplay form
            model = await _mediator.Send(new GetBlogPost() { BlogPost = blogPost });
            return View("BlogPost", model);
        }
        #endregion
    }
}
