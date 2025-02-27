using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Admin;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

public class SearchController : BaseAdminController
{
    private readonly AdminSearchSettings _adminSearchSettings;
    private readonly IBlogService _blogService;
    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;
    private readonly ICollectionService _collectionService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly INewsService _newsService;
    private readonly IOrderService _orderService;
    private readonly IPageService _pageService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly IContextAccessor _contextAccessor;

    public SearchController(IProductService productService, ICategoryService categoryService,
        IBrandService brandService, ICollectionService collectionService,
        IPageService pageService, INewsService newsService, IBlogService blogService, ICustomerService customerService,
        IOrderService orderService,
        AdminSearchSettings adminSearchSettings, ITranslationService translationService, IContextAccessor contextAccessor,
        IGroupService groupService,
        IStoreService storeService, IVendorService vendorService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _pageService = pageService;
        _newsService = newsService;
        _blogService = blogService;
        _customerService = customerService;
        _orderService = orderService;
        _adminSearchSettings = adminSearchSettings;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
        _groupService = groupService;
        _storeService = storeService;
        _vendorService = vendorService;
    }

    [HttpPost]
    public async Task<IActionResult> Index(string searchTerm, FoundMenuItem[] foundMenuItems)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return Json("error");

        if (!await _groupService.IsAdmin(_contextAccessor.WorkContext.CurrentCustomer)) return Json("Access Denied");
        //object = actual result, int = display order for sorting
        var result = new List<Tuple<object, int>>();

