using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Documents;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Documents;

public class DocumentTypeValidator : BaseGrandValidator<DocumentTypeModel>
{
    public DocumentTypeValidator(
        IEnumerable<IValidatorConsumer<DocumentTypeModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Documents.Type.Fields.Name.Required"));
    }
}