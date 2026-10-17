using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Orders;
using Grand.Web.Common.Localization;

namespace Grand.Web.Validators.Customer;

public class AddOrderNoteValidator : BaseGrandValidator<AddOrderNoteModel>
{
    public AddOrderNoteValidator(
        IEnumerable<IValidatorConsumer<AddOrderNoteModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Note).NotEmpty().WithMessage(translationService.GetResource("OrderNote.Fields.Title.Required"));
    }
}