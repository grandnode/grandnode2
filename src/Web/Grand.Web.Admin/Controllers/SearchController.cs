using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure;
using Grand.Domain;
using Grand.Domain.AdminSearch;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Web.Common.DataSource;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Common;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Business.Catalog.Interfaces.Brands;

namespace Grand.Web.Admin.Controllers
{
    public class SearchController : BaseAdminController
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ICollectionService _collectionService;
        private readonly IPageService _pageService;
        private readonly INewsService _newsService;
        private readonly IBlogService _blogService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly AdminSearchSettings _adminSearchSettings;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly IBrandService _brandService;

        public SearchController(IProductService productService, ICategoryService categoryService,
            IBrandService brandService, ICollectionService collectionService,
            IPageService pageService, INewsService newsService, IBlogService blogService, ICustomerService customerService, IOrderService orderService,
            AdminSearchSettings adminSearchSettings, ITranslationService translationService, IWorkContext workContext, IGroupService groupService,
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
            _workContext = workContext;
            _groupService = groupService;
            _storeService = storeService;
            _vendorService = vendorService;
        }

        [HttpPost]
        public async Task<IActionResult> Index(string searchTerm, FoundMenuItem[] foundMenuItems)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Json("error");

            if (!await _groupService.IsAdmin(_workContext.CurrentCustomer))
            {
                return Json("Access Denied");
            }
            //object = actual result, int = display order for sorting
            List<Tuple<object, int>> result = new List<Tuple<object, int>>();

