﻿using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Features.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetFormatBasePriceHandler : IRequestHandler<GetFormatBasePrice, string>
    {
        private readonly ITranslationService _translationService;
        private readonly IMeasureService _measureService;
        private readonly IPriceFormatter _priceFormatter;

        public GetFormatBasePriceHandler(
            ITranslationService translationService,
            IMeasureService measureService,
            IPriceFormatter priceFormatter)
        {
            _translationService = translationService;
            _measureService = measureService;
            _priceFormatter = priceFormatter;
        }

        public async Task<string> Handle(GetFormatBasePrice request, CancellationToken cancellationToken)
        {
            if (request.Product == null)
                throw new ArgumentNullException(nameof(request.Product));

            if (!request.Product.BasepriceEnabled)
                return null;

            var productAmount = request.Product.BasepriceAmount;
            //Amount in product cannot be 0
            if (productAmount == 0)
                return null;
            var referenceAmount = request.Product.BasepriceBaseAmount;
            var productUnit = await _measureService.GetMeasureWeightById(request.Product.BasepriceUnitId);
            //measure weight cannot be loaded
            if (productUnit == null)
                return null;
            var referenceUnit = await _measureService.GetMeasureWeightById(request.Product.BasepriceBaseUnitId);
            //measure weight cannot be loaded
            if (referenceUnit == null)
                return null;

            request.ProductPrice ??= request.Product.Price;

            var basePrice = request.ProductPrice.Value /
                            //do not round. otherwise, it can cause issues
                            await _measureService.ConvertWeight(productAmount, productUnit, referenceUnit, false) *
                            referenceAmount;
            var basePriceStr = _priceFormatter.FormatPrice(basePrice, false);

            var result = string.Format(_translationService.GetResource("Products.BasePrice"),
                basePriceStr, referenceAmount.ToString("G29"), referenceUnit.Name);

            return result;
        }
    }
}
