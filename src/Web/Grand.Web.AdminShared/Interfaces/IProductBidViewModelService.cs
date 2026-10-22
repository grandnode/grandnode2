using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductBidViewModelService
{
    Task<(IEnumerable<ProductModel.BidModel> bidModels, int totalCount)> PrepareBidMode(string productId, int pageIndex,
        int pageSize);
}
