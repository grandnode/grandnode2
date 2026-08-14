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
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var product = await productService.GetProductById(x.ProductId);
            if (product == null) return true;
            return product.VendorId == contextAccessor.WorkContext.CurrentVendor.Id;
        }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permissions"));
    }
}

public class ProductRelatedValidVendor : BaseGrandValidator<IProductRelatedValidVendor>
{
    public ProductRelatedValidVendor(
        IEnumerable<IValidatorConsumer<IProductRelatedValidVendor>> validators,
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            //RelatedProductModel/SimilarProductModel actions only ever read and mutate ProductId1's
            //mapping list, so ownership of ProductId1 is what must be enforced here. Accepting ownership
            //of ProductId2 as an alternative (the previous "||") let a vendor who owns any product supply
            //it as ProductId2 and edit/delete another vendor's ProductId1 mapping.
            var product1 = await productService.GetProductById(x.ProductId1);
            if (product1 == null) return true;
            return product1.VendorId == contextAccessor.WorkContext.CurrentVendor.Id;
        }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permissions"));
    }
}