using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class OrderNotificationCommandHandler : IRequestHandler<OrderNotificationCommand>
{
    private readonly IOrderService _orderService;
    private readonly IOrderMessageProviderService _orderMessageProviderService;
    private readonly OrderSettings _orderSettings;
    private readonly LanguageSettings _languageSettings;
    private readonly IPdfService _pdfService;
    private readonly ILogger<OrderNotificationCommandHandler> _logger;
    private readonly IMediator _mediator;

    public OrderNotificationCommandHandler(
        IOrderService orderService,
        IOrderMessageProviderService orderMessageProviderService,
        OrderSettings orderSettings,
        LanguageSettings languageSettings,
        IPdfService pdfService,
        ILogger<OrderNotificationCommandHandler> logger,
        IMediator mediator)
    {
        _orderService = orderService;
        _orderMessageProviderService = orderMessageProviderService;
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
            if (request.OriginalCustomerIfImpersonated != null)
                //this order is placed by a store administrator impersonating a customer
                await _orderService.InsertOrderNote(new OrderNote {
                    Note =
                        $"Order placed by a store owner ('{request.OriginalCustomerIfImpersonated.Email}'. ID = {request.OriginalCustomerIfImpersonated.Id}) impersonating the customer.",
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
            if (request.NotifyStoreOwnerAndVendors)
                await _orderMessageProviderService.SendOrderPlacedStoreOwnerMessage(request.Order, request.Customer,
                    _languageSettings.DefaultAdminLanguageId, request.Host);

            if (request.NotifyCustomer)
            {
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

                await _orderMessageProviderService.SendOrderPlacedCustomerMessage(request.Order, request.Customer,
                    request.Order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName,
                    orderPlacedAttachments, request.Host);
            }

            if (request.NotifyStoreOwnerAndVendors && request.Order.OrderItems.Any(x => !string.IsNullOrEmpty(x.VendorId)))
            {
                var vendors = await _mediator.Send(new GetVendorsInOrderQuery { Order = request.Order });
                foreach (var vendor in vendors)
                    await _orderMessageProviderService.SendOrderPlacedVendorMessage(request.Order, request.Customer, vendor,
                        _languageSettings.DefaultAdminLanguageId, request.Host);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Place order send notification error");
        }
    }
}
