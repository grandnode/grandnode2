using FluentValidation;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.News;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.News;

public class AddNewsCommentValidator : BaseGrandValidator<AddNewsCommentModel>
{
    public AddNewsCommentValidator(
        IEnumerable<IValidatorConsumer<AddNewsCommentModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IWorkContext workContext, IGroupService groupService, INewsService newsService,
        CaptchaSettings captchaSettings, NewsSettings newsSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.CommentTitle).NotEmpty()
            .WithMessage(translationService.GetResource("News.Comments.CommentTitle.Required"));
        RuleFor(x => x.CommentTitle).Length(1, 200).WithMessage(
            string.Format(translationService.GetResource("News.Comments.CommentTitle.MaxLengthValidation"), 200));
        RuleFor(x => x.CommentText).NotEmpty()
            .WithMessage(translationService.GetResource("News.Comments.CommentText.Required"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !newsSettings.AllowNotRegisteredUsersToLeaveComments)
                context.AddFailure(translationService.GetResource("News.Comments.OnlyRegisteredUsersLeaveComments"));
            var newsItem = await newsService.GetNewsById(x.Id);
            if (newsItem is not { Published: true } || !newsItem.AllowComments)
                context.AddFailure(translationService.GetResource("News.Comments.NotAllowed"));
        });
        if (captchaSettings.Enabled && captchaSettings.ShowOnNewsCommentPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}