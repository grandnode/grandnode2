using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Catalog
{
    public class ProductTierPriceValidator : BaseGrandValidator<ProductTierPriceDto>
    {
        public ProductTierPriceValidator(
            IEnumerable<IValidatorConsumer<ProductTierPriceDto>> validators,
            ITranslationService translationService, IStoreService storeService, IGroupService groupService)
            : base(validators)
        {
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(translationService.GetResource("Api.Catalog.ProductTierPrice.Fields.Quantity.GreaterThan0"));
            RuleFor(x => x.Price).GreaterThan(0).WithMessage(translationService.GetResource("Api.Catalog.ProductTierPrice.Fields.Price.GreaterThan0"));

            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.StoreId))
                {
                    var store = await storeService.GetStoreById(x.StoreId);
                    if (store == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductTierPrice.Fields.StoreId.NotExists"));
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.CustomerGroupId))
                {
                    var group = await groupService.GetCustomerGroupById(x.CustomerGroupId);
                    if (group == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductTierPrice.Fields.CustomerGroupId.NotExists"));
        }
    }
}
