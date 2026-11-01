using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class ProcessOrderPaidCommandHandler : IRequestHandler<ProcessOrderPaidCommand, bool>
{
    private readonly ICustomerService _customerService;
    private readonly LanguageSettings _languageSettings;
    private readonly ILogger<ProcessOrderPaidCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IOrderMessageProviderService _orderMessageProviderService;
    private readonly OrderSettings _orderSettings;
    private readonly IPdfService _pdfService;

    public ProcessOrderPaidCommandHandler(
        IOrderMessageProviderService orderMessageProviderService,
        ICustomerService customerService,
        IMediator mediator,
        IPdfService pdfService,
        OrderSettings orderSettings,
        LanguageSettings languageSettings,
        ILogger<ProcessOrderPaidCommandHandler> logger)
    {
        _orderMessageProviderService = orderMessageProviderService;
        _customerService = customerService;
        _mediator = mediator;
        _pdfService = pdfService;
        _orderSettings = orderSettings;
        _languageSettings = languageSettings;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessOrderPaidCommand request, CancellationToken cancellationToken)
    {
        await ProcessOrderPaid(request.Order);
        return true;
    }

    /// <summary>
    ///     Process order paid status
    /// </summary>
    /// <param name="order">Order</param>
    protected virtual async Task ProcessOrderPaid(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        //an order waiting for an approval is processed as paid once it is approved, not before
        if (order.OrderStatusId == (int)OrderStatusSystem.AwaitingApproval)
            return;

        //raise event
        await _mediator.Publish(new OrderPaidEvent(order));

        //order paid email notification
        if (order.OrderTotal != 0)
        {
            var customer = await _customerService.GetCustomerById(order.CustomerId);

            string invoiceDownloadId = null;

            try
            {
                if (_orderSettings.AttachPdfInvoiceToOrderPaidEmail)
                    invoiceDownloadId = await _pdfService.SaveOrderToBinary(order, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error - order paid attachment file {OrderOrderNumber}", order.OrderNumber);
            }

            await _orderMessageProviderService.SendOrderPaidCustomerMessage(order, customer, order.CustomerLanguageId,
                invoiceDownloadId);

            await _orderMessageProviderService.SendOrderPaidStoreOwnerMessage(order, customer,
                _languageSettings.DefaultAdminLanguageId);
            if (order.OrderItems.Any(x => !string.IsNullOrEmpty(x.VendorId)))
            {
                var vendors = await _mediator.Send(new GetVendorsInOrderQuery { Order = order });
                foreach (var vendor in vendors)
                    await _orderMessageProviderService.SendOrderPaidVendorMessage(order, vendor,
                        _languageSettings.DefaultAdminLanguageId);
            }
        }
    }
}