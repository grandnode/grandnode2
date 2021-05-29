using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ProcessOrderPaidCommandHandler : IRequestHandler<ProcessOrderPaidCommand, bool>
    {
        private readonly IMessageProviderService _messageProviderService;
        private readonly ICustomerService _customerService;
        private readonly IMediator _mediator;
        private readonly IPdfService _pdfService;
        private readonly OrderSettings _orderSettings;
        private readonly LanguageSettings _languageSettings;

        public ProcessOrderPaidCommandHandler(
            IMessageProviderService messageProviderService,
            ICustomerService customerService,
            IMediator mediator,
            IPdfService pdfService,
            OrderSettings orderSettings,
            LanguageSettings languageSettings)
        {
            _messageProviderService = messageProviderService;
            _customerService = customerService;
            _mediator = mediator;
            _pdfService = pdfService;
            _orderSettings = orderSettings;
            _languageSettings = languageSettings;
        }

        public async Task<bool> Handle(ProcessOrderPaidCommand request, CancellationToken cancellationToken)
        {
            await ProcessOrderPaid(request.Order);
            return true;
        }

        /// <summary>
        /// Process order paid status
        /// </summary>
        /// <param name="order">Order</param>
        protected virtual async Task ProcessOrderPaid(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //raise event
            await _mediator.Publish(new OrderPaidEvent(order));

            //order paid email notification
            if (order.OrderTotal != 0)
            {
                var customer = await _customerService.GetCustomerById(order.CustomerId);

                var orderPaidAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderPaidEmail && !_orderSettings.AttachPdfInvoiceToBinary ?
                    await _pdfService.PrintOrderToPdf(order, "")
                    : null;
                var orderPaidAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderPaidEmail && !_orderSettings.AttachPdfInvoiceToBinary ?
                    "order.pdf" : null;

                var orderPaidAttachments = _orderSettings.AttachPdfInvoiceToOrderPaidEmail && _orderSettings.AttachPdfInvoiceToBinary ?
                    new List<string> { await _pdfService.SaveOrderToBinary(order, "") } : new List<string>();

                await _messageProviderService.SendOrderPaidCustomerMessage(order, customer, order.CustomerLanguageId,
                    orderPaidAttachmentFilePath, orderPaidAttachmentFileName, orderPaidAttachments);

                await _messageProviderService.SendOrderPaidStoreOwnerMessage(order, customer, _languageSettings.DefaultAdminLanguageId);
                if (order.OrderItems.Any(x => !string.IsNullOrEmpty(x.VendorId)))
                {
                    var vendors = await _mediator.Send(new GetVendorsInOrderQuery() { Order = order });
                    foreach (var vendor in vendors)
                    {
                        await _messageProviderService.SendOrderPaidVendorMessage(order, vendor, _languageSettings.DefaultAdminLanguageId);
                    }
                }
                //TODO add "order paid email sent" order note
            }
        }
    }
}
