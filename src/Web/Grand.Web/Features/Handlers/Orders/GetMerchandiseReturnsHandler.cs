using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Orders
{
    public class GetMerchandiseReturnsHandler : IRequestHandler<GetMerchandiseReturns, CustomerMerchandiseReturnsModel>
    {
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IGroupService _groupService;
        private readonly IMediator _mediator;

        public GetMerchandiseReturnsHandler(
            IOrderService orderService,
            ITranslationService translationService,
            IDateTimeService dateTimeService,
            IPriceFormatter priceFormatter,
            IGroupService groupService,
            IMediator mediator)
        {
            _orderService = orderService;
            _groupService = groupService;
            _translationService = translationService;
            _dateTimeService = dateTimeService;
            _priceFormatter = priceFormatter;
            _mediator = mediator;
        }

        public async Task<CustomerMerchandiseReturnsModel> Handle(GetMerchandiseReturns request, CancellationToken cancellationToken)
        {
            var model = new CustomerMerchandiseReturnsModel();

            var query = new GetMerchandiseReturnQuery()
            {
                StoreId = request.Store.Id,
            };

            if (await _groupService.IsOwner(request.Customer))
                query.OwnerId = request.Customer.Id;
            else
                query.CustomerId = request.Customer.Id;

            var merchandiseReturns = await _mediator.Send(query);

            foreach (var merchandiseReturn in merchandiseReturns)
            {
                var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
                double total = 0;
                foreach (var rrItem in merchandiseReturn.MerchandiseReturnItems)
                {
                    var orderItem = order.OrderItems.Where(x => x.Id == rrItem.OrderItemId).First();

                    if (order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                    {
                        //including tax
                        total += orderItem.UnitPriceInclTax * rrItem.Quantity;
                    }
                    else
                    {
                        //excluding tax
                        total += orderItem.UnitPriceExclTax * rrItem.Quantity;
                    }
                }

                var itemModel = new CustomerMerchandiseReturnsModel.MerchandiseReturnModel
                {
                    Id = merchandiseReturn.Id,
                    ReturnNumber = merchandiseReturn.ReturnNumber,
                    MerchandiseReturnStatus = merchandiseReturn.MerchandiseReturnStatus.GetTranslationEnum(_translationService, request.Language.Id),
                    CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturn.CreatedOnUtc, DateTimeKind.Utc),
                    ProductsCount = merchandiseReturn.MerchandiseReturnItems.Sum(x => x.Quantity),
                    ReturnTotal = _priceFormatter.FormatPrice(total)
                };

                model.Items.Add(itemModel);
            }

            return model;
        }
    }
}
