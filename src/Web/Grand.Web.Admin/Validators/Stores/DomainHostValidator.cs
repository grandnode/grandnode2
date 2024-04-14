using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Stores;

namespace Grand.Web.Admin.Validators.Stores;

public class DomainHostValidator : BaseGrandValidator<DomainHostModel>
{
    public DomainHostValidator(
        IEnumerable<IValidatorConsumer<DomainHostModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Url).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Stores.Domains.Fields.Url.Required"));
        RuleFor(x => x.Url).Must((x, _, _) =>
        {
            try
            {
                new Uri(x.Url);
                return true;
            }
            catch
            {
                return false;
            }
        }).WithMessage(translationService.GetResource("Admin.Configuration.Stores.Domains.Fields.Url.WrongFormat"));
    }
}