        if (searchTerm.Length >= _adminSearchSettings.MinSearchTermLength)
        {
            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInProducts)
            {
                var products = (await _productService.SearchProducts(keywords: searchTerm,
                    pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count, showHidden: true)).products;
                foreach (var product in products)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = product.Name,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Product/Edit/") + product.Id,
                        source = _translationService.GetResource("Admin.Catalog.Products")
                    }, _adminSearchSettings.ProductsDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInCategories)
            {
                var categories = await _categoryService.GetAllCategories(categoryName: searchTerm,
                    pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count, showHidden:
                    await _groupService.IsAdmin(_contextAccessor.WorkContext.CurrentCustomer));
                foreach (var category in categories)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = category.Name,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Category/Edit/") + category.Id,
                        source = _translationService.GetResource("Admin.Catalog.Categories")
                    }, _adminSearchSettings.CategoriesDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInCollections)
            {
                var collections = await _collectionService.GetAllCollections(searchTerm,
                    pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count, showHidden: true);
                foreach (var collection in collections)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = collection.Name,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Collection/Edit/") + collection.Id,
                        source = _translationService.GetResource("Admin.Catalog.Collections")
                    }, _adminSearchSettings.CollectionsDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInPages)
            {
                var pages = (await _pageService.GetAllPages("")).Where(x =>
                    (x.SystemName != null && x.SystemName.ToLower().Contains(searchTerm.ToLower())) ||
                    (x.Title != null && x.Title.ToLower().Contains(searchTerm.ToLower())));
                foreach (var page in pages)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = page.SystemName,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Page/Edit/") + page.Id,
                        source = _translationService.GetResource("Admin.Content.Pages")
                    }, _adminSearchSettings.PagesDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInNews)
            {
                var news = await _newsService.GetAllNews(newsTitle: searchTerm,
                    pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count, showHidden: true);
                foreach (var signleNews in news)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = signleNews.Title,
                        link = Url.Content($"~/{Constants.AreaAdmin}/News/Edit/") + signleNews.Id,
                        source = _translationService.GetResource("Admin.Content.News")
                    }, _adminSearchSettings.NewsDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInBlogs)
            {
                var blogPosts = await _blogService.GetAllBlogPosts(blogPostName: searchTerm,
                    pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count, showHidden: true);
                foreach (var blogPost in blogPosts)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = blogPost.Title,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Blog/Edit/") + blogPost.Id,
                        source = _translationService.GetResource("Admin.Content.Blog")
                    }, _adminSearchSettings.BlogsDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInCustomers)
            {
                var customersByEmail = await _customerService.GetAllCustomers(email: searchTerm,
                    pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count);
                IPagedList<Customer> customersByUsername = new PagedList<Customer>();
                if (_adminSearchSettings.MaxSearchResultsCount - result.Count - customersByEmail.Count > 0)
                    customersByUsername = await _customerService.GetAllCustomers(username: searchTerm,
                        pageSize: _adminSearchSettings.MaxSearchResultsCount
                                  - result.Count - customersByEmail.Count);
                var combined = customersByEmail.Union(customersByUsername).GroupBy(x => x.Email).Select(x => x.First());

                foreach (var customer in combined)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = customer.Email,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Customer/Edit/") + customer.Id,
                        source = _translationService.GetResource("Admin.Customers")
                    }, _adminSearchSettings.CustomersDisplayOrder));
            }

            if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInMenu &&
                foundMenuItems != null && foundMenuItems.Any())
                foreach (var menuItem in foundMenuItems)
                {
                    if (result.Count >= _adminSearchSettings.MaxSearchResultsCount)
                        break;

                    var formatted = _translationService.GetResource("Admin.AdminSearch.Menu") + " > ";
                    if (string.IsNullOrEmpty(menuItem.grandParent))
                        formatted += menuItem.parent;
                    else
                        formatted += menuItem.grandParent + " > " + menuItem.parent;

                    result.Add(new Tuple<object, int>(new
                    {
                        menuItem.title,
                        menuItem.link,
                        source = formatted
                    }, _adminSearchSettings.MenuDisplayOrder));
                }
        }

        if (result.Count < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInOrders)
        {
            int.TryParse(searchTerm, out var orderNumber);
            if (orderNumber > 0)
            {
                var order = await _orderService.GetOrderByNumber(orderNumber);
                if (order != null)
                    result.Add(new Tuple<object, int>(new
                    {
                        title = order.OrderNumber,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Order/Edit/") + order.Id,
                        source = _translationService.GetResource("Admin.Orders")
                    }, _adminSearchSettings.OrdersDisplayOrder));
            }

            var orders = await _orderService.GetOrdersByCode(searchTerm);
            foreach (var order in orders)
                result.Add(new Tuple<object, int>(new
                {
                    title = order.Code,
                    link = Url.Content($"~/{Constants.AreaAdmin}/Order/Edit/") + order.Id,
                    source = _translationService.GetResource("Admin.Orders")
                }, _adminSearchSettings.OrdersDisplayOrder));
        }

        result = result.OrderBy(x => x.Item2).ToList();
        return Json(result.Take(_adminSearchSettings.MaxSearchResultsCount).Select(x => x.Item1).ToList());
    }


    [HttpGet]
    public async Task<IActionResult> Category(string categoryId, DataSourceRequestFilter model)
    {
        var categories = await _categoryService.GetAllCategories(
            parentId: null,
            categoryName: model.GetNameFilterValue(),
            storeId: "",
            pageIndex: 0,
            pageSize: _adminSearchSettings.CategorySizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(categoryId, categories, async category => await _categoryService.GetFormattedBreadCrumb(category));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Collection(string collectionId, DataSourceRequestFilter model)
    {
        var collections = await _collectionService.GetAllCollections(
            collectionName: model.GetNameFilterValue(),
            storeId: "",
            pageIndex: 0,
            pageSize: _adminSearchSettings.CollectionSizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(collectionId, collections, collection => Task.FromResult(collection.Name));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> CustomerGroup(string customerGroupId, DataSourceRequestFilter model)
    {
        var groups = await _groupService.GetAllCustomerGroups(
            model.GetNameFilterValue(),
            pageSize: _adminSearchSettings.CustomerGroupSizeLimit);

        var gridModel = await DataSourceResultHelper.GetSearchResult(customerGroupId, groups, store => Task.FromResult(store.Name));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Stores(string storeId, DataSourceRequestFilter model)
    {
        var stores = await _storeService.GetAllStores();
        var staffStoreId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        if (!string.IsNullOrEmpty(staffStoreId))
            stores = stores.Where(x => x.Id == staffStoreId).ToList();

        if (model.GetNameFilterValue() != null)
            stores = stores.Where(x => x.Name.Contains(model.GetNameFilterValue(), StringComparison.InvariantCultureIgnoreCase)).ToList();

        var gridModel = await DataSourceResultHelper.GetSearchResult(storeId, stores, store => Task.FromResult(store.Name));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Vendor(string vendorId, DataSourceRequestFilter model)
    {
        var vendors = await _vendorService.GetAllVendors(
            model.GetNameFilterValue(),
            pageSize: _adminSearchSettings.VendorSizeLimit, showHidden: true);

        var gridModel = await DataSourceResultHelper.GetSearchResult(vendorId, vendors, vendor => Task.FromResult(vendor.Name));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Brand(string brandId, DataSourceRequestFilter model)
    {
        var brands = await _brandService.GetAllBrands(
            model.GetNameFilterValue(),
            pageSize: _adminSearchSettings.BrandSizeLimit);

        var gridModel = await DataSourceResultHelper.GetSearchResult(brandId, brands, brand => Task.FromResult(brand.Name));
        return Json(gridModel);
    }
}