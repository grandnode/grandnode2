using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Knowledgebase;
using System.Collections.Generic;

namespace Grand.Web.Validators.Knowledgebase
{
    public class KnowledgebaseArticleValidator : BaseGrandValidator<KnowledgebaseArticleModel>
    {
        public KnowledgebaseArticleValidator(
            IEnumerable<IValidatorConsumer<KnowledgebaseArticleModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.AddNewComment.CommentText).NotEmpty().WithMessage(translationService.GetResource("Grand.knowledgebase.addarticlecomment.result")).When(x => x.AddNewComment != null);
        }
    }
}