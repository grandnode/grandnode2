using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Validators.Orders;

public class MerchandiseReturnReasonValidator : BaseGrandValidator<MerchandiseReturnReasonDto>
{
    public MerchandiseReturnReasonValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnReasonDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IMerchandiseReturnService merchandiseReturnService)
        : base(validators)
    {
        var languageId = contextAccessor.WorkingLanguageId();

        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Api.Orders.MerchandiseReturnReason.Fields.Name.Required",
                languageId));
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var reason = await merchandiseReturnService.GetMerchandiseReturnReasonById(x.Id);
                if (reason == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Orders.MerchandiseReturnReason.Fields.Id.NotExists",
            languageId));
    }
}
