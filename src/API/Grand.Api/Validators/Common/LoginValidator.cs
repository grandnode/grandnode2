using FluentValidation;
using Grand.Api.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Common;

public class LoginValidator : BaseGrandValidator<LoginModel>
{
    public LoginValidator(
        IEnumerable<IValidatorConsumer<LoginModel>> validators,
        BackendAPIConfig apiConfig, ICustomerService customerService, IUserApiService userApiService,
        IEncryptionService encryptionService)
        : base(validators)
    {
        if (!apiConfig.Enabled)
            RuleFor(x => x).Must(_ => false).WithMessage("API is disabled");

        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            if (!string.IsNullOrEmpty(x.Email))
            {
                var userapi = await userApiService.GetUserByEmail(x.Email.ToLowerInvariant());
                if (userapi is { IsActive: true })
                {
                    var base64EncodedBytes = Convert.FromBase64String(x.Password);
                    var password = Encoding.UTF8.GetString(base64EncodedBytes);
                    if (userapi.Password == encryptionService.EncryptText(password, userapi.PrivateKey))
                        return true;
                }
            }

            return false;
        }).WithMessage("User not exists or password is wrong");
        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            if (!string.IsNullOrEmpty(x.Email))
            {
                var customer = await customerService.GetCustomerByEmail(x.Email.ToLowerInvariant());
                if (customer is { Active: true } && !customer.IsSystemAccount())
                    return true;
            }

            return false;
        }).WithMessage("Customer not exist");
    }
}