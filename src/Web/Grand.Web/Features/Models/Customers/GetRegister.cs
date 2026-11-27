using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetRegister : IRequest<RegisterModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Language Language { get; set; }
    public RegisterModel? Model { get; set; }
    public bool ExcludeProperties { get; set; }
    public IList<CustomAttribute>? OverrideCustomCustomerAttributes { get; set; }
}