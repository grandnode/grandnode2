﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Permissions;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetSearchAutoCompleteHandler : IRequestHandler<GetSearchAutoComplete, IList<SearchAutoCompleteModel>>
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly IAclService _aclService;
    private readonly IBlogService _blogService;
    private readonly BlogSettings _blogSettings;
    private readonly IBrandService _brandService;
    private readonly CatalogSettings _catalogSettings;
    private readonly ICategoryService _categoryService;
    private readonly MediaSettings _mediaSettings;
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;
    private readonly IPictureService _pictureService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPricingService _pricingService;
    private readonly ISearchTermService _searchTermService;
    private readonly ITaxService _taxService;
    private readonly IContextAccessor _contextAccessor;

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
        IContextAccessor contextAccessor,
        IPermissionService permissionService,
        CatalogSettings catalogSettings,
        MediaSettings mediaSettings,
        BlogSettings blogSettings, AccessControlConfig accessControlConfig)
    {
        _pictureService = pictureService;
        _brandService = brandService;
        _categoryService = categoryService;
        _aclService = aclService;
        _searchTermService = searchTermService;
        _blogService = blogService;
        _pricingService = priceCalculationService;
        _taxService = taxService;
        _contextAccessor = contextAccessor;
        _priceFormatter = priceFormatter;
        _mediator = mediator;
        _permissionService = permissionService;
        _catalogSettings = catalogSettings;
        _mediaSettings = mediaSettings;
        _blogSettings = blogSettings;
        _accessControlConfig = accessControlConfig;
    }

    public async Task<IList<SearchAutoCompleteModel>> Handle(GetSearchAutoComplete request,
        CancellationToken cancellationToken)
    {
        var model = new List<SearchAutoCompleteModel>();
        var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0
            ? _catalogSettings.ProductSearchAutoCompleteNumberOfProducts
            : 10;
        var storeId = request.Store.Id;
        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(request.CategoryId))
        {
            categoryIds.Add(request.CategoryId);
            if (_catalogSettings.ShowProductsFromSubcategoriesInSearchBox)
                //include subcategories
                categoryIds.AddRange(await _mediator.Send(
                    new GetChildCategoryIds
                        { ParentCategoryId = request.CategoryId, Customer = request.Customer, Store = request.Store },
                    cancellationToken));
        }

        var products = (await _mediator.Send(new GetSearchProductsQuery {
            Customer = request.Customer,
            StoreId = storeId,
            Keywords = request.Term,
            CategoryIds = categoryIds,
            SearchSku = _catalogSettings.SearchBySku,
            SearchDescriptions = _catalogSettings.SearchByDescription,
            LanguageId = request.Language.Id,
            VisibleIndividuallyOnly = true,
            PageSize = productNumber
        }, cancellationToken)).products;

        var categories = new List<string>();
        var brands = new List<string>();

        var storeurl = _contextAccessor.StoreContext.CurrentHost.Url.TrimEnd('/');

        var displayPrices =
            await _permissionService.Authorize(StandardPermission.DisplayPrices, _contextAccessor.WorkContext.CurrentCustomer);

        foreach (var item in products)
        {
            var pictureUrl = "";
            if (_catalogSettings.ShowProductImagesInSearchAutoComplete)
            {
                var picture = item.ProductPictures.OrderByDescending(p => p.IsDefault)  
                    .ThenBy(p => p.DisplayOrder) 
                    .FirstOrDefault();
                if (picture != null)
                    pictureUrl = await _pictureService.GetPictureUrl(picture.PictureId,
                        _mediaSettings.AutoCompleteSearchThumbPictureSize);
            }

            var rating = await _mediator.Send(new GetProductReviewOverview {
                Language = request.Language,
                Product = item,
                Store = request.Store
            }, cancellationToken);


            var displayPricesItem = !item.CallForPrice && displayPrices;

            var price = displayPricesItem
                ? await PreparePrice(item, request)
                : (Price: string.Empty, PriceWithDiscount: string.Empty);

            model.Add(new SearchAutoCompleteModel {
                SearchType = "Product",
                Label = item.GetTranslation(x => x.Name, request.Language.Id) ?? "",
                Desc = item.GetTranslation(x => x.ShortDescription, request.Language.Id) ?? "",
                PictureUrl = pictureUrl,
                AllowCustomerReviews = rating.AllowCustomerReviews,
                Rating = rating.AvgRating,
                Price = price.Price,
                PriceWithDiscount = price.PriceWithDiscount,
                Url = $"{storeurl}/{item.SeName}"
            });
            categories.AddRange(item.ProductCategories.Select(category => category.CategoryId));
            brands.Add(item.BrandId);
        }

        foreach (var item in brands.Distinct())
        {
            var brand = await _brandService.GetBrandById(item);
            if (brand is not { Published: true }) continue;
            var allow = true;
            
            if (!_accessControlConfig.IgnoreAcl && !_aclService.Authorize(brand, _contextAccessor.WorkContext.CurrentCustomer))
                allow = false;

            if (!_accessControlConfig.IgnoreStoreLimitations && !_aclService.Authorize(brand, storeId))
                allow = false;
            if (!allow) continue;

            var desc = "";
            if (_catalogSettings.SearchByDescription)
                desc = "&sid=true";
            model.Add(new SearchAutoCompleteModel {
                SearchType = "Brand",
                Label = brand.GetTranslation(x => x.Name, request.Language.Id),
                Desc = "",
                PictureUrl = "",
                Url = $"{storeurl}/search?q={request.Term}&adv=true&brand={item}{desc}"
            });
        }

        foreach (var item in categories.Distinct())
        {
            var category = await _categoryService.GetCategoryById(item);
            if (category is not { Published: true }) continue;
            var allow = true;
            if (!_accessControlConfig.IgnoreAcl && !_aclService.Authorize(category, _contextAccessor.WorkContext.CurrentCustomer))
                allow = false;
            if (!_accessControlConfig.IgnoreStoreLimitations && !_aclService.Authorize(category, storeId))
                allow = false;
            if (!allow) continue;
            var desc = "";
            if (_catalogSettings.SearchByDescription)
                desc = "&sid=true";
            model.Add(new SearchAutoCompleteModel {
                SearchType = "Category",
                Label = category.GetTranslation(x => x.Name, request.Language.Id),
                Desc = "",
                PictureUrl = "",
                Url = $"{storeurl}/search?q={request.Term}&adv=true&cid={item}{desc}"
            });
        }

        if (_blogSettings.ShowBlogPostsInSearchAutoComplete)
        {
            var posts = await _blogService.GetAllBlogPosts(storeId, pageSize: productNumber,
                blogPostName: request.Term);
            model.AddRange(posts.Select(item => new SearchAutoCompleteModel {
                SearchType = "Blog",
                Label = item.GetTranslation(x => x.Title, request.Language.Id),
                Desc = "",
                PictureUrl = "",
                Url = $"{storeurl}/{item.SeName}"
            }));
        }

        //search term statistics
        if (string.IsNullOrEmpty(request.Term) || !_catalogSettings.SaveSearchAutoComplete) return model;

        var searchTerm = await _searchTermService.GetSearchTermByKeyword(request.Term, request.Store.Id);
        if (searchTerm != null)
        {
            searchTerm.Count++;
            await _searchTermService.UpdateSearchTerm(searchTerm);
        }
        else
        {
            searchTerm = new SearchTerm {
                Keyword = request.Term,
                StoreId = storeId,
                Count = 1
            };
            await _searchTermService.InsertSearchTerm(searchTerm);
        }

        return model;
    }

    private async Task<(string Price, string PriceWithDiscount)> PreparePrice(Product product,
        GetSearchAutoComplete request)
    {
        var finalPriceWithoutDiscount =
            (await _taxService.GetProductPrice(product,
                (await _pricingService.GetFinalPrice(product, request.Customer, request.Store, request.Currency,
                    includeDiscounts: false)).finalPrice)).productprice;

        var appliedPrice = await _pricingService.GetFinalPrice(product, request.Customer, request.Store,
            request.Currency, includeDiscounts: true);
        var finalPriceWithDiscount = (await _taxService.GetProductPrice(product, appliedPrice.finalPrice)).productprice;

        var price = _priceFormatter.FormatPrice(finalPriceWithoutDiscount);
        var priceWithDiscount = _priceFormatter.FormatPrice(finalPriceWithDiscount);

        return (price, priceWithDiscount);
    }
}