using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Media;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetAddToCartHandler : IRequestHandler<GetAddToCart, AddToCartModel>
    {
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ITranslationService _translationService;
        private readonly ITaxService _taxService;
        private readonly IPricingService _pricingService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IAuctionService _auctionService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly TaxSettings _taxSettings;
        private readonly MediaSettings _mediaSettings;

        public GetAddToCartHandler(
            IProductAttributeFormatter productAttributeFormatter,
            ITranslationService translationService,
            ITaxService taxService,
            IPricingService priceCalculationService,
            IPriceFormatter priceFormatter,
            IShoppingCartService shoppingCartService,
            IOrderCalculationService orderTotalCalculationService,
            IPictureService pictureService,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            IAuctionService auctionService,
            ShoppingCartSettings shoppingCartSettings,
            TaxSettings taxSettings,
            MediaSettings mediaSettings)
        {
            _productAttributeFormatter = productAttributeFormatter;
            _translationService = translationService;
            _taxService = taxService;
            _pricingService = priceCalculationService;
            _priceFormatter = priceFormatter;
            _shoppingCartService = shoppingCartService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _pictureService = pictureService;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _auctionService = auctionService;
            _shoppingCartSettings = shoppingCartSettings;
            _taxSettings = taxSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<AddToCartModel> Handle(GetAddToCart request, CancellationToken cancellationToken)
        {
            var model = new AddToCartModel
            {
                AttributeDescription = await _productAttributeFormatter.FormatAttributes(request.Product, request.Attributes),
                ProductSeName = request.Product.GetSeName(request.Language.Id),
                CartType = request.CartType,
                ProductId = request.Product.Id,
                ProductName = request.Product.GetTranslation(x => x.Name, request.Language.Id),
                Quantity = request.Quantity
            };

            //reservation info
            if (request.Product.ProductTypeId == ProductType.Reservation)
            {
                if (request.EndDate == default(DateTime) || request.EndDate == null)
                {
                    model.ReservationInfo = string.Format(_translationService.GetResource("ShoppingCart.Reservation.StartDate"), request.StartDate?.ToString(_shoppingCartSettings.ReservationDateFormat));
                }
                else
                {
                    model.ReservationInfo = string.Format(_translationService.GetResource("ShoppingCart.Reservation.Date"), request.StartDate?.ToString(_shoppingCartSettings.ReservationDateFormat), request.EndDate?.ToString(_shoppingCartSettings.ReservationDateFormat));
                }

                if (!string.IsNullOrEmpty(request.Parameter))
                {
                    model.ReservationInfo += "<br>" + string.Format(_translationService.GetResource("ShoppingCart.Reservation.Option"), request.Parameter);
                }
                if (!string.IsNullOrEmpty(request.Duration))
                {
                    model.ReservationInfo += "<br>" + string.Format(_translationService.GetResource("ShoppingCart.Reservation.Duration"), request.Duration);
                }
            }

            if (request.CartType != ShoppingCartType.Auctions)
            {
                var cartItems = request.Customer.ShoppingCartItems
                    .Where(x => x.ShoppingCartTypeId == request.CartType &&
                    x.ProductId == request.Product.Id &&
                    x.EnteredPrice == request.CustomerEnteredPrice);

                if (request.Attributes != null && request.Attributes.Any() && cartItems.Count() > 1)
                {
                    cartItems = cartItems.Where(x => x.Attributes.All(y => request.Attributes.Any(z => z.Key == y.Key && z.Value == y.Value)));
                }

                var sci = cartItems.FirstOrDefault();

                model.ItemQuantity = sci.Quantity;

                //unit prices
                if (request.Product.CallForPrice)
                {
                    model.Price = _translationService.GetResource("Products.CallForPrice");
                }
                else
                {
                    var productprices = await _taxService.GetProductPrice(request.Product, (await _pricingService.GetUnitPrice(sci, request.Product)).unitprice);
                    double taxRate = productprices.taxRate;
                    model.Price = !request.CustomerEnteredPrice.HasValue ? _priceFormatter.FormatPrice(productprices.productprice) : _priceFormatter.FormatPrice(request.CustomerEnteredPrice.Value);
                    model.DecimalPrice = request.CustomerEnteredPrice ?? productprices.productprice;
                    model.TotalPrice = _priceFormatter.FormatPrice(productprices.productprice * sci.Quantity);
                }

                //picture
                model.Picture = await PrepareCartItemPicture(request);
            }
            else
            {
                model.Picture = await PrepareCartItemPicture(request);
            }

            var cart = _shoppingCartService.GetShoppingCart(request.Store.Id, request.CartType);

            if (request.CartType != ShoppingCartType.Auctions)
            {
                model.TotalItems = cart.Sum(x => x.Quantity);
            }
            else
            {
                model.TotalItems = 0;
                var grouped = (await _auctionService.GetBidsByCustomerId(request.Customer.Id)).GroupBy(x => x.ProductId);
                foreach (var item in grouped)
                {
                    var p = await _productService.GetProductById(item.Key);
                    if (p != null && p.AvailableEndDateTimeUtc > DateTime.UtcNow)
                    {
                        model.TotalItems++;
                    }
                }
            }


            if (request.CartType == ShoppingCartType.ShoppingCart)
            {
                var subTotalIncludingTax = request.TaxDisplayType == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
                var shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(cart, subTotalIncludingTax);
                List<ApplyDiscount> orderSubTotalAppliedDiscounts = shoppingCartSubTotal.appliedDiscounts;
                double subTotalWithDiscountBase = shoppingCartSubTotal.subTotalWithDiscount;
                model.SubTotal = _priceFormatter.FormatPrice(shoppingCartSubTotal.subTotalWithoutDiscount, request.Currency, request.Language, subTotalIncludingTax);
                model.DecimalSubTotal = shoppingCartSubTotal.subTotalWithoutDiscount;
                if (shoppingCartSubTotal.discountAmount > 0)
                {
                    model.SubTotalDiscount = _priceFormatter.FormatPrice(-shoppingCartSubTotal.discountAmount, request.Currency, request.Language, subTotalIncludingTax);
                }
            }
            else if (request.CartType == ShoppingCartType.Auctions)
            {
                model.IsAuction = true;
                model.HighestBidValue = request.Product.HighestBid;
                model.HighestBid = _priceFormatter.FormatPrice(request.Product.HighestBid);
                model.EndTime = request.Product.AvailableEndDateTimeUtc;
            }

            return model;

        }

        private async Task<PictureModel> PrepareCartItemPicture(GetAddToCart request)
        {
            var sciPicture = await request.Product.GetProductPicture(request.Attributes, _productService, _pictureService, _productAttributeParser);
            return new PictureModel
            {
                Id = sciPicture?.Id,
                ImageUrl = await _pictureService.GetPictureUrl(sciPicture, _mediaSettings.AddToCartThumbPictureSize, true),
                Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), request.Product.Name),
                AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"), request.Product.Name),
            };
        }
    }
}
