using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Products;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductDetailsAttributeChangeHandler : IRequestHandler<GetProductDetailsAttributeChange, ProductDetailsAttributeChangeModel>
    {
        private readonly IMediator _mediator;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly IPermissionService _permissionService;
        private readonly IPricingService _pricingService;
        private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
        private readonly ITaxService _taxService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ITranslationService _translationService;
        private readonly IPictureService _pictureService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly MediaSettings _mediaSettings;

        public GetProductDetailsAttributeChangeHandler(
            IMediator mediator,
            IProductAttributeParser productAttributeParser,
            IStockQuantityService stockQuantityService,
            IPermissionService permissionService,
            IPricingService priceCalculationService,
            IOutOfStockSubscriptionService outOfStockSubscriptionService,
            ITaxService taxService,
            IPriceFormatter priceFormatter,
            ITranslationService translationService,
            IPictureService pictureService,
            ShoppingCartSettings shoppingCartSettings,
            MediaSettings mediaSettings)
        {
            _mediator = mediator;
            _productAttributeParser = productAttributeParser;
            _stockQuantityService = stockQuantityService;
            _permissionService = permissionService;
            _pricingService = priceCalculationService;
            _outOfStockSubscriptionService = outOfStockSubscriptionService;
            _taxService = taxService;
            _priceFormatter = priceFormatter;
            _translationService = translationService;
            _pictureService = pictureService;
            _shoppingCartSettings = shoppingCartSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<ProductDetailsAttributeChangeModel> Handle(GetProductDetailsAttributeChange request, CancellationToken cancellationToken)
        {
            var model = new ProductDetailsAttributeChangeModel();

            var customAttributes = await _mediator.Send(new GetParseProductAttributes() { Product = request.Product, Form = request.Form });

            string warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ?
               request.Form["WarehouseId"].ToString() :
               request.Product.UseMultipleWarehouses ? request.Store.DefaultWarehouseId :
               (string.IsNullOrEmpty(request.Store.DefaultWarehouseId) ? request.Product.WarehouseId : request.Store.DefaultWarehouseId);

            //rental attributes
            DateTime? rentalStartDate = null;
            DateTime? rentalEndDate = null;
            if (request.Product.ProductTypeId == ProductType.Reservation)
            {
                request.Product.ParseReservationDates(request.Form, out rentalStartDate, out rentalEndDate);
            }

            model.Sku = request.Product.FormatSku(customAttributes, _productAttributeParser);
            model.Mpn = request.Product.FormatMpn(customAttributes, _productAttributeParser);
            model.Gtin = request.Product.FormatGtin(customAttributes, _productAttributeParser);

            if (await _permissionService.Authorize(StandardPermission.DisplayPrices) && !request.Product.EnteredPrice && request.Product.ProductTypeId != ProductType.Auction)
            {
                //we do not calculate price of "customer enters price" option is enabled
                var unitprice = await _pricingService.GetUnitPrice(request.Product,
                    request.Customer,
                    request.Currency,
                    ShoppingCartType.ShoppingCart,
                    1, customAttributes, (double?)default,
                    rentalStartDate, rentalEndDate,
                    true);

                double discountAmount = unitprice.discountAmount;
                List<ApplyDiscount> scDiscounts = unitprice.appliedDiscounts;
                double finalPrice = unitprice.unitprice;
                var productprice = await _taxService.GetProductPrice(request.Product, finalPrice);
                double finalPriceWithDiscount = productprice.productprice;
                double taxRate = productprice.taxRate;
                model.Price = _priceFormatter.FormatPrice(finalPriceWithDiscount);
            }
            //stock
            model.StockAvailability = _stockQuantityService.FormatStockMessage(request.Product, warehouseId, customAttributes);

            //out of stock subscription
            if ((request.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes
                || request.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock) &&
                request.Product.BackorderModeId == BackorderMode.NoBackorders &&
                request.Product.AllowOutOfStockSubscriptions)
            {
                var combination = _productAttributeParser.FindProductAttributeCombination(request.Product, customAttributes);

                if (combination != null)
                    if (_stockQuantityService.GetTotalStockQuantityForCombination(request.Product, combination, warehouseId: warehouseId) <= 0)
                        model.DisplayOutOfStockSubscription = true;

                if (request.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                {
                    model.DisplayOutOfStockSubscription = request.Product.AllowOutOfStockSubscriptions;
                    customAttributes = new List<CustomAttribute>();
                }

                var subscription = await _outOfStockSubscriptionService
                   .FindSubscription(request.Customer.Id,
                    request.Product.Id, customAttributes, request.Store.Id, warehouseId);

                if (subscription != null)
                    model.ButtonTextOutOfStockSubscription = _translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable");
                else
                    model.ButtonTextOutOfStockSubscription = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable");

            }


            //conditional attributes
            if (request.ValidateAttributeConditions)
            {
                var attributes = request.Product.ProductAttributeMappings;
                foreach (var attribute in attributes)
                {
                    var conditionMet = _productAttributeParser.IsConditionMet(request.Product, attribute, customAttributes);
                    if (conditionMet.HasValue)
                    {
                        if (conditionMet.Value)
                            model.EnabledAttributeMappingIds.Add(attribute.Id);
                        else
                            model.DisabledAttributeMappingids.Add(attribute.Id);
                    }
                }
            }
            //picture. used when we want to override a default product picture when some attribute is selected
            if (request.LoadPicture)
            {

                //first, try to get product attribute combination picture
                var pictureId = _productAttributeParser.FindProductAttributeCombination(request.Product, customAttributes)?.PictureId;
                if (string.IsNullOrEmpty(pictureId))
                {
                    pictureId = _productAttributeParser.ParseProductAttributeValues(request.Product, customAttributes)
                        .FirstOrDefault(attributeValue => !string.IsNullOrEmpty(attributeValue.PictureId))?.PictureId ?? "";
                }

                if (!string.IsNullOrEmpty(pictureId))
                {
                    var pictureModel = new PictureModel
                    {
                        Id = pictureId,
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(pictureId),
                        ImageUrl = await _pictureService.GetPictureUrl(pictureId, _mediaSettings.ProductDetailsPictureSize)
                    };
                    model.PictureFullSizeUrl = pictureModel.FullSizeImageUrl;
                    model.PictureDefaultSizeUrl = pictureModel.ImageUrl;
                }
            }
            return model;
        }
    }
}
