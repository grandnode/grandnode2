using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Documents;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Documents;

public class DocumentValidator : BaseGrandValidator<DocumentModel>
{
    public DocumentValidator(
        IEnumerable<IValidatorConsumer<DocumentModel>> validators,
        IRequestTranslationService translationService)
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