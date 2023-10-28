using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog
{
    public class ProductCollectionModelValidator : BaseGrandValidator<ProductModel.ProductCollectionModel>
    {
        public ProductCollectionModelValidator(
            IEnumerable<IValidatorConsumer<ProductModel.ProductCollectionModel>> validators,
            ITranslationService translationService, IProductService productService, IWorkContext workContext)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var product = await productService.GetProductById(x.ProductId);
                if (product != null)
                    if (product.VendorId != workContext.CurrentVendor.Id)
                        return false;

                return true;
            }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.Permisions"));
        }
    }
}