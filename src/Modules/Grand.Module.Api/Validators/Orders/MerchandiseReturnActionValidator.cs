using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Validators.Orders;

public class MerchandiseReturnActionValidator : BaseGrandValidator<MerchandiseReturnActionDto>
{
    public MerchandiseReturnActionValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnActionDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IMerchandiseReturnService merchandiseReturnService)
        : base(validators)
    {
        var languageId = contextAccessor.WorkingLanguageId();

        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Api.Orders.MerchandiseReturnAction.Fields.Name.Required",
                languageId));
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var action = await merchandiseReturnService.GetMerchandiseReturnActionById(x.Id);
                if (action == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Orders.MerchandiseReturnAction.Fields.Id.NotExists",
            languageId));
    }
}
