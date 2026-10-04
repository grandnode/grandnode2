using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Messages.Common;

public class OrderNotificationCommand : IRequest
{
    public Order Order { get; set; }
    public IWorkContext WorkContext { get; set; }
}
