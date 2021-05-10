using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Orders
{
    public class GetMerchandiseReturnDetailsHandler : IRequestHandler<GetMerchandiseReturnDetails, MerchandiseReturnDetailsModel>
    {
        private readonly IProductService _productService;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly IDateTimeService _dateTimeService;
        private readonly OrderSettings _orderSettings;

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

        public async Task<MerchandiseReturnDetailsModel> Handle(GetMerchandiseReturnDetails request, CancellationToken cancellationToken)
        {
            var model = new MerchandiseReturnDetailsModel();
            model.Comments = request.MerchandiseReturn.CustomerComments;
            model.ReturnNumber = request.MerchandiseReturn.ReturnNumber;
            model.ExternalId = request.MerchandiseReturn.ExternalId;
            model.MerchandiseReturnStatus = request.MerchandiseReturn.MerchandiseReturnStatus;
            model.CreatedOnUtc = request.MerchandiseReturn.CreatedOnUtc;
            model.ShowPickupAddress = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress;
            model.ShowPickupDate = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate;
            model.PickupDate = request.MerchandiseReturn.PickupDate;
            model.UserFields = request.MerchandiseReturn.UserFields;
            model.PickupAddress = await _mediator.Send(new GetAddressModel()
            {
                Language = request.Language,
                Address = request.MerchandiseReturn.PickupAddress,
                ExcludeProperties = false,
            });

            foreach (var item in request.MerchandiseReturn.MerchandiseReturnItems)
            {
                var orderItem = request.Order.OrderItems.Where(x => x.Id == item.OrderItemId).FirstOrDefault();
                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);

                string unitPrice = string.Empty;
                if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    unitPrice = _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax);
                }
                else
                {
                    //excluding tax
                    unitPrice = _priceFormatter.FormatPrice(orderItem.UnitPriceExclTax);
                }

                model.MerchandiseReturnItems.Add(new MerchandiseReturnDetailsModel.MerchandiseReturnItemModel
                {
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

        private async Task PrepareMerchandiseReturnNotes(GetMerchandiseReturnDetails request, MerchandiseReturnDetailsModel model)
        {
            foreach (var merchandiseReturnNote in (await _merchandiseReturnService.GetMerchandiseReturnNotes(request.MerchandiseReturn.Id))
                    .Where(rrn => rrn.DisplayToCustomer)
                    .OrderByDescending(rrn => rrn.CreatedOnUtc)
                    .ToList())
            {
                model.MerchandiseReturnNotes.Add(new MerchandiseReturnDetailsModel.MerchandiseReturnNote
                {
                    Id = merchandiseReturnNote.Id,
                    MerchandiseReturnId = merchandiseReturnNote.MerchandiseReturnId,
                    HasDownload = !String.IsNullOrEmpty(merchandiseReturnNote.DownloadId),
                    Note =  merchandiseReturnNote.Note,
                    CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturnNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
        }
    }
}
