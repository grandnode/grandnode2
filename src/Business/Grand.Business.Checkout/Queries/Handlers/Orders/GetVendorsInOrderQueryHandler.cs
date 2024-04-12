using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetVendorsInOrderQueryHandler : IRequestHandler<GetVendorsInOrderQuery, IList<Vendor>>
{
    private readonly IVendorService _vendorService;

    public GetVendorsInOrderQueryHandler(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    public async Task<IList<Vendor>> Handle(GetVendorsInOrderQuery request, CancellationToken cancellationToken)
    {
        return await GetVendorsInOrder(request.Order);
    }

    protected virtual async Task<IList<Vendor>> GetVendorsInOrder(Order order)
    {
        var vendors = new List<Vendor>();
        foreach (var vendorKey in order.OrderItems.GroupBy(x => x.VendorId))
        {
            if (string.IsNullOrEmpty(vendorKey.Key)) continue;
            var vendor = await _vendorService.GetVendorById(vendorKey.Key);
            if (vendor is { Deleted: false, Active: true }) vendors.Add(vendor);
        }

        return vendors;
    }
}