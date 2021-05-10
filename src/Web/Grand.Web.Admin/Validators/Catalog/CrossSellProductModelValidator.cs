using FluentValidation;
using Grand.Infrastructure;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;
using Grand.Business.Catalog.Interfaces.Products;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class CrossSellProductModelValidator : BaseGrandValidator<ProductModel.CrossSellProductModel>
    {
        public CrossSellProductModelValidator(
            IEnumerable<IValidatorConsumer<ProductModel.CrossSellProductModel>> validators,
            ITranslationService translationService, IProductService productService, IWorkContext workContext)
            : base(validators)
        {
            if (!string.IsNullOrEmpty(workContext.CurrentCustomer.StaffStoreId))
            {
                RuleFor(x => x).MustAsync(async (x, y, context) =>
                {
                    var product = await productService.GetProductById(x.ProductId);
                    if (product != null)
                        if (!product.AccessToEntityByStore(workContext.CurrentCustomer.StaffStoreId))
                            return false;

                    return true;
                }).WithMessage(translationService.GetResource("Admin.Catalog.Products.Permisions"));
            }
            else if (workContext.CurrentVendor != null)
            {
                RuleFor(x => x).MustAsync(async (x, y, context) =>
                {
                    var product = await productService.GetProductById(x.ProductId);
                    if (product != null)
                        if (product != null && product.VendorId != workContext.CurrentVendor.Id)
                            return false;

                    return true;
                }).WithMessage(translationService.GetResource("Admin.Catalog.Products.Permisions"));
            }
        }
    }
}