﻿using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Interfaces
{
    public interface IProductReviewViewModelService
    {
        Task PrepareProductReviewModel(ProductReviewModel model,
            ProductReview productReview, bool excludeProperties, bool formatReviewText);
        Task<(IEnumerable<ProductReviewModel> productReviewModels, int totalCount)> PrepareProductReviewsModel(ProductReviewListModel model, int pageIndex, int pageSize);
        Task<ProductReview> UpdateProductReview(ProductReview productReview, ProductReviewModel model);
        Task<ProductReviewListModel> PrepareProductReviewListModel(string storeId);
        Task DeleteProductReview(ProductReview productReview);
        Task ApproveSelected(IList<string> selectedIds, string storeId);
        Task DisapproveSelected(IList<string> selectedIds, string storeId);

    }
}
