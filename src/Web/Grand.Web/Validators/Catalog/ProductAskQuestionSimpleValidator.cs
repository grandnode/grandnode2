using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Models.Catalog;

namespace Grand.Web.Validators.Catalog
{
    public class ProductAskQuestionSimpleValidator : BaseGrandValidator<ProductAskQuestionSimpleModel>
    {
        public ProductAskQuestionSimpleValidator(
            IEnumerable<IValidatorConsumer<ProductAskQuestionSimpleModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.AskQuestionEmail).NotEmpty().WithMessage(translationService.GetResource("Products.AskQuestion.Email.Required"));
            RuleFor(x => x.AskQuestionEmail).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.AskQuestionMessage).NotEmpty().WithMessage(translationService.GetResource("Products.AskQuestion.Message.Required"));
            RuleFor(x => x.AskQuestionFullName).NotEmpty().WithMessage(translationService.GetResource("Products.AskQuestion.FullName.Required"));
        }
    }
}