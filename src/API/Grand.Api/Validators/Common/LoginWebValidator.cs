using FluentValidation;
using Grand.Api.Models.Common;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Common
{
    public class LoginWebValidator : BaseGrandValidator<LoginWebModel>
    {
        public LoginWebValidator(
            IEnumerable<IValidatorConsumer<LoginWebModel>> validators,
            FrontendAPIConfig apiConfig,
            ICustomerService customerService,
            ICustomerManagerService customerManagerService)
            : base(validators)
        {
            if (!apiConfig.Enabled)
                RuleFor(x => x).Must((x) => false).WithMessage("API is disabled");
            else
            {
                RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
                RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
                RuleFor(x => x).MustAsync(async (x, context) =>
                {
                    if (!string.IsNullOrEmpty(x.Email))
                    {
                        var customer = await customerService.GetCustomerByEmail(x.Email.ToLowerInvariant());
                        if (customer != null && customer.Active && !customer.IsSystemAccount())
                        {
                            var base64EncodedBytes = System.Convert.FromBase64String(x.Password);
                            var password = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                            var result = await customerManagerService.LoginCustomer(x.Email, password);
                            if (result == CustomerLoginResults.Successful)
                                return true;

                            return false;
                        }
                    }
                    return false;
                }).WithMessage("Customer not exist or password is wrong");
            }
        }
    }
}
