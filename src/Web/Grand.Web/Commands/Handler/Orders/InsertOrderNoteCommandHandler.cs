using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Orders;
using Grand.Web.Commands.Models.Orders;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Orders
{
    public class InsertOrderNoteCommandHandler : IRequestHandler<InsertOrderNoteCommand, OrderNote>
    {
        private readonly IMessageProviderService _messageProviderService;
        private readonly IOrderService _orderService;

        public InsertOrderNoteCommandHandler(IMessageProviderService messageProviderService, IOrderService orderService)
        {
            _messageProviderService = messageProviderService;
            _orderService = orderService;
        }

        public async Task<OrderNote> Handle(InsertOrderNoteCommand request, CancellationToken cancellationToken)
        {
            var orderNote = new OrderNote
            {
                CreatedOnUtc = DateTime.UtcNow,
                DisplayToCustomer = true,
                Note = request.OrderNote.Note,
                OrderId = request.OrderNote.OrderId,
                CreatedByCustomer = true
            };
            await _orderService.InsertOrderNote(orderNote);

            //email
            await _messageProviderService.SendNewOrderNoteAddedCustomerMessage(request.Order, orderNote);

            return orderNote;
        }
    }
}
