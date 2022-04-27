using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Knowledgebase;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Knowledgebase;

namespace Grand.Web.Admin.Validators.Knowledgebase
{
    public class KnowledgebaseCategoryModelValidator : BaseGrandValidator<KnowledgebaseCategoryModel>
    {
        public KnowledgebaseCategoryModelValidator(
            IEnumerable<IValidatorConsumer<KnowledgebaseCategoryModel>> validators,
            ITranslationService translationService, IKnowledgebaseService knowledgebaseService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Name.Required"));
            RuleFor(x => x.ParentCategoryId).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.ParentCategoryId))
                {
                    if (x.Id == x.ParentCategoryId)
                        return false;

                    var category = await knowledgebaseService.GetKnowledgebaseCategory(x.ParentCategoryId);
                    if (category == null)
                    {
                        return false;
                    }
                }

                return true;
            }).WithMessage(translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.ParentCategoryId.MustExist"));
        }
    }
}
