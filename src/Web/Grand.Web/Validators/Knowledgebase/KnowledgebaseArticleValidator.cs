using FluentValidation;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Knowledgebase;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Knowledgebase;

public class KnowledgebaseArticleValidator : BaseGrandValidator<KnowledgebaseArticleModel>
{
    public KnowledgebaseArticleValidator(
        IEnumerable<IValidatorConsumer<KnowledgebaseArticleModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IWorkContext workContext, IGroupService groupService,
        IKnowledgebaseService knowledgebaseService, KnowledgebaseSettings knowledgebaseSettings,
        CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.AddNewComment.CommentText).NotEmpty()
            .WithMessage(translationService.GetResource("Grand.knowledgebase.addarticlecomment.result"))
            .When(x => x.AddNewComment != null);

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !knowledgebaseSettings.AllowNotRegisteredUsersToLeaveComments)
                context.AddFailure(
                    translationService.GetResource("Knowledgebase.Article.Comments.OnlyRegisteredUsersLeaveComments"));
            var article = await knowledgebaseService.GetPublicKnowledgebaseArticle(x.ArticleId);
            if (article is not { AllowComments: true })
                context.AddFailure(translationService.GetResource("Knowledgebase.Article.Comments.NotAllowed"));
        });

        if (captchaSettings.Enabled && captchaSettings.ShowOnArticleCommentPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}