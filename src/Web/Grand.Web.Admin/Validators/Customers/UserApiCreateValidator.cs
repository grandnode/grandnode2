using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers;

public class UserApiCreateValidator : BaseGrandValidator<UserApiCreateModel>
{
    public UserApiCreateValidator(
        IEnumerable<IValidatorConsumer<UserApiCreateModel>> validators,
        ITranslationService translationService, ICustomerService customerService)
        : base(validators)
    {
        RuleFor(x => x.Password).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.System.UserApi.Password.Required"));
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
    }
}