﻿using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Vendors;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Discounts;

namespace Grand.Web.Admin.Interfaces
{
    public interface IDiscountViewModelService
    {
        DiscountListModel PrepareDiscountListModel();
        Task<(IEnumerable<DiscountModel> discountModel, int totalCount)> PrepareDiscountModel(DiscountListModel model, int pageIndex, int pageSize);
        Task PrepareDiscountModel(DiscountModel model, Discount discount);
        Task<Discount> InsertDiscountModel(DiscountModel model);
        Task<Discount> UpdateDiscountModel(Discount discount, DiscountModel model);
        Task DeleteDiscount(Discount discount);
        Task InsertCouponCode(string discountId, string couponCode);
        string GetRequirementUrlInternal(IDiscountRule discountRequirementRule, Discount discount, string discountRequirementId);
        Task DeleteDiscountRequirement(DiscountRule discountRequirement, Discount discount);
        Task<DiscountModel.AddProductToDiscountModel> PrepareProductToDiscountModel();
        Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(DiscountModel.AddProductToDiscountModel model, int pageIndex, int pageSize);
        Task InsertProductToDiscountModel(DiscountModel.AddProductToDiscountModel model);
        Task DeleteProduct(Discount discount, Product product);
        Task DeleteCategory(Discount discount, Category category);
        Task DeleteBrand(Discount discount, Brand brand);
        Task DeleteVendor(Discount discount, Vendor vendor);
        Task DeleteCollection(Discount discount, Collection collection);
        Task InsertCategoryToDiscountModel(DiscountModel.AddCategoryToDiscountModel model);
        Task InsertBrandToDiscountModel(DiscountModel.AddBrandToDiscountModel model);
        Task InsertCollectionToDiscountModel(DiscountModel.AddCollectionToDiscountModel model);
        Task InsertVendorToDiscountModel(DiscountModel.AddVendorToDiscountModel model);
        Task<(IEnumerable<DiscountModel.DiscountUsageHistoryModel> usageHistoryModels, int totalCount)> PrepareDiscountUsageHistoryModel(Discount discount, int pageIndex, int pageSize);

    }
}
