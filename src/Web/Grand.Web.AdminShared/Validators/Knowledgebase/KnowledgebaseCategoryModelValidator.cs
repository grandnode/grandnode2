using FluentValidation;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Knowledgebase;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Knowledgebase;

public class KnowledgebaseCategoryModelValidator : BaseGrandValidator<KnowledgebaseCategoryModel>
{
    public KnowledgebaseCategoryModelValidator(
        IEnumerable<IValidatorConsumer<KnowledgebaseCategoryModel>> validators,
        IRequestTranslationService translationService, IKnowledgebaseService knowledgebaseService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Name.Required"));
        RuleFor(x => x.ParentCategoryId).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.ParentCategoryId))
            {
                if (x.Id == x.ParentCategoryId)
                    return false;

                var category = await knowledgebaseService.GetKnowledgebaseCategory(x.ParentCategoryId);
                if (category == null) return false;
            }

            return true;
        }).WithMessage(
            translationService.GetResource(
                "Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.ParentCategoryId.MustExist"));
    }
}