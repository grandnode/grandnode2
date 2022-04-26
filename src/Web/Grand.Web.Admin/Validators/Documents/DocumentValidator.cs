using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Documents;

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
