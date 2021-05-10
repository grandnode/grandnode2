using FluentValidation;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Knowledgebase;
using System.Collections.Generic;
using System.Linq;

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
                var category = await knowledgebaseService.GetKnowledgebaseCategory(x.ParentCategoryId);
                if (category != null || string.IsNullOrEmpty(x.ParentCategoryId))
                {
                    return true;
                }
                else
                {
                    var categories = await knowledgebaseService.GetKnowledgebaseCategories();
                    if (!categories.Any())
                    {
                        return true;
                    }
                }

                return false;
            }).WithMessage(translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.ParentCategoryId.MustExist"));
        }
    }
}
