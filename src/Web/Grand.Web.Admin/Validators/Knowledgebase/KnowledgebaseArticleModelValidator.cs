using FluentValidation;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Knowledgebase;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Knowledgebase
{
    public class KnowledgebaseArticleModelValidator : BaseGrandValidator<KnowledgebaseArticleModel>
    {
        public KnowledgebaseArticleModelValidator(
            IEnumerable<IValidatorConsumer<KnowledgebaseArticleModel>> validators,
            ITranslationService translationService, IKnowledgebaseService knowledgebaseService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Name.Required"));
            RuleFor(x => x.ParentCategoryId).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId.Required"));
            RuleFor(x => x.ParentCategoryId).Must(x =>
            {
                var category = knowledgebaseService.GetKnowledgebaseCategory(x);
                if (category != null)
                {
                    return true;
                }

                return false;
            }).WithMessage(translationService.GetResource("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId.MustExist"));
        }
    }
}
