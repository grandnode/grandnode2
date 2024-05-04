using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class AddBundleProductModelValidator : BaseGrandValidator<ProductModel.AddBundleProductModel>
{
    public AddBundleProductModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.AddBundleProductModel>> validators,
        ITranslationService translationService, IProductService productService, IWorkContext workContext)
        : base(validators)
    {
        if (!string.IsNullOrEmpty(workContext.CurrentCustomer.StaffStoreId))
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var product = await productService.GetProductById(x.ProductId);
                if (product != null)
                    if (!product.AccessToEntityByStore(workContext.CurrentCustomer.StaffStoreId))
                        return false;

                return true;
            }).WithMessage(translationService.GetResource("Admin.Catalog.Products.Permissions"));
    }
}