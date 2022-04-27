using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Commands.Messages;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetMerchandiseReturnTokensCommandHandler : IRequestHandler<GetMerchandiseReturnTokensCommand, LiquidMerchandiseReturn>
    {
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;
        private readonly IProductService _productService;

        public GetMerchandiseReturnTokensCommandHandler(
            ITranslationService translationService,
            ICountryService countryService,
            IProductService productService)
        {
            _translationService = translationService;
            _countryService = countryService;
            _productService = productService;
        }

        public async Task<LiquidMerchandiseReturn> Handle(GetMerchandiseReturnTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidMerchandiseReturn = new LiquidMerchandiseReturn(request.MerchandiseReturn, request.Store, request.Host, request.Order, request.MerchandiseReturnNote);

            var country = await _countryService.GetCountryById(request.MerchandiseReturn.PickupAddress.CountryId);

            liquidMerchandiseReturn.Status = request.MerchandiseReturn.MerchandiseReturnStatus.GetTranslationEnum(_translationService, request.Language.Id);

            liquidMerchandiseReturn.PickupAddressStateProvince =
                            !string.IsNullOrEmpty(request.MerchandiseReturn.PickupAddress.StateProvinceId) ?
                            country?.StateProvinces.FirstOrDefault(x => x.Id == request.MerchandiseReturn.PickupAddress.StateProvinceId)?.GetTranslation(x => x.Name, request.Language.Id) : "";

            liquidMerchandiseReturn.PickupAddressCountry =
                            !string.IsNullOrEmpty(request.MerchandiseReturn.PickupAddress.CountryId) ?
                            (country?.GetTranslation(x => x.Name, request.Language.Id)) : "";

            foreach (var merchandiseReturnItem in request.MerchandiseReturn.MerchandiseReturnItems)
            {
                var orderItem = request.Order.OrderItems.Where(x => x.Id == merchandiseReturnItem.OrderItemId).First();
                var product = await _productService.GetProductById(orderItem.ProductId);
                var liqitem = new LiquidMerchandiseReturnItem(merchandiseReturnItem, orderItem, product, request.Order.CustomerLanguageId);

                liquidMerchandiseReturn.Items.Add(liqitem);
            }

            return liquidMerchandiseReturn;

        }
    }
}
