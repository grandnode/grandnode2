using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog
{
    public class RelatedProductModelValidator : BaseGrandValidator<ProductModel.RelatedProductModel>
    {
        public RelatedProductModelValidator(
            IEnumerable<IValidatorConsumer<ProductModel.RelatedProductModel>> validators,
            ITranslationService translationService, IProductService productService, IWorkContext workContext)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var product = await productService.GetProductById(x.ProductId1);
                if (product == null) return true;
                return product.VendorId == workContext.CurrentVendor.Id;
            }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permisions"));
            
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var product = await productService.GetProductById(x.ProductId2);
                if (product == null) return true;
                return product.VendorId == workContext.CurrentVendor.Id;
            }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permisions"));
        }
    }
}