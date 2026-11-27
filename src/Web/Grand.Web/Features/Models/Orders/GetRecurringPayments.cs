using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Mediator;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Models.Orders;

public class GetRecurringPayments : IRequest<RecurringPaymentListModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Language Language { get; set; }
    public OrderPagingModel Command { get; set; } = new();
}
