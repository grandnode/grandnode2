using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetBrandHandler : IRequestHandler<GetBrand, BrandModel>
    {
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly CatalogSettings _catalogSettings;

        public GetBrandHandler(
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

        public async Task<BrandModel> Handle(GetBrand request, CancellationToken cancellationToken)
        {
            var model = request.Brand.ToModel(request.Language);

            if (request.Command != null && request.Command.OrderBy == null && request.Brand.DefaultSort >= 0)
                request.Command.OrderBy = request.Brand.DefaultSort;

            //view/sorting/page size
            var options = await _mediator.Send(new GetViewSortSizeOptions()
            {
                Command = request.Command,
                PagingFilteringModel = request.Command,
                Language = request.Language,
                AllowCustomersToSelectPageSize = request.Brand.AllowCustomersToSelectPageSize,
                PageSizeOptions = request.Brand.PageSizeOptions,
                PageSize = request.Brand.PageSize
            });
            model.PagingFilteringContext = options.command;

            IList<string> alreadyFilteredSpecOptionIds = await model.PagingFilteringContext.SpecificationFilter.GetAlreadyFilteredSpecOptionIds
                (_httpContextAccessor, _specificationAttributeService);

            var products = (await _mediator.Send(new GetSearchProductsQuery()
            {
                LoadFilterableSpecificationAttributeOptionIds = !_catalogSettings.IgnoreFilterableSpecAttributeOption,
                BrandId = request.Brand.Id,
                Customer = request.Customer,
                StoreId = request.Store.Id,
                VisibleIndividuallyOnly = true,
                FeaturedProducts = _catalogSettings.IncludeFeaturedProductsInNormalLists ? null : (bool?)false,
                FilteredSpecs = alreadyFilteredSpecOptionIds,
                OrderBy = (ProductSortingEnum)request.Command.OrderBy,
                PageIndex = request.Command.PageNumber - 1,
                PageSize = request.Command.PageSize
            }));

            model.Products = (await _mediator.Send(new GetProductOverview()
            {
                Products = products.products,
                PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages
            })).ToList();

            model.PagingFilteringContext.LoadPagedList(products.products);

            //specs
            await model.PagingFilteringContext.SpecificationFilter.PrepareSpecsFilters(alreadyFilteredSpecOptionIds,
                products.filterableSpecificationAttributeOptionIds,
                _specificationAttributeService, _httpContextAccessor, _cacheBase, request.Language.Id);

            return model;
        }
    }
}
