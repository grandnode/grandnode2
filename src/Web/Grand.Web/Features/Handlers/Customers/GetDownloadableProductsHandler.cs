﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetDownloadableProductsHandler : IRequestHandler<GetDownloadableProducts, CustomerDownloadableProductsModel>
    {
        private readonly IProductService _productService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IGroupService _groupService;
        private readonly IMediator _mediator;

        public GetDownloadableProductsHandler(
            IProductService productService,
            IDateTimeService dateTimeService,
            IGroupService groupService,
            IMediator mediator)
        {
            _productService = productService;
            _dateTimeService = dateTimeService;
            _groupService = groupService;
            _mediator = mediator;
        }

        public async Task<CustomerDownloadableProductsModel> Handle(GetDownloadableProducts request, CancellationToken cancellationToken)
        {
            var model = new CustomerDownloadableProductsModel();

            var query = new GetOrderQuery {
                StoreId = request.Store.Id
            };

            if (!await _groupService.IsOwner(request.Customer))
                query.CustomerId = request.Customer.Id;
            else
                query.OwnerId = request.Customer.Id;

            var orders = await _mediator.Send(query, cancellationToken);

            foreach (var order in orders)
            {
                foreach (var orderitem in order.OrderItems)
                {
                    var product = await _productService.GetProductByIdIncludeArch(orderitem.ProductId);
                    if (product is not { IsDownload: true }) continue;
                    
                    var itemModel = new CustomerDownloadableProductsModel.DownloadableProductsModel {
                        OrderItemGuid = orderitem.OrderItemGuid,
                        OrderId = order.Id,
                        OrderNumber = order.OrderNumber,
                        CreatedOn = _dateTimeService.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
                        ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                        ProductSeName = product.GetSeName(request.Language.Id),
                        ProductAttributes = orderitem.AttributeDescription,
                        ProductId = orderitem.ProductId
                    };
                    model.Items.Add(itemModel);

                    if (order.IsDownloadAllowed(orderitem, product))
                        itemModel.DownloadId = product.DownloadId;

                    if (order.IsLicenseDownloadAllowed(orderitem, product))
                        itemModel.LicenseId = !string.IsNullOrEmpty(orderitem.LicenseDownloadId) ? orderitem.LicenseDownloadId : "";
                }
            }

            return model;
        }
    }
}
