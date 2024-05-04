using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Admin;
using Grand.Domain.Catalog;
using Grand.Web.Common.DataSource;
using Grand.Web.Vendor.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

public class SearchController : BaseVendorController
{
    private readonly AdminSearchSettings _adminSearchSettings;
    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;
    private readonly ICollectionService _collectionService;

    public SearchController(ICategoryService categoryService,
        IBrandService brandService, ICollectionService collectionService,
        AdminSearchSettings adminSearchSettings)
    {
        _categoryService = categoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _adminSearchSettings = adminSearchSettings;
    }

    [HttpGet]
    public async Task<IActionResult> Category(string categoryId)
    {
        var value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

        async Task<IList<SearchModel>> PrepareModel(IEnumerable<Category> categories)
        {
            var model = new List<SearchModel>();
            if (!string.IsNullOrEmpty(categoryId))
            {
                var currentCategory = await _categoryService.GetCategoryById(categoryId);
                if (currentCategory != null)
                    model.Add(new SearchModel {
                        Id = currentCategory.Id,
                        Name = await _categoryService.GetFormattedBreadCrumb(currentCategory)
                    });
            }

            foreach (var item in categories)
                if (item.Id != categoryId)
                    model.Add(new SearchModel {
                        Id = item.Id,
                        Name = await _categoryService.GetFormattedBreadCrumb(item)
                    });
            return model;
        }

        var categories = await _categoryService.GetAllCategories(
            categoryName: value,
            pageSize: _adminSearchSettings.CategorySizeLimit);
        var gridModel = new DataSourceResult {
            Data = await PrepareModel(categories)
        };
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Collection(string collectionId)
    {
        var value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

        async Task<IList<SearchModel>> PrepareModel(IEnumerable<Collection> collections)
        {
            var model = new List<SearchModel>();
            if (!string.IsNullOrEmpty(collectionId))
            {
                var currentCollection = await _collectionService.GetCollectionById(collectionId);
                if (currentCollection != null)
                    model.Add(new SearchModel {
                        Id = currentCollection.Id,
                        Name = currentCollection.Name
                    });
            }

            model.AddRange(from item in collections
                where item.Id != collectionId
                select new SearchModel { Id = item.Id, Name = item.Name });
            return model;
        }

        var collections = await _collectionService.GetAllCollections(
            value,
            pageSize: _adminSearchSettings.CollectionSizeLimit);

        var gridModel = new DataSourceResult {
            Data = await PrepareModel(collections)
        };
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Brand(string brandId)
    {
        var value = HttpContext.Request.Query["filter[filters][0][value]"].ToString();

        async Task<IList<SearchModel>> PrepareModel(IEnumerable<Brand> brands)
        {
            var model = new List<SearchModel>();
            if (!string.IsNullOrEmpty(brandId))
            {
                var currentBrand = await _brandService.GetBrandById(brandId);
                if (currentBrand != null)
                    model.Add(new SearchModel {
                        Id = currentBrand.Id,
                        Name = currentBrand.Name
                    });
            }

            model.AddRange(from item in brands
                where item.Id != brandId
                select new SearchModel { Id = item.Id, Name = item.Name });
            return model;
        }

        var brands = await _brandService.GetAllBrands(
            value,
            pageSize: _adminSearchSettings.BrandSizeLimit);
        var gridModel = new DataSourceResult {
            Data = await PrepareModel(brands)
        };
        return Json(gridModel);
    }
}