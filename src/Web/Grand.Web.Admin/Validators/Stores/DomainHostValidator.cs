using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Stores;
using System.Collections.Generic;
using System;

namespace Grand.Web.Admin.Validators.Stores
{
    public class DomainHostValidator : BaseGrandValidator<DomainHostModel>
    {
        public DomainHostValidator(
            IEnumerable<IValidatorConsumer<DomainHostModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Url).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Domains.Fields.Url.Required"));
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
            }).WithMessage(translationService.GetResource("Admin.Configuration.Stores.Domains.Fields.Url.WrongFormat"));
        }
    }
}