            if (searchTerm.Length >= _adminSearchSettings.MinSearchTermLength)
            {
                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInProducts)
                {
                    var products = (await _productService.SearchProducts(keywords: searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count(), showHidden: true)).products;
                    foreach (var product in products)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = product.Name,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Product/Edit/") + product.Id,
                            source = _translationService.GetResource("Admin.Catalog.Products")
                        }, _adminSearchSettings.ProductsDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInCategories)
                {
                    var categories = await _categoryService.GetAllCategories(categoryName: searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count(), showHidden:
                        await _groupService.IsAdmin(_workContext.CurrentCustomer));
                    foreach (var category in categories)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = category.Name,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Category/Edit/") + category.Id,
                            source = _translationService.GetResource("Admin.Catalog.Categories")
                        }, _adminSearchSettings.CategoriesDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInCollections)
                {
                    var collections = await _collectionService.GetAllCollections(searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count(), showHidden: true);
                    foreach (var collection in collections)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = collection.Name,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Collection/Edit/") + collection.Id,
                            source = _translationService.GetResource("Admin.Catalog.Collections")
                        }, _adminSearchSettings.CollectionsDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInPages)
                {
                    var pages = (await _pageService.GetAllPages("")).Where(x => (x.SystemName != null && x.SystemName.ToLower().Contains(searchTerm.ToLower())) ||
                    (x.Title != null && x.Title.ToLower().Contains(searchTerm.ToLower())));
                    foreach (var page in pages)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = page.SystemName,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Page/Edit/") + page.Id,
                            source = _translationService.GetResource("Admin.Content.Pages")
                        }, _adminSearchSettings.PagesDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInNews)
                {
                    var news = await _newsService.GetAllNews(newsTitle: searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count(), showHidden: true);
                    foreach (var signleNews in news)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = signleNews.Title,
                            link = Url.Content($"~/{Constants.AreaAdmin}/News/Edit/") + signleNews.Id,
                            source = _translationService.GetResource("Admin.Content.News")
                        }, _adminSearchSettings.NewsDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInBlogs)
                {
                    var blogPosts = await _blogService.GetAllBlogPosts(blogPostName: searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count(), showHidden: true);
                    foreach (var blogPost in blogPosts)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = blogPost.Title,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Blog/Edit/") + blogPost.Id,
                            source = _translationService.GetResource("Admin.Content.Blog")
                        }, _adminSearchSettings.BlogsDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInCustomers)
                {
                    var customersByEmail = await _customerService.GetAllCustomers(email: searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount - result.Count());
                    IPagedList<Customer> customersByUsername = new PagedList<Customer>();
                    if (_adminSearchSettings.MaxSearchResultsCount - result.Count() - customersByEmail.Count() > 0)
                    {
                        customersByUsername = await _customerService.GetAllCustomers(username: searchTerm, pageSize: _adminSearchSettings.MaxSearchResultsCount
                            - result.Count() - customersByEmail.Count());
                    }
                    var combined = customersByEmail.Union(customersByUsername).GroupBy(x => x.Email).Select(x => x.First());

                    foreach (var customer in combined)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = customer.Email,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Customer/Edit/") + customer.Id,
                            source = _translationService.GetResource("Admin.Customers")
                        }, _adminSearchSettings.CustomersDisplayOrder));
                    }
                }

                if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInMenu && foundMenuItems != null && foundMenuItems.Any())
                {
                    foreach (var menuItem in foundMenuItems)
                    {
                        if (result.Count() >= _adminSearchSettings.MaxSearchResultsCount)
                            break;

                        string formatted = _translationService.GetResource("Admin.AdminSearch.Menu") + " > ";
                        if (string.IsNullOrEmpty(menuItem.grandParent))
                        {
                            formatted += menuItem.parent;
                        }
                        else
                        {
                            formatted += menuItem.grandParent + " > " + menuItem.parent;
                        }

                        result.Add(new Tuple<object, int>(new
                        {
                            title = menuItem.title,
                            link = menuItem.link,
                            source = formatted
                        }, _adminSearchSettings.MenuDisplayOrder));
                    }
                }
            }

            if (result.Count() < _adminSearchSettings.MaxSearchResultsCount && _adminSearchSettings.SearchInOrders)
            {
                int.TryParse(searchTerm, out int orderNumber);
                if (orderNumber > 0)
                {
                    var order = await _orderService.GetOrderByNumber(orderNumber);
                    if (order != null)
                    {
                        result.Add(new Tuple<object, int>(new
                        {
                            title = order.OrderNumber,
                            link = Url.Content($"~/{Constants.AreaAdmin}/Order/Edit/") + order.Id,
                            source = _translationService.GetResource("Admin.Orders")
                        }, _adminSearchSettings.OrdersDisplayOrder));
                    }
                }
                var orders = await _orderService.GetOrdersByCode(searchTerm);
                foreach (var order in orders)
                {
                    result.Add(new Tuple<object, int>(new
                    {
                        title = order.Code,
                        link = Url.Content($"~/{Constants.AreaAdmin}/Order/Edit/") + order.Id,
                        source = _translationService.GetResource("Admin.Orders")
                    }, _adminSearchSettings.OrdersDisplayOrder));
                }

            }

            result = result.OrderBy(x => x.Item2).ToList();
            return Json(result.Take(_adminSearchSettings.MaxSearchResultsCount).Select(x => x.Item1).ToList());
        }


        [HttpGet]
        public async Task<IActionResult> Category(string categoryId)
        {
            string value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

            async Task<IList<SearchModel>> PrepareModel(IList<Category> categories)
            {
                var model = new List<SearchModel>();
                if (!string.IsNullOrEmpty(categoryId))
                {
                    var currentCategory = await _categoryService.GetCategoryById(categoryId);
                    if (currentCategory != null)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = currentCategory.Id,
                            Name = await _categoryService.GetFormattedBreadCrumb(currentCategory),
                        });
                    }
                }
                foreach (var item in categories)
                {
                    if (item.Id != categoryId)
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = await _categoryService.GetFormattedBreadCrumb(item),
                        });
                }
                return model;
            }
            var storeId = _workContext.CurrentCustomer.StaffStoreId;

            var categories = (await _categoryService.GetAllCategories(
                   storeId: storeId,
                   categoryName: value,
                   pageSize: _adminSearchSettings.CategorySizeLimit));
            var gridModel = new DataSourceResult
            {
                Data = await PrepareModel(categories),
            };
            return Json(gridModel);
        }

        [HttpGet]
        public async Task<IActionResult> Collection(string collectionId)
        {
            string value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

            async Task<IList<SearchModel>> PrepareModel(IList<Collection> collections)
            {
                var model = new List<SearchModel>();
                if (!string.IsNullOrEmpty(collectionId))
                {
                    var currentCollection = await _collectionService.GetCollectionById(collectionId);
                    if (currentCollection != null)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = currentCollection.Id,
                            Name = currentCollection.Name,
                        });
                    }
                }
                foreach (var item in collections)
                {
                    if (item.Id != collectionId)
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = item.Name,
                        });
                }
                return model;
            }

            var storeId = _workContext.CurrentCustomer.StaffStoreId;

            var collections = (await _collectionService.GetAllCollections(
                    storeId: storeId,
                    collectionName: value,
                    pageSize: _adminSearchSettings.CollectionSizeLimit));

            var gridModel = new DataSourceResult
            {
                Data = await PrepareModel(collections),
            };
            return Json(gridModel);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerGroup(string[] customerGroups)
        {
            string value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

            async Task<IList<SearchModel>> PrepareModel(IList<CustomerGroup> groups)
            {
                var model = new List<SearchModel>();
                if (customerGroups != null && customerGroups.Any())
                {
                    var currentGroups = await _groupService.GetAllByIds(customerGroups);
                    foreach (var item in currentGroups)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = item.Name,

                        });
                    }
                }
                foreach (var item in groups)
                {
                    if (model.FirstOrDefault(x => x.Id == item.Id) == null)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = item.Name,
                        });
                    }
                }
                return model;
            }

            var groups = (await _groupService.GetAllCustomerGroups(
                     name: value,
                     pageSize: _adminSearchSettings.CustomerGroupSizeLimit));

            var gridModel = new DataSourceResult
            {
                Data = await PrepareModel(groups),
            };
            return Json(gridModel);
        }

        [HttpGet]
        public async Task<IActionResult> Stores(string[] stores)
        {
            string value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

            async Task<IList<SearchModel>> PrepareModel(IList<Store> groups)
            {
                var model = new List<SearchModel>();
                if (stores != null && stores.Any())
                {
                    foreach (var item in stores)
                    {
                        var currentStore = await _storeService.GetStoreById(item);
                        if (currentStore != null)
                            model.Add(new SearchModel()
                            {
                                Id = currentStore.Id,
                                Name = currentStore.Name,
                            });

                    }
                }
                foreach (var item in groups)
                {
                    if (model.FirstOrDefault(x => x.Id == item.Id) == null)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = item.Name,
                        });
                    }
                }
                return model;
            }

            var groups = await _storeService.GetAllStores();
            var storeId = _workContext.CurrentCustomer.StaffStoreId;
            if (!string.IsNullOrEmpty(storeId))
                groups = groups.Where(x => x.Id == storeId).ToList();

            var gridModel = new DataSourceResult
            {
                Data = await PrepareModel(groups),
            };
            return Json(gridModel);
        }

        [HttpGet]
        public async Task<IActionResult> Vendor(string vendorId)
        {
            string value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

            async Task<IList<SearchModel>> PrepareModel(IList<Vendor> vendors)
            {
                var model = new List<SearchModel>();
                if (!string.IsNullOrEmpty(vendorId))
                {
                    var currentVendor = await _vendorService.GetVendorById(vendorId);
                    if (currentVendor != null)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = currentVendor.Id,
                            Name = currentVendor.Name,
                        });
                    }
                }
                foreach (var item in vendors)
                {
                    if (item.Id != vendorId)
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = item.Name,
                        });
                }
                return model;
            }
            var vendors = (await _vendorService.GetAllVendors(
                   name: value,
                   pageSize: _adminSearchSettings.VendorSizeLimit, showHidden: true));
            var gridModel = new DataSourceResult
            {
                Data = await PrepareModel(vendors),
            };
            return Json(gridModel);
        }
        [HttpGet]
        public async Task<IActionResult> Brand(string brandId)
        {
            string value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

            async Task<IList<SearchModel>> PrepareModel(IList<Brand> brands)
            {
                var model = new List<SearchModel>();
                if (!string.IsNullOrEmpty(brandId))
                {
                    var currentBrand = await _brandService.GetBrandById(brandId);
                    if (currentBrand != null)
                    {
                        model.Add(new SearchModel()
                        {
                            Id = currentBrand.Id,
                            Name = currentBrand.Name,
                        });
                    }
                }
                foreach (var item in brands)
                {
                    if (item.Id != brandId)
                        model.Add(new SearchModel()
                        {
                            Id = item.Id,
                            Name = item.Name,
                        });
                }
                return model;
            }
            var brands = (await _brandService.GetAllBrands(
                   brandName: value,
                   pageSize: _adminSearchSettings.BrandSizeLimit));
            var gridModel = new DataSourceResult
            {
                Data = await PrepareModel(brands),
            };
            return Json(gridModel);
        }
    }
}