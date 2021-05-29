using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetSearchAutoCompleteHandler : IRequestHandler<GetSearchAutoComplete, IList<SearchAutoCompleteModel>>
    {
        private readonly IPictureService _pictureService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IAclService _aclService;
        private readonly ISearchTermService _searchTermService;
        private readonly IBlogService _blogService;
        private readonly ITaxService _taxService;
        private readonly IPricingService _pricingService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;
        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly BlogSettings _blogSettings;

        public GetSearchAutoCompleteHandler(
            IPictureService pictureService,
            IBrandService brandService,
            ICategoryService categoryService,
            IAclService aclService,
            ISearchTermService searchTermService,
            IBlogService blogService,
            IPricingService priceCalculationService,
            ITaxService taxService,
            IPriceFormatter priceFormatter,
            IMediator mediator,
            IWorkContext workContext,
            IPermissionService permissionService,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            BlogSettings blogSettings)
        {
            _pictureService = pictureService;
            _brandService = brandService;
            _categoryService = categoryService;
            _aclService = aclService;
            _searchTermService = searchTermService;
            _blogService = blogService;
            _pricingService = priceCalculationService;
            _taxService = taxService;
            _workContext = workContext;
            _priceFormatter = priceFormatter;
            _mediator = mediator;
            _permissionService = permissionService;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _blogSettings = blogSettings;
        }

        public async Task<IList<SearchAutoCompleteModel>> Handle(GetSearchAutoComplete request, CancellationToken cancellationToken)
        {
            var model = new List<SearchAutoCompleteModel>();
            var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0 ?
                _catalogSettings.ProductSearchAutoCompleteNumberOfProducts : 10;
            var storeId = request.Store.Id;
            var categoryIds = new List<string>();
            if (!string.IsNullOrEmpty(request.CategoryId))
            {
                categoryIds.Add(request.CategoryId);
                if (_catalogSettings.ShowProductsFromSubcategoriesInSearchBox)
                {
                    //include subcategories
                    categoryIds.AddRange(await _mediator.Send(new GetChildCategoryIds() { ParentCategoryId = request.CategoryId, Customer = request.Customer, Store = request.Store }));
                }
            }

            var products = (await _mediator.Send(new GetSearchProductsQuery()
            {
                Customer = request.Customer,
                StoreId = storeId,
                Keywords = request.Term,
                CategoryIds = categoryIds,
                SearchSku = _catalogSettings.SearchBySku,
                SearchDescriptions = _catalogSettings.SearchByDescription,
                LanguageId = request.Language.Id,
                VisibleIndividuallyOnly = true,
                PageSize = productNumber
            })).products;

            var categories = new List<string>();
            var brands = new List<string>();

            var storeurl = _workContext.CurrentStore.SslEnabled ? _workContext.CurrentStore.SecureUrl.TrimEnd('/') : _workContext.CurrentStore.Url.TrimEnd('/');

            var displayPrices = await _permissionService.Authorize(StandardPermission.DisplayPrices, _workContext.CurrentCustomer);

            foreach (var item in products)
            {
                var pictureUrl = "";
                if (_catalogSettings.ShowProductImagesInSearchAutoComplete)
                {
                    var picture = item.ProductPictures.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                    if (picture != null)
                        pictureUrl = await _pictureService.GetPictureUrl(picture.PictureId, _mediaSettings.AutoCompleteSearchThumbPictureSize);
                }
                var rating = await _mediator.Send(new GetProductReviewOverview()
                {
                    Language = request.Language,
                    Product = item,
                    Store = request.Store
                });

                var price = displayPrices ? await PreparePrice(item, request) : (Price: string.Empty, PriceWithDiscount: string.Empty);

                model.Add(new SearchAutoCompleteModel()
                {
                    SearchType = "Product",
                    Label = item.GetTranslation(x => x.Name, request.Language.Id) ?? "",
                    Desc = item.GetTranslation(x => x.ShortDescription, request.Language.Id) ?? "",
                    PictureUrl = pictureUrl,
                    AllowCustomerReviews = rating.AllowCustomerReviews,
                    Rating = rating.TotalReviews > 0 ? (((rating.RatingSum * 100) / rating.TotalReviews) / 5) : 0,
                    Price = price.Price,
                    PriceWithDiscount = price.PriceWithDiscount,
                    Url = $"{storeurl}/{item.SeName}"
                });
                foreach (var category in item.ProductCategories)
                {
                    categories.Add(category.CategoryId);
                }
                brands.Add(item.BrandId);
            }

            foreach (var item in brands.Distinct())
            {
                var brand = await _brandService.GetBrandById(item);
                if (brand != null && brand.Published)
                {
                    var allow = true;
                    if (!CommonHelper.IgnoreAcl)
                        if (!_aclService.Authorize(brand, _workContext.CurrentCustomer))
                            allow = false;
                    if (!CommonHelper.IgnoreStoreLimitations)
                        if (!_aclService.Authorize(brand, storeId))
                            allow = false;
                    if (allow)
                    {
                        var desc = "";
                        if (_catalogSettings.SearchByDescription)
                            desc = "&sid=true";
                        model.Add(new SearchAutoCompleteModel()
                        {
                            SearchType = "Brand",
                            Label = brand.GetTranslation(x => x.Name, request.Language.Id),
                            Desc = "",
                            PictureUrl = "",
                            Url = $"{storeurl}/search?q={request.Term}&adv=true&brand={item}{desc}"
                        });
                    }
                }
            }
            foreach (var item in categories.Distinct())
            {
                var category = await _categoryService.GetCategoryById(item);
                if (category != null && category.Published)
                {
                    var allow = true;
                    if (!CommonHelper.IgnoreAcl)
                        if (!_aclService.Authorize(category, _workContext.CurrentCustomer))
                            allow = false;
                    if (!CommonHelper.IgnoreStoreLimitations)
                        if (!_aclService.Authorize(category, storeId))
                            allow = false;
                    if (allow)
                    {
                        var desc = "";
                        if (_catalogSettings.SearchByDescription)
                            desc = "&sid=true";
                        model.Add(new SearchAutoCompleteModel()
                        {
                            SearchType = "Category",
                            Label = category.GetTranslation(x => x.Name, request.Language.Id),
                            Desc = "",
                            PictureUrl = "",
                            Url = $"{storeurl}/search?q={request.Term}&adv=true&cid={item}{desc}"
                        });
                    }
                }
            }

            if (_blogSettings.ShowBlogPostsInSearchAutoComplete)
            {
                var posts = await _blogService.GetAllBlogPosts(storeId: storeId, pageSize: productNumber, blogPostName: request.Term);
                foreach (var item in posts)
                {
                    model.Add(new SearchAutoCompleteModel()
                    {
                        SearchType = "Blog",
                        Label = item.GetTranslation(x => x.Title, request.Language.Id),
                        Desc = "",
                        PictureUrl = "",
                        Url = $"{storeurl}/{item.SeName}"
                    });
                }
            }
            //search term statistics
            if (!String.IsNullOrEmpty(request.Term) && _catalogSettings.SaveSearchAutoComplete)
            {
                var searchTerm = await _searchTermService.GetSearchTermByKeyword(request.Term, request.Store.Id);
                if (searchTerm != null)
                {
                    searchTerm.Count++;
                    await _searchTermService.UpdateSearchTerm(searchTerm);
                }
                else
                {
                    searchTerm = new SearchTerm
                    {
                        Keyword = request.Term,
                        StoreId = storeId,
                        Count = 1
                    };
                    await _searchTermService.InsertSearchTerm(searchTerm);
                }

            }
            return model;
        }

        private async Task<(string Price, string PriceWithDiscount)> PreparePrice(Product product, GetSearchAutoComplete request)
        {
            string price, priceWithDiscount;

            double finalPriceWithoutDiscount =
                (await (_taxService.GetProductPrice(product,
                (await _pricingService.GetFinalPrice(product, request.Customer, request.Currency, includeDiscounts: false)).finalPrice))).productprice;

            var appliedPrice = (await _pricingService.GetFinalPrice(product, request.Customer, request.Currency, includeDiscounts: true));
            var finalPriceWithDiscount = (await _taxService.GetProductPrice(product, appliedPrice.finalPrice)).productprice;

            price = _priceFormatter.FormatPrice(finalPriceWithoutDiscount);
            priceWithDiscount = _priceFormatter.FormatPrice(finalPriceWithDiscount);

            return (price, priceWithDiscount);
        }
    }
}
