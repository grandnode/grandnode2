using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog;

public class ProductValidator : BaseGrandValidator<ProductModel>
{
    public ProductValidator(
        IEnumerable<IValidatorConsumer<ProductModel>> validators,
        ITranslationService translationService, CommonSettings commonSettings)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Catalog.Products.Fields.Name.Required"));
        if (!commonSettings.AllowEditProductEndedAuction)
            RuleFor(x => x.AuctionEnded && x.ProductTypeId == (int)ProductType.Auction).Equal(false)
                .WithMessage(translationService.GetResource("Admin.Catalog.Products.Cannoteditauction"));

        RuleFor(x => x.ProductTypeId == (int)ProductType.Auction && !x.AvailableEndDateTime.HasValue)
            .Equal(false)
            .WithMessage(
                translationService.GetResource("Vendor.Catalog.Products.Fields.AvailableEndDateTime.Required"));
    }
}