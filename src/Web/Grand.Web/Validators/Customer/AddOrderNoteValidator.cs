using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Orders;
using System.Collections.Generic;

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
