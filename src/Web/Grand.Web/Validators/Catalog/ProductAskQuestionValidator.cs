using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Catalog;
using System.Collections.Generic;

namespace Grand.Web.Validators.Catalog
{
    public class ProductAskQuestionValidator : BaseGrandValidator<ProductAskQuestionModel>
    {
        public ProductAskQuestionValidator(
            IEnumerable<IValidatorConsumer<ProductAskQuestionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Products.AskQuestion.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.Message).NotEmpty().WithMessage(translationService.GetResource("Products.AskQuestion.Message.Required"));
            RuleFor(x => x.FullName).NotEmpty().WithMessage(translationService.GetResource("Products.AskQuestion.FullName.Required"));
        }
    }
}