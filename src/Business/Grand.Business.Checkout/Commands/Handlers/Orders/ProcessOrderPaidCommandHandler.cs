using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class ProcessOrderPaidCommandHandler : IRequestHandler<ProcessOrderPaidCommand, bool>
{
    private readonly ICustomerService _customerService;
    private readonly LanguageSettings _languageSettings;
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
        LanguageSettings languageSettings)
    {
        _orderMessageProviderService = orderMessageProviderService;
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

            var invoiceDownloadId = _orderSettings.AttachPdfInvoiceToOrderPaidEmail
                ? await _pdfService.SaveOrderToBinary(order, "")
                : null;

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