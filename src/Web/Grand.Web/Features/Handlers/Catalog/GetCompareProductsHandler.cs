﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching.Constants;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCompareProductsHandler : IRequestHandler<GetCompareProducts, CompareProductsModel>
{
    private readonly IAclService _aclService;
    private readonly CatalogSettings _catalogSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMediator _mediator;
    private readonly IProductService _productService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public GetCompareProductsHandler(
        IProductService productService,
        IWorkContextAccessor workContextAccessor,
        IAclService aclService,
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        CatalogSettings catalogSettings)
    {
        _productService = productService;
        _workContextAccessor = workContextAccessor;
        _aclService = aclService;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _catalogSettings = catalogSettings;
    }

    public async Task<CompareProductsModel> Handle(GetCompareProducts request, CancellationToken cancellationToken)
    {
        var model = new CompareProductsModel {
            IncludeShortDescriptionInCompareProducts = _catalogSettings.IncludeShortDescriptionInCompareProducts,
            IncludeFullDescriptionInCompareProducts = _catalogSettings.IncludeFullDescriptionInCompareProducts
        };

        var products = new List<Product>();
        var productIds = GetComparedProductIds();
        foreach (var id in productIds)
        {
            var product = await _productService.GetProductById(id);
            if (product is { Published: true })
                products.Add(product);
        }

        //ACL and store acl
        products = products.Where(p =>
            _aclService.Authorize(p, _workContextAccessor.WorkContext.CurrentCustomer) &&
            _aclService.Authorize(p, _workContextAccessor.WorkContext.CurrentStore.Id)).ToList();
        //availability dates
        products = products.Where(p => p.IsAvailable()).ToList();

        (await _mediator.Send(new GetProductOverview {
            PrepareSpecificationAttributes = true,
            Products = products,
            ProductThumbPictureSize = request.PictureProductThumbSize
        }, cancellationToken)).ToList().ForEach(model.Products.Add);

        return model;
    }

    protected virtual List<string> GetComparedProductIds()
    {
        //try to get cookie
        if (!_httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(CacheKey.PRODUCTS_COMPARE_COOKIE_NAME,
                out var productIdsCookie) || string.IsNullOrEmpty(productIdsCookie))
            return [];

        //get array of string product identifiers from cookie
        var productIds = productIdsCookie.Split(['|'], StringSplitOptions.RemoveEmptyEntries);

        //return list of int product identifiers
        return productIds.Select(productId => productId).Distinct().Take(10).ToList();
    }
}