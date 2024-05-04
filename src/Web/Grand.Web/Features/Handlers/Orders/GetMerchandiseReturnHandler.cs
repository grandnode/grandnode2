using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Handlers.Orders;

public class GetMerchandiseReturnHandler : IRequestHandler<GetMerchandiseReturn, MerchandiseReturnModel>
{
    private readonly IAclService _aclService;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IMediator _mediator;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly OrderSettings _orderSettings;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;
    private readonly IVendorService _vendorService;
    private readonly IWorkContext _workContext;

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
        OrderSettings orderSettings,
        ICurrencyService currencyService)
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
        _currencyService = currencyService;
    }

    public async Task<MerchandiseReturnModel> Handle(GetMerchandiseReturn request,
        CancellationToken cancellationToken)
    {
        var model = new MerchandiseReturnModel {
            OrderId = request.Order.Id,
            OrderNumber = request.Order.OrderNumber,
            OrderCode = request.Order.Code,
            ShowPickupAddress = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress,
            ShowPickupDate = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate,
            PickupDateRequired = _orderSettings.MerchandiseReturns_PickupDateRequired,
            //return reasons
            AvailableReturnReasons = await PrepareAvailableReturnReasons(),
            //return actions
            AvailableReturnActions = await PrepareAvailableReturnActions()
        };

        //products
        await PrepareItems(request, model);

        if (_orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress) await PreparePickupAddress(request, model);

        return model;
    }

    private async Task<IList<MerchandiseReturnModel.MerchandiseReturnReasonModel>> PrepareAvailableReturnReasons()
    {
        var reasons = new List<MerchandiseReturnModel.MerchandiseReturnReasonModel>();
        foreach (var rrr in await _merchandiseReturnService.GetAllMerchandiseReturnReasons())
            reasons.Add(new MerchandiseReturnModel.MerchandiseReturnReasonModel {
                Id = rrr.Id,
                Name = rrr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
            });
        return reasons;
    }

    private async Task<IList<MerchandiseReturnModel.MerchandiseReturnActionModel>> PrepareAvailableReturnActions()
    {
        var actions = new List<MerchandiseReturnModel.MerchandiseReturnActionModel>();
        foreach (var rra in await _merchandiseReturnService.GetAllMerchandiseReturnActions())
            actions.Add(new MerchandiseReturnModel.MerchandiseReturnActionModel {
                Id = rra.Id,
                Name = rra.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
            });
        return actions;
    }

    private async Task PrepareItems(GetMerchandiseReturn request, MerchandiseReturnModel model)
    {
        var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);
        var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
        foreach (var orderItem in request.Order.OrderItems)
        {
            var qtyDelivery = shipments.Where(x => x.DeliveryDateUtc.HasValue).SelectMany(x => x.ShipmentItems)
                .Where(x => x.OrderItemId == orderItem.Id).Sum(x => x.Quantity);

            var merchandiseReturns =
                await _merchandiseReturnService.SearchMerchandiseReturns(orderItemId: orderItem.Id);
            var qtyReturn = merchandiseReturns.Sum(rr => rr.MerchandiseReturnItems.Sum(rrItem => rrItem.Quantity));

            var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
            if (product.NotReturnable) continue;


            var orderItemModel = new MerchandiseReturnModel.OrderItemModel {
                Id = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id),
                AttributeInfo = orderItem.AttributeDescription,
                VendorId = orderItem.VendorId,
                VendorName = string.IsNullOrEmpty(orderItem.VendorId)
                    ? ""
                    : (await _vendorService.GetVendorById(orderItem.VendorId))?.Name,
                Quantity = qtyDelivery - qtyReturn
            };
            if (orderItemModel.Quantity > 0)
                model.Items.Add(orderItemModel);

            orderItemModel.IncludingTax = request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
            orderItemModel.UnitPrice = _priceFormatter.FormatPrice(
                request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax
                    ? orderItem.UnitPriceInclTax
                    : orderItem.UnitPriceExclTax, currency);
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
            if (country != null &&
                (!country.AllowsShipping || !_aclService.Authorize(country, request.Store.Id))) continue;
            addresses.Add(item);
        }

        foreach (var address in addresses)
        {
            var addressModel = await _mediator.Send(new GetAddressModel {
                Language = request.Language,
                Store = request.Store,
                Address = address,
                ExcludeProperties = false
            });
            model.ExistingAddresses.Add(addressModel);
        }

        //new address
        var countries = await _countryService.GetAllCountriesForShipping(request.Language.Id, request.Store.Id);
        model.MerchandiseReturnNewAddress = await _mediator.Send(new GetAddressModel {
            Language = request.Language,
            Store = request.Store,
            Address = null,
            ExcludeProperties = false,
            LoadCountries = () => countries,
            PrePopulateWithCustomerFields = true,
            Customer = _workContext.CurrentCustomer
        });
    }
}