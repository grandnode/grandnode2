using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Models.Orders;

namespace Grand.Web.Validators.Customer
{
    public class AddOrderNoteValidator : BaseGrandValidator<AddOrderNoteModel>
    {
        public AddOrderNoteValidator(
            IEnumerable<IValidatorConsumer<AddOrderNoteModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Note).NotEmpty().WithMessage(translationService.GetResource("OrderNote.Fields.Title.Required"));
        }
    }
}
