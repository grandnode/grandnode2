using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Documents;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Documents
{
    public class DocumentValidator : BaseGrandValidator<DocumentModel>
    {
        public DocumentValidator(
            IEnumerable<IValidatorConsumer<DocumentModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Documents.Document.Fields.Name.Required"));

            RuleFor(x => x.Number)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Documents.Document.Fields.Number.Required"));

        }
    }
}
