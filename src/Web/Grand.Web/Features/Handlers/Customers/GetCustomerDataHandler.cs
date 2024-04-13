using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.Customers;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers;

public class GetCustomerDataHandler : IRequestHandler<GetCustomerData, byte[]>
{
    private readonly ISchemaProperty<Address> _addresSchemaProperty;
    private readonly ISchemaProperty<Customer> _customerSchemaProperty;
    private readonly IExportProvider _exportProvider;
    private readonly ISchemaProperty<Order> _orderSchemaProperty;
    private readonly IOrderService _orderService;

    public GetCustomerDataHandler(
        IExportProvider exportProvider,
        ISchemaProperty<Customer> customerSchemaProperty,
        ISchemaProperty<Address> addresSchemaProperty,
        ISchemaProperty<Order> orderSchemaProperty,
        IOrderService orderService)
    {
        _exportProvider = exportProvider;
        _customerSchemaProperty = customerSchemaProperty;
        _addresSchemaProperty = addresSchemaProperty;
        _orderSchemaProperty = orderSchemaProperty;
        _orderService = orderService;
    }

    public async Task<byte[]> Handle(GetCustomerData request, CancellationToken cancellationToken)
    {
        var orders = await _orderService.SearchOrders(
            customerId: request.Customer.Id);

        _exportProvider
            .BuilderExportToByte(await _customerSchemaProperty.GetProperties(), new List<Customer> { request.Customer })
            .BuilderExportToByte(await _addresSchemaProperty.GetProperties(), request.Customer.Addresses)
            .BuilderExportToByte(await _orderSchemaProperty.GetProperties(), orders);

        return _exportProvider.BuilderExportToByte();
    }
}