using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductBidViewModelService(
    IDateTimeService dateTimeService,
    IContextAccessor contextAccessor,
    ICustomerService customerService,
    IAuctionService auctionService,
    IPriceFormatter priceFormatter)
    : IProductBidViewModelService
{
    public virtual async Task<(IEnumerable<ProductModel.BidModel> bidModels, int totalCount)> PrepareBidMode(
        string productId, int pageIndex, int pageSize)
    {
        var bids = await auctionService.GetBidsByProductId(productId, pageIndex - 1, pageSize);
        var bidsModel = new List<ProductModel.BidModel>();
        foreach (var x in bids)
            bidsModel.Add(new ProductModel.BidModel {
                BidId = x.Id,
                ProductId = x.ProductId,
                Amount = priceFormatter.FormatPrice(x.Amount, contextAccessor.WorkContext.WorkingCurrency),
                Date = dateTimeService.ConvertToUserTime(x.Date, DateTimeKind.Utc),
                CustomerId = x.CustomerId,
                Email = (await customerService.GetCustomerById(x.CustomerId))?.Email,
                OrderId = x.OrderId
            });
        return (bidsModel, bids.TotalCount);
    }
}
