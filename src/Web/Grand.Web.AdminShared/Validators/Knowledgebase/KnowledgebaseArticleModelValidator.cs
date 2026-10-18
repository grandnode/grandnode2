using FluentValidation;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Knowledgebase;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Knowledgebase;

public class KnowledgebaseArticleModelValidator : BaseGrandValidator<KnowledgebaseArticleModel>
{
    public KnowledgebaseArticleModelValidator(
        IEnumerable<IValidatorConsumer<KnowledgebaseArticleModel>> validators,
        IRequestTranslationService translationService, IKnowledgebaseService knowledgebaseService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Name.Required"));
        RuleFor(x => x.ParentCategoryId).NotEmpty().WithMessage(
            translationService.GetResource(
                "Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId.Required"));
        RuleFor(x => x.ParentCategoryId).Must(x =>
        {
            var category = knowledgebaseService.GetKnowledgebaseCategory(x);
            if (category != null) return true;

            return false;
        }).WithMessage(
            translationService.GetResource(
                "Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId.MustExist"));
    }
}