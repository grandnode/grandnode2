using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Permissions;

namespace Grand.Web.Admin.Validators.Permission;

public class PermissionCreateModelValidator : BaseGrandValidator<PermissionCreateModel>
{
    public PermissionCreateModelValidator(
        IEnumerable<IValidatorConsumer<PermissionCreateModel>> validators,
        IPermissionService permissionService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.Name.Required"));
        RuleFor(x => x.SystemName).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.SystemName.Required"));
        RuleFor(x => x.Area).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.Area.Required"));
        RuleFor(x => x.Category).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.Category.Required"));
        RuleFor(x => x.SystemName).CustomAsync(async (x, context, _) =>
        {
            var permissionBySystemName = await permissionService.GetPermissionBySystemName(x);
            if (permissionBySystemName != null)
                context.AddFailure(translationService.GetResource("Admin.Permissions.Fields.SystemName.Exist"));
        });
    }
}