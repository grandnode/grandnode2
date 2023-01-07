﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Web.Features.Models.Products;
using MediatR;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductLayoutViewPathHandler : IRequestHandler<GetProductLayoutViewPath, string>
    {
        private readonly IProductLayoutService _productLayoutService;

        public GetProductLayoutViewPathHandler(IProductLayoutService productLayoutService)
        {
            _productLayoutService = productLayoutService;
        }

        public async Task<string> Handle(GetProductLayoutViewPath request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.ProductLayoutId))
                throw new ArgumentNullException(nameof(request.ProductLayoutId));

            var layout = await _productLayoutService.GetProductLayoutById(request.ProductLayoutId) ?? (await _productLayoutService.GetAllProductLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;

        }
    }
}
