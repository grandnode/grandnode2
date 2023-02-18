﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Handlers.Orders
{
    public class GetShipmentDetailsHandler : IRequestHandler<GetShipmentDetails, ShipmentDetailsModel>
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly IShippingService _shippingService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IMediator _mediator;

        private readonly ShippingSettings _shippingSettings;
        private readonly ShippingProviderSettings _shippingProviderSettings;
        private readonly CatalogSettings _catalogSettings;

        public GetShipmentDetailsHandler(
            IDateTimeService dateTimeService,
            IProductService productService,
            IShippingService shippingService,
            IShipmentService shipmentService,
            IMediator mediator,
            ShippingSettings shippingSettings,
            CatalogSettings catalogSettings,
            ShippingProviderSettings shippingProviderSettings)
        {
            _dateTimeService = dateTimeService;
            _productService = productService;
            _shippingService = shippingService;
            _shipmentService = shipmentService;
            _mediator = mediator;
            _shippingSettings = shippingSettings;
            _catalogSettings = catalogSettings;
            _shippingProviderSettings = shippingProviderSettings;
        }

        public async Task<ShipmentDetailsModel> Handle(GetShipmentDetails request, CancellationToken cancellationToken)
        {
            if (request.Shipment == null)
                throw new ArgumentNullException(nameof(request.Shipment));

            var model = new ShipmentDetailsModel {
                Id = request.Shipment.Id,
                ShipmentNumber = request.Shipment.ShipmentNumber
            };

            if (request.Shipment.ShippedDateUtc.HasValue)
                model.ShippedDate = _dateTimeService.ConvertToUserTime(request.Shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
            if (request.Shipment.DeliveryDateUtc.HasValue)
                model.DeliveryDate = _dateTimeService.ConvertToUserTime(request.Shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);

            //tracking number and shipment information
            if (!string.IsNullOrEmpty(request.Shipment.TrackingNumber))
            {
                model.TrackingNumber = request.Shipment.TrackingNumber;
                var srcm = _shippingService.LoadShippingRateCalculationProviderBySystemName(request.Order.ShippingRateProviderSystemName);
                if (srcm != null &&
                    srcm.IsShippingRateMethodActive(_shippingProviderSettings))
                {
                    var shipmentTracker = srcm.ShipmentTracker;
                    if (shipmentTracker != null)
                    {
                        model.TrackingNumberUrl = await shipmentTracker.GetUrl(request.Shipment.TrackingNumber);
                        if (_shippingSettings.DisplayShipmentEventsToCustomers)
                        {
                            var shipmentEvents = await shipmentTracker.GetShipmentEvents(request.Shipment.TrackingNumber);
                            if (shipmentEvents != null)
                            {
                                foreach (var shipmentEvent in shipmentEvents)
                                {
                                    var shipmentStatusEventModel = new ShipmentDetailsModel.ShipmentStatusEventModel
                                        {
                                            Date = shipmentEvent.Date,
                                            EventName = shipmentEvent.EventName,
                                            Location = shipmentEvent.Location
                                        };
                                    model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                                }
                            }
                        }
                    }
                }
            }

            //products in this shipment
            model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;
            foreach (var shipmentItem in request.Shipment.ShipmentItems)
            {
                var orderItem = request.Order.OrderItems.FirstOrDefault(x => x.Id == shipmentItem.OrderItemId);
                if (orderItem == null)
                    continue;
                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
                var shipmentItemModel = new ShipmentDetailsModel.ShipmentItemModel {
                    Id = shipmentItem.Id,
                    Sku = product.FormatSku(orderItem.Attributes),
                    ProductId = orderItem.ProductId,
                    ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                    ProductSeName = product.GetSeName(request.Language.Id),
                    AttributeInfo = orderItem.AttributeDescription,
                    QuantityOrdered = orderItem.Quantity,
                    QuantityShipped = shipmentItem.Quantity
                };

                model.Items.Add(shipmentItemModel);
            }

            //shipment notes 
            model.ShipmentNotes = await PrepareShipmentNotesModel(request);
            //order details model
            model.Order = await PrepareOrderModel(request);

            return model;
        }

        private async Task<IList<ShipmentDetailsModel.ShipmentNote>> PrepareShipmentNotesModel(GetShipmentDetails request)
        {
            var notes = new List<ShipmentDetailsModel.ShipmentNote>();
            foreach (var shipmentNote in (await _shipmentService.GetShipmentNotes(request.Shipment.Id))
                .Where(on => on.DisplayToCustomer)
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList())
            {
                notes.Add(new ShipmentDetailsModel.ShipmentNote {
                    Id = shipmentNote.Id,
                    ShipmentId = shipmentNote.ShipmentId,
                    HasDownload = !string.IsNullOrEmpty(shipmentNote.DownloadId),
                    Note = shipmentNote.Note,
                    CreatedOn = _dateTimeService.ConvertToUserTime(shipmentNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            return notes;
        }
        private async Task<ShipmentDetailsModel.OrderModel> PrepareOrderModel(GetShipmentDetails request)
        {
            var model = new ShipmentDetailsModel.OrderModel();
            var order = request.Order;
            model.UserFields = order.UserFields;
            model.Id = order.Id;
            model.OrderNumber = order.OrderNumber;
            model.OrderCode = order.Code;
            model.ShippingMethod = order.ShippingMethod;
            model.PickUpInStore = order.PickUpInStore;
            if (!order.PickUpInStore)
            {
                model.ShippingAddress = await _mediator.Send(new GetAddressModel {
                    Language = request.Language,
                    Address = order.ShippingAddress,
                    ExcludeProperties = false
                });
            }
            else
            {
                if (order.PickupPoint is { Address: { } })
                {
                    model.PickupAddress = await _mediator.Send(new GetAddressModel {
                        Language = request.Language,
                        Address = order.PickupPoint.Address,
                        ExcludeProperties = false
                    });
                }
            }
            return model;
        }


    }
}
