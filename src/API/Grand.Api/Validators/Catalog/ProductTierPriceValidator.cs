using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;

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
