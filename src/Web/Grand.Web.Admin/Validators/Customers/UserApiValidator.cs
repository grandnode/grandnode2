using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Customers
{
    public class UserApiValidator : BaseGrandValidator<UserApiModel>
    {
        public UserApiValidator(
            IEnumerable<IValidatorConsumer<UserApiModel>> validators,
            ITranslationService translationService, ICustomerService customerService)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Admin.System.UserApi.Email.Required"));
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.Email))
                {
                    var customer = await customerService.GetCustomerByEmail(x.Email.ToLowerInvariant());
                    if (customer != null && customer.Active && !customer.IsSystemAccount)
                        return true;
                }
                return false;
            }).WithMessage(translationService.GetResource("Admin.System.UserApi.Email.CustomerNotExist")); ;
        }
    }
}