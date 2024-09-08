using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Tax;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Handlers.Orders;

public class GetMerchandiseReturnsHandler : IRequestHandler<GetMerchandiseReturns, CustomerMerchandiseReturnsModel>
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IGroupService _groupService;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IEnumTranslationService _enumTranslationService;
    
    public GetMerchandiseReturnsHandler(
        IOrderService orderService,
        IDateTimeService dateTimeService,
        IPriceFormatter priceFormatter,
        IGroupService groupService,
        IMediator mediator, 
        IEnumTranslationService enumTranslationService)
    {
        _orderService = orderService;
        _groupService = groupService;
        _dateTimeService = dateTimeService;
        _priceFormatter = priceFormatter;
        _mediator = mediator;
        _enumTranslationService = enumTranslationService;
    }

    public async Task<CustomerMerchandiseReturnsModel> Handle(GetMerchandiseReturns request,
        CancellationToken cancellationToken)
    {
        var model = new CustomerMerchandiseReturnsModel();

        var query = new GetMerchandiseReturnQuery {
            StoreId = request.Store.Id
        };

        if (await _groupService.IsOwner(request.Customer))
            query.OwnerId = request.Customer.Id;
        else
            query.CustomerId = request.Customer.Id;

        var merchandiseReturns = await _mediator.Send(query, cancellationToken);

        foreach (var merchandiseReturn in merchandiseReturns)
        {
            var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
            double total = 0;
            foreach (var rrItem in merchandiseReturn.MerchandiseReturnItems)
            {
                var orderItem = order.OrderItems.First(x => x.Id == rrItem.OrderItemId);

                if (order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                    //including tax
                    total += orderItem.UnitPriceInclTax * rrItem.Quantity;
                else
                    //excluding tax
                    total += orderItem.UnitPriceExclTax * rrItem.Quantity;
            }

            var itemModel = new CustomerMerchandiseReturnsModel.MerchandiseReturnModel {
                Id = merchandiseReturn.Id,
                ReturnNumber = merchandiseReturn.ReturnNumber,
                MerchandiseReturnStatus = _enumTranslationService.GetTranslationEnum(merchandiseReturn.MerchandiseReturnStatus),
                CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturn.CreatedOnUtc, DateTimeKind.Utc),
                ProductsCount = merchandiseReturn.MerchandiseReturnItems.Sum(x => x.Quantity),
                ReturnTotal = _priceFormatter.FormatPrice(total)
            };

            model.Items.Add(itemModel);
        }

        return model;
    }
}