using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Catalog;

public class GetVendor : IRequest<VendorModel>
{
    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public Language Language { get; set; }
    public Domain.Vendors.Vendor Vendor { get; set; }
    public CatalogPagingFilteringModel Command { get; set; }
}