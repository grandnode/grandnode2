using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Permissions;

namespace Grand.Web.Admin.Validators.Permission;

public class PermissionUpdateModelValidator : BaseGrandValidator<PermissionUpdateModel>
{
    public PermissionUpdateModelValidator(
        IEnumerable<IValidatorConsumer<PermissionUpdateModel>> validators,
        IPermissionService permissionService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.Name.Required"));
        RuleFor(x => x.Area).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.Area.Required"));
        RuleFor(x => x.Category).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Permissions.Fields.Category.Required"));
        RuleFor(x => x.Id).CustomAsync(async (x, context, _) =>
        {
            var permission = await permissionService.GetPermissionById(x);
            if (permission == null)
                context.AddFailure(translationService.GetResource("Admin.Permissions.Fields.Id.NotExist"));
        });
    }
}