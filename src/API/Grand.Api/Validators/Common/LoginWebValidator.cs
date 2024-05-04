using FluentValidation;
using Grand.Api.Models.Common;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Common;

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
        {
            RuleFor(x => x).Must(_ => false).WithMessage("API is disabled");
        }
        else
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
            RuleFor(x => x).MustAsync(async (x, _) =>
            {
                if (!string.IsNullOrEmpty(x.Email))
                {
                    var customer = await customerService.GetCustomerByEmail(x.Email.ToLowerInvariant());
                    if (customer is { Active: true } && !customer.IsSystemAccount())
                    {
                        var base64EncodedBytes = Convert.FromBase64String(x.Password);
                        var password = Encoding.UTF8.GetString(base64EncodedBytes);
                        var result = await customerManagerService.LoginCustomer(x.Email, password);
                        return result == CustomerLoginResults.Successful;
                    }
                }

                return false;
            }).WithMessage("Customer not exist or password is wrong");
        }
    }
}