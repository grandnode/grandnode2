using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Orders
{
    public class GetMerchandiseReturnHandler : IRequestHandler<GetMerchandiseReturn, MerchandiseReturnModel>
    {
        private readonly IWorkContext _workContext;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICountryService _countryService;
        private readonly IAclService _aclService;
        private readonly IVendorService _vendorService;
        private readonly IMediator _mediator;
        private readonly OrderSettings _orderSettings;

        public GetMerchandiseReturnHandler(
            IWorkContext workContext,
            IMerchandiseReturnService merchandiseReturnService,
            IShipmentService shipmentService,
            IProductService productService,
            IPriceFormatter priceFormatter,
            ICountryService countryService,
            IAclService aclService,
            IVendorService vendorService,
            IMediator mediator,
            OrderSettings orderSettings
            )
        {
            _workContext = workContext;
            _merchandiseReturnService = merchandiseReturnService;
            _shipmentService = shipmentService;
            _productService = productService;
            _priceFormatter = priceFormatter;
            _countryService = countryService;
            _aclService = aclService;
            _vendorService = vendorService;
            _mediator = mediator;
            _orderSettings = orderSettings;
        }

        public async Task<MerchandiseReturnModel> Handle(GetMerchandiseReturn request, CancellationToken cancellationToken)
        {
            var model = new MerchandiseReturnModel();
            model.OrderId = request.Order.Id;
            model.OrderNumber = request.Order.OrderNumber;
            model.OrderCode = request.Order.Code;
            model.ShowPickupAddress = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress;
            model.ShowPickupDate = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate;
            model.PickupDateRequired = _orderSettings.MerchandiseReturns_PickupDateRequired;

            //return reasons
            model.AvailableReturnReasons = await PrepareAvailableReturnReasons();

            //return actions
            model.AvailableReturnActions = await PrepareAvailableReturnActions();

            //products
            await PrepareItems(request, model);

            if (_orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            {
                await PreparePickupAddress(request, model);
            }

            return model;
        }

        private async Task<IList<MerchandiseReturnModel.MerchandiseReturnReasonModel>> PrepareAvailableReturnReasons()
        {
            var reasons = new List<MerchandiseReturnModel.MerchandiseReturnReasonModel>();
            foreach (var rrr in await _merchandiseReturnService.GetAllMerchandiseReturnReasons())
                reasons.Add(new MerchandiseReturnModel.MerchandiseReturnReasonModel()
                {
                    Id = rrr.Id,
                    Name = rrr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
                });
            return reasons;
        }

        private async Task<IList<MerchandiseReturnModel.MerchandiseReturnActionModel>> PrepareAvailableReturnActions()
        {
            var actions = new List<MerchandiseReturnModel.MerchandiseReturnActionModel>();
            foreach (var rra in await _merchandiseReturnService.GetAllMerchandiseReturnActions())
                actions.Add(new MerchandiseReturnModel.MerchandiseReturnActionModel()
                {
                    Id = rra.Id,
                    Name = rra.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
                });
            return actions;
        }

        private async Task PrepareItems(GetMerchandiseReturn request, MerchandiseReturnModel model)
        {
            var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);

            foreach (var orderItem in request.Order.OrderItems)
            {
                var qtyDelivery = shipments.Where(x => x.DeliveryDateUtc.HasValue).SelectMany(x => x.ShipmentItems).Where(x => x.OrderItemId == orderItem.Id).Sum(x => x.Quantity);

                var query = new GetMerchandiseReturnQuery()
                {
                    StoreId = request.Store.Id,
                };

                var merchandiseReturns = await _merchandiseReturnService.SearchMerchandiseReturns(orderItemId: orderItem.Id);
                int qtyReturn = 0;

                foreach (var rr in merchandiseReturns)
                {
                    foreach (var rrItem in rr.MerchandiseReturnItems)
                    {
                        qtyReturn += rrItem.Quantity;
                    }
                }

                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
                if (!product.NotReturnable)
                {
                    var orderItemModel = new MerchandiseReturnModel.OrderItemModel
                    {
                        Id = orderItem.Id,
                        ProductId = orderItem.ProductId,
                        ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id),
                        AttributeInfo = orderItem.AttributeDescription,
                        VendorId = orderItem.VendorId,
                        VendorName = string.IsNullOrEmpty(orderItem.VendorId) ? "" : (await _vendorService.GetVendorById(orderItem.VendorId))?.Name,
                        Quantity = qtyDelivery - qtyReturn,
                    };
                    if (orderItemModel.Quantity > 0)
                        model.Items.Add(orderItemModel);
                    //unit price
                    if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                    {
                        //including tax
                        orderItemModel.UnitPrice = await _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax, request.Order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);
                    }
                    else
                    {
                        //excluding tax
                        orderItemModel.UnitPrice = await _priceFormatter.FormatPrice(orderItem.UnitPriceExclTax, request.Order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);
                    }
                }
            }

        }

        private async Task PreparePickupAddress(GetMerchandiseReturn request, MerchandiseReturnModel model)
        {
            //existing addresses
            var addresses = new List<Address>();
            foreach (var item in _workContext.CurrentCustomer.Addresses)
            {
                if (string.IsNullOrEmpty(item.CountryId))
                {
                    addresses.Add(item);
                    continue;
                }
                var country = await _countryService.GetCountryById(item.CountryId);
                if (country == null || (country.AllowsShipping && _aclService.Authorize(country, request.Store.Id)))
                {
                    addresses.Add(item);
                    continue;
                }
            }

            foreach (var address in addresses)
            {
                var addressModel = await _mediator.Send(new GetAddressModel()
                {
                    Language = request.Language,
                    Store = request.Store,
                    Address = address,
                    ExcludeProperties = false,
                });
                model.ExistingAddresses.Add(addressModel);
            }

            //new address
            var countries = await _countryService.GetAllCountriesForShipping(request.Language.Id, request.Store.Id);
            model.NewAddress = await _mediator.Send(new GetAddressModel()
            {
                Language = request.Language,
                Store = request.Store,
                Address = null,
                ExcludeProperties = false,
                LoadCountries = () => countries,
                PrePopulateWithCustomerFields = true,
                Customer = _workContext.CurrentCustomer,
            });
        }
    }
}
