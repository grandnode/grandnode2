﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class AddRelatedProductModelValidator : BaseGrandValidator<ProductModel.AddRelatedProductModel>
    {
        public AddRelatedProductModelValidator(
            IEnumerable<IValidatorConsumer<ProductModel.AddRelatedProductModel>> validators,
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