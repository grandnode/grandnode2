using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class OrderNotificationCommandHandler : IRequestHandler<OrderNotificationCommand>
{
    private readonly IOrderService _orderService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly OrderSettings _orderSettings;
    private readonly LanguageSettings _languageSettings;
    private readonly IPdfService _pdfService;
    private readonly ILogger<OrderNotificationCommandHandler> _logger;
    private readonly IMediator _mediator;

    public OrderNotificationCommandHandler(
        IOrderService orderService,
        IMessageProviderService messageProviderService,
        OrderSettings orderSettings,
        LanguageSettings languageSettings,
        IPdfService pdfService,
        ILogger<OrderNotificationCommandHandler> logger,
        IMediator mediator)
    {
        _orderService = orderService;
        _messageProviderService = messageProviderService;
        _orderSettings = orderSettings;
        _languageSettings = languageSettings;
        _pdfService = pdfService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(OrderNotificationCommand request, CancellationToken cancellationToken)
    {

        try
        {
            if (request.WorkContext.OriginalCustomerIfImpersonated != null)
                //this order is placed by a store administrator impersonating a customer
                await _orderService.InsertOrderNote(new OrderNote {
                    Note =
                        $"Order placed by a store owner ('{request.WorkContext.OriginalCustomerIfImpersonated.Email}'. ID = {request.WorkContext.OriginalCustomerIfImpersonated.Id}) impersonating the customer.",
                    DisplayToCustomer = false,
                    OrderId = request.Order.Id
                });
            else
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "Order placed",
                    DisplayToCustomer = false,
                    OrderId = request.Order.Id
                });

            //send email notifications
            await _messageProviderService.SendOrderPlacedStoreOwnerMessage(request.Order, request.WorkContext.CurrentCustomer,
                _languageSettings.DefaultAdminLanguageId);

            string orderPlacedAttachmentFilePath = string.Empty, orderPlacedAttachmentFileName = string.Empty;
            var orderPlacedAttachments = new List<string>();

            try
            {
                orderPlacedAttachmentFilePath =
                    _orderSettings.AttachPdfInvoiceToOrderPlacedEmail && !_orderSettings.AttachPdfInvoiceToBinary
                        ? await _pdfService.PrintOrderToPdf(request.Order, request.Order.CustomerLanguageId)
                        : null;
                orderPlacedAttachmentFileName =
                    _orderSettings.AttachPdfInvoiceToOrderPlacedEmail && !_orderSettings.AttachPdfInvoiceToBinary
                        ? "order.pdf"
                        : null;
                orderPlacedAttachments = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail &&
                                         _orderSettings.AttachPdfInvoiceToBinary
                    ? [
                        await _pdfService.SaveOrderToBinary(request.Order, request.Order.CustomerLanguageId)
                    ]
                    : [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error - order placed attachment file {OrderOrderNumber}", request.Order.OrderNumber);
            }

            await _messageProviderService.SendOrderPlacedCustomerMessage(request.Order, request.WorkContext.CurrentCustomer, request.Order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName, orderPlacedAttachments);

            if (request.Order.OrderItems.Any(x => !string.IsNullOrEmpty(x.VendorId)))
            {
                var vendors = await _mediator.Send(new GetVendorsInOrderQuery { Order = request.Order });
                foreach (var vendor in vendors)
                    await _messageProviderService.SendOrderPlacedVendorMessage(request.Order, request.WorkContext.CurrentCustomer, vendor,
                        _languageSettings.DefaultAdminLanguageId);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Place order send notification error");
        }
    }
}
