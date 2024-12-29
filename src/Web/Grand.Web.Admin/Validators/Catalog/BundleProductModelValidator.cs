using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class BundleProductModelValidator : BaseGrandValidator<ProductModel.BundleProductModel>
{
    public BundleProductModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.BundleProductModel>> validators,
        ITranslationService translationService, IProductService productService, IWorkContextAccessor workContextAccessor)
        : base(validators)
    {
        if (!string.IsNullOrEmpty(workContextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var product = await productService.GetProductById(x.ProductBundleId);
                if (product != null)
                    if (!product.AccessToEntityByStore(workContextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                        return false;

                return true;
            }).WithMessage(translationService.GetResource("Admin.Catalog.Products.Permissions"));
    }
}