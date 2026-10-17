using FluentValidation;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Catalog;

public class ProductValidator : BaseGrandValidator<ProductDto>
{
    public ProductValidator(
        IEnumerable<IValidatorConsumer<ProductDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor, IProductService productService,
        IProductLayoutService productLayoutService,
        IBrandService brandService,
        IVendorService vendorService, CommonSettings commonSettings)
        : base(validators)
    {
        var languageId = contextAccessor.WorkContext.WorkingLanguage.Id;

        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.Name.Required", languageId));
        //RuleFor(x => x.ProductType).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.ProductType.Required", languageId));
        //RuleFor(x => x.BackorderMode).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.BackorderMode.Required", languageId));
        //RuleFor(x => x.DownloadActivationType).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.DownloadActivationType.Required", languageId));
        //RuleFor(x => x.IntervalUnitType).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.IntervalUnitType.Required", languageId));
        //RuleFor(x => x.GiftCardType).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.GiftCardType.Required", languageId));
        //RuleFor(x => x.LowStockActivity).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.LowStockActivity.Required", languageId));
        //RuleFor(x => x.ManageInventoryMethod).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.ManageInventoryMethod.Required", languageId));
        //RuleFor(x => x.RecurringCyclePeriod).IsInEnum().WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.RecurringCyclePeriod.Required", languageId));

        if (!commonSettings.AllowEditProductEndedAuction)
            RuleFor(x => x.AuctionEnded && x.ProductTypeId == ProductType.Auction).Equal(false)
                .WithMessage(translationService.GetResource("Api.Catalog.Products.Cannoteditauction", languageId));

        RuleFor(x => x.ProductTypeId == ProductType.Auction && !x.AvailableEndDateTimeUtc.HasValue).Equal(false)
            .WithMessage(translationService.GetResource("Api.Catalog.Products.Fields.AvailableEndDateTime.Required", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.ParentGroupedProductId))
            {
                var product = await productService.GetProductById(x.ParentGroupedProductId);
                if (product == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.ParentGroupedProductId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.ProductLayoutId))
            {
                var layout = await productLayoutService.GetProductLayoutById(x.ProductLayoutId);
                if (layout == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.ProductLayoutId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.VendorId))
            {
                var vendor = await vendorService.GetVendorById(x.VendorId);
                if (vendor == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.VendorId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.BrandId))
            {
                var brand = await brandService.GetBrandById(x.BrandId);
                if (brand == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.BrandId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var product = await productService.GetProductById(x.Id);
                if (product == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Product.Fields.Id.NotExists", languageId));
    }
}