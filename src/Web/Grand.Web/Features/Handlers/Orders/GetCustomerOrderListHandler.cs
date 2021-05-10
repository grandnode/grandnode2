using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Orders
{
    public class GetCustomerOrderListHandler : IRequestHandler<GetCustomerOrderList, CustomerOrderListModel>
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly ITranslationService _translationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IGroupService _groupService;
        private readonly IMediator _mediator;
        private readonly IOrderStatusService _orderStatusService;
        private readonly IOrderService _orderService;
        private readonly OrderSettings _orderSettings;

        public GetCustomerOrderListHandler(
            IDateTimeService dateTimeService,
            ITranslationService translationService,
            IGroupService groupService,
            IMediator mediator,
            IPriceFormatter priceFormatter,
            IOrderStatusService orderStatusService,
            IOrderService orderService,
            OrderSettings orderSettings)
        {
            _dateTimeService = dateTimeService;
            _translationService = translationService;
            _groupService = groupService;
            _priceFormatter = priceFormatter;
            _orderStatusService = orderStatusService;
            _orderService = orderService;
            _orderSettings = orderSettings;
            _mediator = mediator;
        }

        public async Task<CustomerOrderListModel> Handle(GetCustomerOrderList request, CancellationToken cancellationToken)
        {
            var model = new CustomerOrderListModel();
            await PrepareOrder(model, request);
            return model;
        }

        private async Task PrepareOrder(CustomerOrderListModel model, GetCustomerOrderList request)
        {
            if (request.Command.PageSize <= 0) request.Command.PageSize = _orderSettings.PageSize;
            if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;
            if (request.Command.PageSize == 0)
                request.Command.PageSize = 10;

            var customerId = string.Empty;
            var ownerId = string.Empty;

            if (!await _groupService.IsOwner(request.Customer))
                customerId = request.Customer.Id;
            else
                ownerId = request.Customer.Id;

            var orders = await _orderService.SearchOrders(
                customerId: customerId,
                ownerId : ownerId,
                storeId: request.Store.Id,
                pageIndex: request.Command.PageNumber - 1,
                pageSize: request.Command.PageSize);

            model.PagingContext.LoadPagedList(orders);

            foreach (var order in orders)
            {
                var status = await _orderStatusService.GetByStatusId(order.OrderStatusId);
                var orderModel = new CustomerOrderListModel.OrderDetailsModel
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderCode = order.Code,
                    CustomerEmail = order.BillingAddress?.Email,
                    CreatedOn = _dateTimeService.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
                    OrderStatusId = order.OrderStatusId,
                    OrderStatus = status?.Name,
                    PaymentStatus = order.PaymentStatusId.GetTranslationEnum(_translationService, request.Language.Id),
                    ShippingStatus = order.ShippingStatusId.GetTranslationEnum(_translationService, request.Language.Id),
                    IsMerchandiseReturnAllowed = await _mediator.Send(new IsMerchandiseReturnAllowedQuery() { Order = order })
                };
                orderModel.OrderTotal = await _priceFormatter.FormatPrice(order.OrderTotal, order.CustomerCurrencyCode, false, request.Language);

                model.Orders.Add(orderModel);
            }
        }


    }
}
