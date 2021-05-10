using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetVendorHandler : IRequestHandler<GetVendor, VendorModel>
    {
        private readonly IMediator _mediator;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheBase _cacheBase;

        private readonly VendorSettings _vendorSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;

        public GetVendorHandler(
            IMediator mediator,
            IPictureService pictureService,
            ITranslationService translationService,
            VendorSettings vendorSettings,
            MediaSettings mediaSettings,
            ISpecificationAttributeService specificationAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ICacheBase cacheBase,
            CatalogSettings catalogSettings)
        {
            _mediator = mediator;
            _pictureService = pictureService;
            _translationService = translationService;
            _vendorSettings = vendorSettings;
            _mediaSettings = mediaSettings;
            _specificationAttributeService = specificationAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _cacheBase = cacheBase;
            _catalogSettings = catalogSettings;
        }

        public async Task<VendorModel> Handle(GetVendor request, CancellationToken cancellationToken)
        {
            var model = new VendorModel
            {
                Id = request.Vendor.Id,
                Name = request.Vendor.GetTranslation(x => x.Name, request.Language.Id),
                Description = request.Vendor.GetTranslation(x => x.Description, request.Language.Id),
                MetaKeywords = request.Vendor.GetTranslation(x => x.MetaKeywords, request.Language.Id),
                MetaDescription = request.Vendor.GetTranslation(x => x.MetaDescription, request.Language.Id),
                MetaTitle = request.Vendor.GetTranslation(x => x.MetaTitle, request.Language.Id),
                SeName = request.Vendor.GetSeName(request.Language.Id),
                AllowCustomersToContactVendors = _vendorSettings.AllowCustomersToContactVendors,
                UserFields = request.Vendor.UserFields
            };

            model.Address = await _mediator.Send(new GetVendorAddress()
            {
                Language = request.Language,
                Address = request.Vendor.Address,
                ExcludeProperties = false,
            });

            //prepare picture model
            var pictureModel = new PictureModel
            {
                Id = request.Vendor.PictureId,
                FullSizeImageUrl = await _pictureService.GetPictureUrl(request.Vendor.PictureId),
                ImageUrl = await _pictureService.GetPictureUrl(request.Vendor.PictureId, _mediaSettings.VendorThumbPictureSize),
                Title = string.Format(_translationService.GetResource("Media.Vendor.ImageLinkTitleFormat"), model.Name),
                AlternateText = string.Format(_translationService.GetResource("Media.Vendor.ImageAlternateTextFormat"), model.Name)
            };
            model.PictureModel = pictureModel;

            //view/sorting/page size
            var options = await _mediator.Send(new GetViewSortSizeOptions()
            {
                Command = request.Command,
                PagingFilteringModel = request.Command,
                Language = request.Language,
                AllowCustomersToSelectPageSize = request.Vendor.AllowCustomersToSelectPageSize,
                PageSize = request.Vendor.PageSize,
                PageSizeOptions = request.Vendor.PageSizeOptions
            });
            model.PagingFilteringContext = options.command;

            IList<string> alreadyFilteredSpecOptionIds = await model.PagingFilteringContext.SpecificationFilter.GetAlreadyFilteredSpecOptionIds
              (_httpContextAccessor, _specificationAttributeService);

            //products
            var products = (await _mediator.Send(new GetSearchProductsQuery()
            {
                LoadFilterableSpecificationAttributeOptionIds = !_catalogSettings.IgnoreFilterableSpecAttributeOption,
                Customer = request.Customer,
                VendorId = request.Vendor.Id,
                StoreId = request.Store.Id,
                FilteredSpecs = alreadyFilteredSpecOptionIds,
                VisibleIndividuallyOnly = true,
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
