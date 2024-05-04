using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Handlers.Orders;

public class
    GetMerchandiseReturnDetailsHandler : IRequestHandler<GetMerchandiseReturnDetails, MerchandiseReturnDetailsModel>
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IMediator _mediator;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly OrderSettings _orderSettings;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;
    private readonly IWorkContext _workContext;

    public GetMerchandiseReturnDetailsHandler(
        IProductService productService,
        IMerchandiseReturnService merchandiseReturnService,
        IPriceFormatter priceFormatter,
        IWorkContext workContext,
        IMediator mediator,
        IDateTimeService dateTimeService,
        OrderSettings orderSettings)
    {
        _productService = productService;
        _merchandiseReturnService = merchandiseReturnService;
        _priceFormatter = priceFormatter;
        _workContext = workContext;
        _mediator = mediator;
        _dateTimeService = dateTimeService;
        _orderSettings = orderSettings;
    }

    public async Task<MerchandiseReturnDetailsModel> Handle(GetMerchandiseReturnDetails request,
        CancellationToken cancellationToken)
    {
        var model = new MerchandiseReturnDetailsModel {
            Comments = request.MerchandiseReturn.CustomerComments,
            ReturnNumber = request.MerchandiseReturn.ReturnNumber,
            ExternalId = request.MerchandiseReturn.ExternalId,
            MerchandiseReturnStatus = request.MerchandiseReturn.MerchandiseReturnStatus,
            CreatedOnUtc = request.MerchandiseReturn.CreatedOnUtc,
            ShowPickupAddress = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress,
            ShowPickupDate = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate,
            PickupDate = request.MerchandiseReturn.PickupDate,
            UserFields = request.MerchandiseReturn.UserFields,
            PickupAddress = await _mediator.Send(new GetAddressModel {
                Language = request.Language,
                Address = request.MerchandiseReturn.PickupAddress,
                ExcludeProperties = false
            }, cancellationToken)
        };

        foreach (var item in request.MerchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = request.Order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            if (orderItem == null)
                continue;

            var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);

            var unitPrice = _priceFormatter.FormatPrice(
                request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax
                    ?
                    //including tax
                    orderItem.UnitPriceInclTax
                    :
                    //excluding tax
                    orderItem.UnitPriceExclTax);

            model.MerchandiseReturnItems.Add(new MerchandiseReturnDetailsModel.MerchandiseReturnItemModel {
                OrderItemId = item.OrderItemId,
                Quantity = item.Quantity,
                ReasonForReturn = item.ReasonForReturn,
                RequestedAction = item.RequestedAction,
                ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id),
                ProductPrice = unitPrice
            });
        }

        //merchandise return notes
        await PrepareMerchandiseReturnNotes(request, model);

        return model;
    }

    private async Task PrepareMerchandiseReturnNotes(GetMerchandiseReturnDetails request,
        MerchandiseReturnDetailsModel model)
    {
        foreach (var merchandiseReturnNote in (await _merchandiseReturnService.GetMerchandiseReturnNotes(
                     request.MerchandiseReturn.Id))
                 .Where(rrn => rrn.DisplayToCustomer)
                 .OrderByDescending(rrn => rrn.CreatedOnUtc)
                 .ToList())
            model.MerchandiseReturnNotes.Add(new MerchandiseReturnDetailsModel.MerchandiseReturnNote {
                Id = merchandiseReturnNote.Id,
                MerchandiseReturnId = merchandiseReturnNote.MerchandiseReturnId,
                HasDownload = !string.IsNullOrEmpty(merchandiseReturnNote.DownloadId),
                Note = merchandiseReturnNote.Note,
                CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturnNote.CreatedOnUtc, DateTimeKind.Utc)
            });
    }
}