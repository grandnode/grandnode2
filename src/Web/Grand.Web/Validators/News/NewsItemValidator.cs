using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.News;
using System.Collections.Generic;

namespace Grand.Web.Validators.News
{
    public class NewsItemValidator : BaseGrandValidator<AddNewsCommentModel>
    {
        public NewsItemValidator(
            IEnumerable<IValidatorConsumer<AddNewsCommentModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.CommentTitle).NotEmpty().WithMessage(translationService.GetResource("News.Comments.CommentTitle.Required"));
            RuleFor(x => x.CommentTitle).Length(1, 200).WithMessage(string.Format(translationService.GetResource("News.Comments.CommentTitle.MaxLengthValidation"), 200));
            RuleFor(x => x.CommentText).NotEmpty().WithMessage(translationService.GetResource("News.Comments.CommentText.Required"));
        }}
}