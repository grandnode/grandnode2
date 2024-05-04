using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog;

public class BundleProductModelValidator : BaseGrandValidator<ProductModel.BundleProductModel>
{
    public BundleProductModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.BundleProductModel>> validators,
        ITranslationService translationService, IProductService productService, IWorkContext workContext)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var product = await productService.GetProductById(x.ProductBundleId);
            if (product == null) return true;
            return product.VendorId == workContext.CurrentVendor.Id;
        }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permissions"));
    }
}