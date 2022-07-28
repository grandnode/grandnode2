using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Stores;

namespace Grand.Web.Admin.Validators.Stores
{
    public class StoreValidator : BaseGrandValidator<StoreModel>
    {
        public StoreValidator(
            IEnumerable<IValidatorConsumer<StoreModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Name.Required"));
            RuleFor(x => x.Shortcut).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Shortcut.Required"));
            RuleFor(x => x.Url).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Url.Required"));
            RuleFor(x => x.Url).Must((x, y, context) =>
            {
                try
                {
                    var storeUri = new Uri(x.Url);
                    return true;
                }
                catch
                {
                    return false;
                }
            }).WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Url.WrongFormat"));
            RuleFor(x => x.SecureUrl).Must((x, y, context) =>
            {
                try
                {
                    if (!x.SslEnabled)
                        return true;

                    var sslUri = new Uri(x.SecureUrl);

                    if (!sslUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                        return false;

                    var storeUri = new Uri(x.Url);
                    if (sslUri.Host != storeUri.Host)
                        return false;

                    return true;
                }
                catch
                {
                    return false;
                }
            }).WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.SecureUrl.WrongFormat"));

        }
    }
}