using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Web.Models.Vendors;
using MediatR;

namespace Grand.Web.Features.Models.Common;

public class GetVendorAddress : IRequest<VendorAddressModel>
{
    public VendorAddressModel Model { get; set; }
    public Address Address { get; set; }
    public bool ExcludeProperties { get; set; }
    public Func<IList<Country>> LoadCountries { get; set; }
    public bool PrePopulateWithCustomerFields { get; set; }
    public Customer Customer { get; set; }
    public Language Language { get; set; }
}