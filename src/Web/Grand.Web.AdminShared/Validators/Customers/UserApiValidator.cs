using FluentValidation;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Localization;

#pragma warning disable CS0618 //API users stay supported until removed

namespace Grand.Web.AdminShared.Validators.Customers;

public class UserApiValidator : BaseGrandValidator<UserApiModel>
{
    public UserApiValidator(
        IEnumerable<IValidatorConsumer<UserApiModel>> validators,
        IRequestTranslationService translationService, ICustomerService customerService,
        IUserApiService userApiService)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.System.UserApi.Email.Required"));
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.Email))
            {
                var customer = await customerService.GetCustomerByEmail(x.Email.ToLowerInvariant());
                if (customer is { Active: true, IsSystemAccount: false })
                    return true;
            }

            return false;
        }).WithMessage(translationService.GetResource("Admin.System.UserApi.Email.CustomerNotExist"));
        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            if (string.IsNullOrEmpty(x.Email))
                return true;

            var user = await userApiService.GetUserByEmail(x.Email);
            return user == null || user.Id == x.Id;
        }).WithMessage(translationService.GetResource("Admin.System.UserApi.Email.Registered"));
    }
}