using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCollectionHandler : IRequestHandler<GetCollection, CollectionModel>
{
    private readonly ICacheBase _cacheBase;

    private readonly CatalogSettings _catalogSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMediator _mediator;
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public GetCollectionHandler(
        IMediator mediator,
        ICacheBase cacheBase,
        ISpecificationAttributeService specificationAttributeService,
        IHttpContextAccessor httpContextAccessor,
        CatalogSettings catalogSettings)
    {
        _mediator = mediator;
        _cacheBase = cacheBase;
        _specificationAttributeService = specificationAttributeService;
        _httpContextAccessor = httpContextAccessor;
        _catalogSettings = catalogSettings;
    }

    public async Task<CollectionModel> Handle(GetCollection request, CancellationToken cancellationToken)
    {
        var model = request.Collection.ToModel(request.Language);

        if (request.Command is { OrderBy: null } && request.Collection.DefaultSort >= 0)
            request.Command.OrderBy = request.Collection.DefaultSort;

        //view/sorting/page size
        var options = await _mediator.Send(new GetViewSortSizeOptions {
            Command = request.Command,
            PagingFilteringModel = request.Command,
            Language = request.Language,
            AllowCustomersToSelectPageSize = request.Collection.AllowCustomersToSelectPageSize,
            PageSizeOptions = request.Collection.PageSizeOptions,
            PageSize = request.Collection.PageSize
        }, cancellationToken);
        model.PagingFilteringContext = options.command;

        //featured products
        if (!_catalogSettings.IgnoreFeaturedProducts)
        {
            IPagedList<Product> featuredProducts = null;

            //We cache a value indicating whether we have featured products
            var cacheKey = string.Format(CacheKeyConst.COLLECTION_HAS_FEATURED_PRODUCTS_KEY,
                request.Collection.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            var hasFeaturedProductsCache = await _cacheBase.GetAsync<bool?>(cacheKey, async () =>
            {
                featuredProducts = (await _mediator.Send(new GetSearchProductsQuery {
                    PageSize = _catalogSettings.LimitOfFeaturedProducts,
                    CollectionId = request.Collection.Id,
                    Customer = request.Customer,
                    StoreId = request.Store.Id,
                    VisibleIndividuallyOnly = true,
                    FeaturedProducts = true
                }, cancellationToken)).products;
                return featuredProducts.Any();
            });
            if (hasFeaturedProductsCache.HasValue && hasFeaturedProductsCache.Value && featuredProducts == null)
                //cache indicates that the collection has featured products
                featuredProducts = (await _mediator.Send(new GetSearchProductsQuery {
                    PageSize = _catalogSettings.LimitOfFeaturedProducts,
                    CollectionId = request.Collection.Id,
                    Customer = request.Customer,
                    StoreId = request.Store.Id,
                    VisibleIndividuallyOnly = true,
                    FeaturedProducts = true
                }, cancellationToken)).products;
            if (featuredProducts != null && featuredProducts.Any())
                model.FeaturedProducts = (await _mediator.Send(new GetProductOverview {
                    Products = featuredProducts
                }, cancellationToken)).ToList();
        }

        IList<string> alreadyFilteredSpecOptionIds =
            await model.PagingFilteringContext.SpecificationFilter.GetAlreadyFilteredSpecOptionIds
                (_httpContextAccessor.HttpContext?.Request.Query, _specificationAttributeService);

        var products = await _mediator.Send(new GetSearchProductsQuery {
            LoadFilterableSpecificationAttributeOptionIds = !_catalogSettings.IgnoreFilterableSpecAttributeOption,
            CollectionId = request.Collection.Id,
            Customer = request.Customer,
            StoreId = request.Store.Id,
            VisibleIndividuallyOnly = true,
            FeaturedProducts = _catalogSettings.IncludeFeaturedProductsInNormalLists ? null : false,
            FilteredSpecs = alreadyFilteredSpecOptionIds,
            OrderBy = (ProductSortingEnum)request.Command.OrderBy!,
            Rating = request.Command.Rating,
            PageIndex = request.Command.PageNumber - 1,
            PageSize = request.Command.PageSize
        }, cancellationToken);

        model.Products = (await _mediator.Send(new GetProductOverview {
            Products = products.products,
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages
        }, cancellationToken)).ToList();

        model.PagingFilteringContext.LoadPagedList(products.products);

        //specs
        await model.PagingFilteringContext.SpecificationFilter.PrepareSpecsFilters(alreadyFilteredSpecOptionIds,
            products.filterableSpecificationAttributeOptionIds,
            _specificationAttributeService, _httpContextAccessor.HttpContext?.Request.GetDisplayUrl(),
            request.Language.Id);

        return model;
    }
}