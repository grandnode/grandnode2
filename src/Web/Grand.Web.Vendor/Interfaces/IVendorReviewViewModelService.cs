using Grand.Domain.Vendors;
using Grand.Web.Vendor.Models.VendorReview;

namespace Grand.Web.Vendor.Interfaces;

public interface IVendorReviewViewModelService
{
    Task PrepareVendorReviewModel(VendorReviewModel model,
        VendorReview vendorReview, bool excludeProperties, bool formatReviewText);

    Task<(IEnumerable<VendorReviewModel> vendorReviewModels, int totalCount)> PrepareVendorReviewModel(
        VendorReviewListModel model, int pageIndex, int pageSize);

    Task<VendorReview> UpdateVendorReviewModel(VendorReview vendorReview, VendorReviewModel model);
    Task DeleteVendorReview(VendorReview vendorReview);
    Task ApproveVendorReviews(IEnumerable<string> selectedIds);
    Task DisapproveVendorReviews(IEnumerable<string> selectedIds);
}