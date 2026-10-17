using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Stores;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Stores;

public class DomainHostValidator : BaseGrandValidator<DomainHostModel>
{
    public DomainHostValidator(
        IEnumerable<IValidatorConsumer<DomainHostModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Url).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Stores.Domains.Fields.Url.Required"));
        RuleFor(x => x.Url).Must((x, _, _) =>
        {
            try
            {
                var uri = new Uri(x.Url);
                return uri != null;
            }
            catch
            {
                return false;
            }
        }).WithMessage(translationService.GetResource("Admin.Configuration.Stores.Domains.Fields.Url.WrongFormat"));
    }
}