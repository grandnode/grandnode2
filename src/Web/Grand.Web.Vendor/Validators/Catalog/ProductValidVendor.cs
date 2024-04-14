using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog;

public class ProductValidVendor : BaseGrandValidator<IProductValidVendor>
{
    public ProductValidVendor(
        IEnumerable<IValidatorConsumer<IProductValidVendor>> validators,
        ITranslationService translationService, IProductService productService, IWorkContext workContext)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var product = await productService.GetProductById(x.ProductId);
            if (product == null) return true;
            return product.VendorId == workContext.CurrentVendor.Id;
        }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permissions"));
    }
}

public class ProductRelatedValidVendor : BaseGrandValidator<IProductRelatedValidVendor>
{
    public ProductRelatedValidVendor(
        IEnumerable<IValidatorConsumer<IProductRelatedValidVendor>> validators,
        ITranslationService translationService, IProductService productService, IWorkContext workContext)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var product1 = await productService.GetProductById(x.ProductId1);
            if (product1 == null) return true;
            var product2 = await productService.GetProductById(x.ProductId2);
            if (product2 == null) return true;
            return product1.VendorId == workContext.CurrentVendor.Id ||
                   product2.VendorId == workContext.CurrentVendor.Id;
        }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permissions"));
    }
}