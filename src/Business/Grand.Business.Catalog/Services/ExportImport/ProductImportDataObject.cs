using Grand.Business.Catalog.Extensions;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.System;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;

namespace Grand.Business.Catalog.Services.ExportImport;

public class ProductImportDataObject : IImportDataObject<ProductDto>
{
    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;
    private readonly ICollectionService _collectionService;
    private readonly IDeliveryDateService _deliveryDateService;
    private readonly ILanguageService _languageService;
    private readonly IMeasureService _measureService;
    private readonly IPictureService _pictureService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IProductCollectionService _productCollectionService;
    private readonly IProductLayoutService _productLayoutService;
    private readonly IProductService _productService;

    private readonly SeoSettings _seoSetting;
    private readonly ISlugService _slugService;
    private readonly ITaxCategoryService _taxService;
    private readonly IWarehouseService _warehouseService;

    public ProductImportDataObject(
        IProductService productService,
        IPictureService pictureService,
        IProductLayoutService productLayoutService,
        IDeliveryDateService deliveryDateService,
        ITaxCategoryService taxService,
        IWarehouseService warehouseService,
        IMeasureService measureService,
        ISlugService slugService,
        ILanguageService languageService,
        ICategoryService categoryService,
        IProductCategoryService productCategoryService,
        IBrandService brandService,
        ICollectionService collectionService,
        IProductCollectionService productCollectionService,
        SeoSettings seoSetting)
    {
        _productService = productService;
        _pictureService = pictureService;
        _productLayoutService = productLayoutService;
        _deliveryDateService = deliveryDateService;
        _taxService = taxService;
        _warehouseService = warehouseService;
        _measureService = measureService;
        _slugService = slugService;
        _languageService = languageService;
        _categoryService = categoryService;
        _productCategoryService = productCategoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _productCollectionService = productCollectionService;
        _seoSetting = seoSetting;
    }

    public async Task Execute(IEnumerable<ProductDto> data)
    {
        foreach (var item in data) await Import(item);
    }

    private async Task Import(ProductDto productDto)
    {
        var product = await _productService.GetProductById(productDto.Id);
        product ??= await _productService.GetProductBySku(productDto.Sku);

        var isNew = product == null;

        if (product == null) product = productDto.MapTo<ProductDto, Product>();
        else productDto.MapTo(product);

        if (!ValidProduct(product)) return;

        if (isNew) await _productService.InsertProduct(product);
        else await _productService.UpdateProduct(product);

        await UpdateProductData(productDto, product, isNew);
    }

    private async Task UpdateProductData(ProductDto productDto, Product product, bool isNew)
    {
        await UpdateProductDataLayout(product);
        await UpdateProductDataDeliveryDate(product);
        await UpdateProductDataTaxCategory(product);
        await UpdateProductDataWarehouse(product);
        await UpdateProductDataUnit(product);
        await UpdateProductDataBrand(product);

        //search engine name
        var sename = product.SeName ?? product.Name;
        sename = await product.ValidateSeName(sename, product.Name, true, _seoSetting, _slugService, _languageService);
        await _slugService.SaveSlug(product, sename, "");
        product.SeName = sename;
        await _productService.UpdateProduct(product);

        product.LowStock = product.MinStockQuantity > 0 && product.MinStockQuantity >= product.StockQuantity;

        if (!string.IsNullOrEmpty(productDto.CategoryIds))
            await PrepareProductCategories(product, productDto.CategoryIds);

        if (!string.IsNullOrEmpty(productDto.CollectionIds))
            await PrepareProductCollections(product, productDto.CollectionIds);

        //pictures
        await PrepareProductPictures(productDto, product, isNew);
    }

    private async Task UpdateProductDataLayout(Product product)
    {
        if (string.IsNullOrEmpty(product.ProductLayoutId))
        {
            product.ProductLayoutId = (await _productLayoutService.GetAllProductLayouts()).FirstOrDefault()?.Id;
        }
        else
        {
            var layout = await _productLayoutService.GetProductLayoutById(product.ProductLayoutId);
            if (layout == null)
                product.ProductLayoutId = (await _productLayoutService.GetAllProductLayouts()).FirstOrDefault()?.Id;
        }
    }

    private async Task UpdateProductDataDeliveryDate(Product product)
    {
        if (string.IsNullOrEmpty(product.DeliveryDateId)) return;
        var deliveryDate = await _deliveryDateService.GetDeliveryDateById(product.DeliveryDateId);
        if (deliveryDate == null)
            product.DeliveryDateId = "";
    }

