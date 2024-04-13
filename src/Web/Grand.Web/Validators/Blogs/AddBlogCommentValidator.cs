using FluentValidation;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Blogs;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Blogs;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Blogs;

public class AddBlogCommentValidator : BaseGrandValidator<AddBlogCommentModel>
{
    public AddBlogCommentValidator(
        IEnumerable<IValidatorConsumer<AddBlogCommentModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        CaptchaSettings captchaSettings, IHttpContextAccessor contextAccessor,
        GoogleReCaptchaValidator googleReCaptchaValidator,
        BlogSettings blogSettings,
        IGroupService groupService, IWorkContext workContext, IBlogService blogService, IAclService aclService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.CommentText).NotEmpty()
            .WithMessage(translationService.GetResource("Blog.Comments.CommentText.Required"));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !blogSettings.AllowNotRegisteredUsersToLeaveComments)
                context.AddFailure(
                    translationService.GetResource("Blog.Comments.OnlyRegisteredUsersLeaveComments"));

            if (!blogSettings.Enabled)
                context.AddFailure(
                    translationService.GetResource("Blog.Disabled"));

            var blogPost = await blogService.GetBlogPostById(x.Id);
            if (blogPost is not { AllowComments: true })
                context.AddFailure(translationService.GetResource("Blog.Comments.NotAllowed"));

            if (!aclService.Authorize(blogPost, workContext.CurrentStore.Id))
                context.AddFailure(translationService.GetResource("Blog.Comments.NotAllowed"));

            if (blogPost == null ||
                (blogPost.StartDateUtc.HasValue && blogPost.StartDateUtc.Value >= DateTime.UtcNow) ||
                (blogPost.EndDateUtc.HasValue && blogPost.EndDateUtc.Value <= DateTime.UtcNow))
                context.AddFailure(translationService.GetResource("Blog.Comments.NotAllowed"));
        });

        if (captchaSettings.Enabled && captchaSettings.ShowOnBlogCommentPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}