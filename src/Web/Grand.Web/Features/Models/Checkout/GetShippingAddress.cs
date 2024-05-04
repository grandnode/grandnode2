using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Models.Checkout;

public class GetShippingAddress : IRequest<CheckoutShippingAddressModel>
{
    public string SelectedCountryId { get; set; }
    public bool PrePopulateNewAddressWithCustomerFields { get; set; }
    public IList<CustomAttribute> OverrideAttributes { get; set; }
    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public Currency Currency { get; set; }
    public Language Language { get; set; }
}