﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.Features.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetProductAttributeHandler : IRequestHandler<GetProductAttribute, ProductAttribute>
    {
        private readonly IProductAttributeService _productAttributeService;

        public GetProductAttributeHandler(IProductAttributeService productAttributeService)
        {
            _productAttributeService = productAttributeService;
        }

        public async Task<ProductAttribute> Handle(GetProductAttribute request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Id))
                throw new ArgumentNullException(nameof(request.Id));

            return await _productAttributeService.GetProductAttributeById(request.Id);
        }
    }
}
