﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Knowledgebase;

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