    private async Task UpdateProductDataTaxCategory(Product product)
    {
        if (string.IsNullOrEmpty(product.TaxCategoryId)) return;
        var taxCategory = await _taxService.GetTaxCategoryById(product.TaxCategoryId);
        if (taxCategory == null)
            product.TaxCategoryId = "";
    }

    private async Task UpdateProductDataWarehouse(Product product)
    {
        if (string.IsNullOrEmpty(product.WarehouseId)) return;
        var warehouse = await _warehouseService.GetWarehouseById(product.WarehouseId);
        if (warehouse == null)
            product.WarehouseId = "";
    }

    private async Task UpdateProductDataUnit(Product product)
    {
        if (string.IsNullOrEmpty(product.UnitId)) return;
        var unit = await _measureService.GetMeasureUnitById(product.UnitId);
        if (unit == null)
            product.UnitId = "";
    }

    private async Task UpdateProductDataBrand(Product product)
    {
        if (string.IsNullOrEmpty(product.BrandId)) return;
        var brand = await _brandService.GetBrandById(product.BrandId);
        if (brand == null)
            product.BrandId = "";
    }

    private async Task PrepareProductCategories(Product product, string categoryIds)
    {
        foreach (var id in categoryIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim()))
        {
            if (product.ProductCategories.FirstOrDefault(x => x.CategoryId == id) != null) continue;
            //ensure that category exists
            var category = await _categoryService.GetCategoryById(id);
            if (category == null) continue;
            var productCategory = new ProductCategory {
                CategoryId = category.Id,
                IsFeaturedProduct = false,
                DisplayOrder = 1
            };
            await _productCategoryService.InsertProductCategory(productCategory, product.Id);
        }
    }

    private async Task PrepareProductCollections(Product product, string collectionIds)
    {
        foreach (var id in collectionIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim()))
        {
            if (product.ProductCollections.FirstOrDefault(x => x.CollectionId == id) != null) continue;
            //ensure that collection exists
            var collection = await _collectionService.GetCollectionById(id);
            if (collection == null) continue;
            var productCollection = new ProductCollection {
                CollectionId = collection.Id,
                IsFeaturedProduct = false,
                DisplayOrder = 1
            };
            await _productCollectionService.InsertProductCollection(productCollection, product.Id);
        }
    }

    private async Task PrepareProductPictures(ProductDto productDto, Product product, bool isNew)
    {
        var picture1 = productDto.Picture1;
        var picture2 = productDto.Picture2;
        var picture3 = productDto.Picture3;

        foreach (var picturePath in new[] { picture1, picture2, picture3 })
        {
            if (string.IsNullOrEmpty(picturePath))
                continue;
            if (!picturePath.ToLower().StartsWith("http".ToLower()))
            {
                var mimeType = MimeTypeExtensions.GetMimeTypeFromFilePath(picturePath);
                var newPictureBinary = await File.ReadAllBytesAsync(picturePath);
                var pictureAlreadyExists = false;
                if (!isNew)
                {
                    //compare with existing product pictures
                    var existingPictures = product.ProductPictures;
                    foreach (var existingPicture in existingPictures)
                    {
                        var pp = await _pictureService.GetPictureById(existingPicture.PictureId);
                        var existingBinary = await _pictureService.LoadPictureBinary(pp);
                        //picture binary after validation (like in database)
                        var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
                        if (!existingBinary.SequenceEqual(validatedPictureBinary) &&
                            !existingBinary.SequenceEqual(newPictureBinary)) continue;
                        //the same picture content
                        pictureAlreadyExists = true;
                        break;
                    }
                }

                if (pictureAlreadyExists) continue;
                var picture = await _pictureService.InsertPicture(newPictureBinary, mimeType,
                    _pictureService.GetPictureSeName(product.Name), "", "", false,
                    Reference.Product, product.Id);
                var productPicture = new ProductPicture {
                    PictureId = picture.Id,
                    DisplayOrder = 1
                };
                await _productService.InsertProductPicture(productPicture, product.Id);
            }
            else
            {
                var fileBinary = await DownloadUrl.DownloadFile(picturePath);
                if (fileBinary == null) continue;
                var mimeType = MimeTypeExtensions.GetMimeTypeFromFilePath(picturePath);
                var picture = await _pictureService.InsertPicture(fileBinary, mimeType,
                    _pictureService.GetPictureSeName(product.Name), "", "", false, Reference.Product, product.Id);
                var productPicture = new ProductPicture {
                    PictureId = picture.Id,
                    DisplayOrder = 1
                };
                await _productService.InsertProductPicture(productPicture, product.Id);
            }
        }
    }

    private bool ValidProduct(Product product)
    {
        return !string.IsNullOrEmpty(product.Name);
    }
}