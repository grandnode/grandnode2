using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Documents;

namespace Grand.Web.Admin.Validators.Documents
{
    public class DocumentTypeValidator : BaseGrandValidator<DocumentTypeModel>
    {
        public DocumentTypeValidator(
            IEnumerable<IValidatorConsumer<DocumentTypeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Documents.Type.Fields.Name.Required"));
        }
    }
}
