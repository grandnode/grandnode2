using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Module.Installer.Models;

namespace Grand.Module.Installer.Validators;

public class InstallValidator : BaseGrandValidator<InstallModel>
{
    public InstallValidator(
        IEnumerable<IValidatorConsumer<InstallModel>> validators)
        : base(validators)
    {
        RuleFor(x => x.AdminEmail).NotEmpty();
        RuleFor(x => x.AdminEmail).EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty();
        RuleFor(x => x.ConfirmPassword).NotEmpty();
        RuleFor(x => x.AdminPassword).Equal(x => x.ConfirmPassword)
            .WithMessage("Passwords must be equals");
    }
}