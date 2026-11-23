using FluentValidation;
using Grand.Module.Api.Models.Common;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Mediator;

namespace Grand.Module.Api.Validators.Common;

public class LoginWebValidator : BaseGrandValidator<LoginWebModel>
{
    public LoginWebValidator(
        IEnumerable<IValidatorConsumer<LoginWebModel>> validators,
        FrontendAPIConfig apiConfig,
        CustomerSettings customerSettings,
        ICustomerService customerService,
        ICustomerManagerService customerManagerService,
        IContextAccessor contextAccessor,
        IMediator mediator)
        : base(validators)
    {
        var storeId = contextAccessor.StoreContext.CurrentStore.Id;

        if (!apiConfig.Enabled)
        {
            RuleFor(x => x).Must(_ => false).WithMessage("API is disabled");
        }
        else
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
            RuleFor(x => x).MustAsync(async (x, cancellationToken) =>
            {
                if (!string.IsNullOrEmpty(x.Email))
                {
                    var customer = await customerService.GetCustomerByEmail(x.Email.ToLowerInvariant(), storeId);
                    //a locked-out account cannot sign in here either, as on the storefront login page
                    if (customer is { Active: true, Deleted: false } && !customer.IsSystemAccount() &&
                        !(customer.CannotLoginUntilDateUtc > DateTime.UtcNow))
                    {
                        string password;
                        try
                        {
                            password = Encoding.UTF8.GetString(Convert.FromBase64String(x.Password));
                        }
                        catch (FormatException)
                        {
                            return false;
                        }

                        //LoginCustomer looks the customer up by username when usernames are enabled
                        var result = await customerManagerService.LoginCustomer(
                            customerSettings.UsernamesEnabled ? customer.Username : x.Email, password, storeId,
                            SignInSurface.FrontendApi);
                        //counted like a wrong password on the login page, so lockout applies to this sign-in too
                        if (result == CustomerLoginResults.WrongPassword)
                            await mediator.Publish(new CustomerLoginFailedEvent(customer, "wrong credentials"),
                                cancellationToken);
                        if (result != CustomerLoginResults.Successful)
                            return false;

                        //a successful sign-in clears the failed attempts, as the login page's does
                        customer.FailedLoginAttempts = 0;
                        customer.CannotLoginUntilDateUtc = null;
                        customer.LastLoginDateUtc = DateTime.UtcNow;
                        await customerService.UpdateCustomerLastLoginDate(customer);
                        return true;
                    }
                }

                return false;
            }).WithMessage("Customer not exist or password is wrong");
        }
    }
}