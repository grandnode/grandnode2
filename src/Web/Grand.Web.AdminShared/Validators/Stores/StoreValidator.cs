using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Stores;

namespace Grand.Web.AdminShared.Validators.Stores;

public class StoreValidator : BaseGrandValidator<StoreModel>
{
    public StoreValidator(
        IEnumerable<IValidatorConsumer<StoreModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Name.Required"));
        RuleFor(x => x.Shortcut).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Shortcut.Required"));
        RuleFor(x => x.Url).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Url.Required"));
        RuleFor(x => x.Url).Must((x, _, _) =>
        {
            //the store url is used to build absolute links outside of a request (emails, sitemap, robots.txt),
            //so it has to be an absolute http(s) url
            if (!Uri.TryCreate(x.Url, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                   uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        }).WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Url.WrongFormat"));
    }
}