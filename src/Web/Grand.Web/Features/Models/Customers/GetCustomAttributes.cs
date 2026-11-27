using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetCustomAttributes : IRequest<IList<CustomerAttributeModel>>
{
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
    public IList<CustomAttribute>? OverrideAttributes { get; set; }
}