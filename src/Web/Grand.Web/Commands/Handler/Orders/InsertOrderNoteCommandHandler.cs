using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Orders;

public class InsertOrderNoteCommandHandler : IRequestHandler<InsertOrderNoteCommand, OrderNote>
{
    private readonly IOrderMessageProviderService _orderMessageProviderService;
    private readonly IOrderService _orderService;

    public InsertOrderNoteCommandHandler(IOrderMessageProviderService orderMessageProviderService, IOrderService orderService)
    {
        _orderMessageProviderService = orderMessageProviderService;
        _orderService = orderService;
    }

    public async Task<OrderNote> Handle(InsertOrderNoteCommand request, CancellationToken cancellationToken)
    {
        var orderNote = new OrderNote {
            DisplayToCustomer = true,
            Note = request.OrderNote.Note,
            OrderId = request.OrderNote.OrderId,
            CreatedByCustomer = true
        };
        await _orderService.InsertOrderNote(orderNote);

        //email
        await _orderMessageProviderService.SendNewOrderNoteAddedCustomerMessage(request.Order, orderNote,
            host: request.Host);

        return orderNote;
    }
}