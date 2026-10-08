using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.System;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Mapper;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

public class ProductImportDataObject : IImportDataObject<ProductDto>, IRowImport<ProductDto>
{
    /// <summary>Most picture downloads one Import call performs; later pictures get a warning instead</summary>
    public const int MaxPictureDownloadsPerBatch = 20;

    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;
    private readonly ICollectionService _collectionService;
    private readonly IDeliveryDateService _deliveryDateService;
    private readonly IMeasureService _measureService;
    private readonly IPictureService _pictureService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IProductCollectionService _productCollectionService;
    private readonly IProductLayoutService _productLayoutService;
    private readonly IProductService _productService;
    private readonly ISlugService _slugService;
    private readonly ITaxCategoryService _taxService;
    private readonly IWarehouseService _warehouseService;
    private readonly ISeNameService _seNameService;
    private readonly SecurityConfig _securityConfig;
    private readonly TaxSettings _taxSettings;
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly ILogger<ProductImportDataObject> _logger;
    private readonly Func<string, Task<DownloadedImage>> _downloadImage;

    /// <param name="downloadImage">Test seam; the default downloads through <see cref="DownloadUrl.DownloadImage" /></param>
    public ProductImportDataObject(
        IProductService productService,
        IPictureService pictureService,
        IProductLayoutService productLayoutService,
        IDeliveryDateService deliveryDateService,
        ITaxCategoryService taxService,
        IWarehouseService warehouseService,
        IMeasureService measureService,
        ISlugService slugService,
        ICategoryService categoryService,
        IProductCategoryService productCategoryService,
        IBrandService brandService,
        ICollectionService collectionService,
        IProductCollectionService productCollectionService,
        ISeNameService seNameService,
        SecurityConfig securityConfig,
        TaxSettings taxSettings,
        ImportHtmlGuard htmlGuard,
        ILogger<ProductImportDataObject> logger,
        Func<string, Task<DownloadedImage>> downloadImage = null)
    {
        _productService = productService;
        _pictureService = pictureService;
        _productLayoutService = productLayoutService;
        _deliveryDateService = deliveryDateService;
        _taxService = taxService;
        _warehouseService = warehouseService;
        _measureService = measureService;
        _slugService = slugService;
        _categoryService = categoryService;
        _productCategoryService = productCategoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _productCollectionService = productCollectionService;
        _seNameService = seNameService;
        _securityConfig = securityConfig;
        _taxSettings = taxSettings;
        _htmlGuard = htmlGuard;
        _logger = logger;
        //only http(s) URLs to public or explicitly allowed hosts are downloaded - a local path would let the
        //import read any file on the server
        _downloadImage = downloadImage ??
                         (url => DownloadUrl.DownloadImage(url, _securityConfig.PictureImportAllowedPrivateHosts));
    }

    public async Task Execute(IEnumerable<ProductDto> data)
    {
        var result = await Import(data.ToList(), false, ImportMode.Panel);
        ImportRows.LogRejected(_logger, "product", result);
    }

    /// <summary>Row API: an unknown Id is rejected, a row without Id is matched by Sku</summary>
    public Task<ImportBatchResult> Import(IReadOnlyList<ProductDto> rows, bool dryRun,
        CancellationToken cancellationToken = default)
    {
        return Import(rows, dryRun, ImportMode.Row, cancellationToken);
    }

    /// <summary>
    ///     Row: unknown Id rejected, Sku matching only when no Id is given. Panel (XLSX): by Id, then by Sku; an unknown Id
    ///     creates the product with that Id.
    /// </summary>
    private enum ImportMode
    {
        Row,
        Panel
    }

    private sealed class PictureBudget
    {
        public int Downloads;
    }

    private async Task<ImportBatchResult> Import(IReadOnlyList<ProductDto> rows, bool dryRun, ImportMode mode,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImportRowResult>(rows.Count);
        //the cap protects the row API; the panel's spreadsheet import downloads every picture
        var budget = mode == ImportMode.Row ? new PictureBudget() : null;
        for (var i = 0; i < rows.Count; i++)
        {
            //a batch that runs out of time stops at a row boundary: what was saved is reported, the rest is not touched
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(ImportRowResult.NotProcessed(i + 1, rows[i].Id, Key(rows[i])));
                continue;
            }

            results.Add(await ImportRow(i + 1, rows[i], dryRun, mode, budget));
        }

        return new ImportBatchResult(dryRun, results);
    }

    private async Task<ImportRowResult> ImportRow(int row, ProductDto dto, bool dryRun, ImportMode mode,
        PictureBudget budget)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);

        Product existing = null;
        if (!string.IsNullOrEmpty(dto.Id))
            existing = await _productService.GetProductById(dto.Id);
        if (existing == null && (string.IsNullOrEmpty(dto.Id) || mode == ImportMode.Panel))
            existing = await _productService.GetProductBySku(dto.Sku);

        if (!string.IsNullOrEmpty(dto.Id) && existing == null && mode == ImportMode.Row)
            errors.Add($"Id '{dto.Id}' was not found.");
        else if (dto.Name == "")
            errors.Add("Name cannot be empty.");
        else if (string.IsNullOrEmpty(dto.Name ?? existing?.Name))
            errors.Add("Name is required.");

        errors.AddRange(_htmlGuard.RichTextErrors((nameof(dto.ShortDescription), dto.ShortDescription),
            (nameof(dto.FullDescription), dto.FullDescription)));
        errors.AddRange(_htmlGuard.PlainTextErrors((nameof(dto.Name), dto.Name), (nameof(dto.SeName), dto.SeName),
            (nameof(dto.MetaKeywords), dto.MetaKeywords), (nameof(dto.MetaDescription), dto.MetaDescription),
            (nameof(dto.MetaTitle), dto.MetaTitle), (nameof(dto.Sku), dto.Sku)));

        await AddReferenceWarnings(dto, warnings);
        //the Sku is what a later call matches on; without one the row cannot be found again
        if (mode == ImportMode.Row && existing == null && string.IsNullOrEmpty(dto.Sku))
            warnings.Add("Row has no Sku; sending it again creates a duplicate.");

        if (errors.Count > 0)
            return new ImportRowResult(row, ImportRowStatus.Rejected, existing?.Id ?? "", key, errors, warnings);

        if (dryRun)
            return new ImportRowResult(row, existing == null ? ImportRowStatus.Created : ImportRowStatus.Updated,
                existing?.Id ?? dto.Id ?? "", key, errors, warnings);

        //a product found by Sku keeps its own id (Panel: the old code mapped the foreign Id over the found product)
        if (existing != null) dto.Id = existing.Id;
        var product = existing;
        var isNew = product == null;
        if (product == null)
        {
            product = dto.MapTo<ProductDto, Product>();
            if (mode == ImportMode.Row) ApplyCreateDefaults(dto, product);
        }
        else
        {
            dto.MapTo(product);
        }

        if (!ValidProduct(product))
        {
            errors.Add("Name is required.");
            return new ImportRowResult(row, ImportRowStatus.Rejected, "", key, errors, warnings);
        }

        if (isNew) await _productService.InsertProduct(product);
        else await _productService.UpdateProduct(product);

        await UpdateProductData(dto, product, isNew, budget, warnings);
        return new ImportRowResult(row, isNew ? ImportRowStatus.Created : ImportRowStatus.Updated, product.Id, key,
            errors, warnings);
    }

    /// <summary>
    ///     Row create: the defaults the admin panel puts on a new product, for every field the row leaves out - without
    ///     them a product cannot be bought (maximum quantity 0) nor found (not visible individually). Published is not
    ///     defaulted: a row-created product stays unpublished unless the row says otherwise.
    /// </summary>
    private void ApplyCreateDefaults(ProductDto dto, Product product)
    {
        if (!dto.VisibleIndividually.HasValue) product.VisibleIndividually = true;
        if (!dto.OrderMinimumQuantity.HasValue) product.OrderMinimumQuantity = 1;
        if (!dto.OrderMaximumQuantity.HasValue) product.OrderMaximumQuantity = 10000;
        if (!dto.NotifyAdminForQuantityBelow.HasValue) product.NotifyAdminForQuantityBelow = 1;
        if (!dto.IsShipEnabled.HasValue) product.IsShipEnabled = true;
        if (!dto.AllowCustomerReviews.HasValue) product.AllowCustomerReviews = true;
        if (dto.TaxCategoryId == null) product.TaxCategoryId = _taxSettings.DefaultTaxCategoryId;
    }

    private static string Key(ProductDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ? dto.Name : !string.IsNullOrEmpty(dto.Sku) ? dto.Sku : dto.Id ?? "";
    }

    /// <summary>Ids that do not exist are skipped (categories, collections) or cleared (brand); say so</summary>
    private async Task AddReferenceWarnings(ProductDto dto, List<string> warnings)
    {
        if (!string.IsNullOrEmpty(dto.BrandId) && await _brandService.GetBrandById(dto.BrandId) == null)
            warnings.Add($"BrandId '{dto.BrandId}' was not found and was cleared.");

        foreach (var id in SplitIds(dto.CategoryIds))
            if (await _categoryService.GetCategoryById(id) == null)
                warnings.Add($"Category id '{id}' was not found and was skipped.");

        foreach (var id in SplitIds(dto.CollectionIds))
            if (await _collectionService.GetCollectionById(id) == null)
                warnings.Add($"Collection id '{id}' was not found and was skipped.");
    }

    private static IEnumerable<string> SplitIds(string ids)
    {
        return string.IsNullOrEmpty(ids)
            ? []
            : ids.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
    }

    private async Task UpdateProductData(ProductDto productDto, Product product, bool isNew,
        PictureBudget budget, List<string> warnings)
    {
        await UpdateProductDataLayout(product);
        await UpdateProductDataDeliveryDate(product);
        await UpdateProductDataTaxCategory(product);
        await UpdateProductDataWarehouse(product);
        await UpdateProductDataUnit(product);
        await UpdateProductDataBrand(product);

        //search engine name
        var seName = product.SeName ?? product.Name;
        seName = await _seNameService.ValidateSeName(product, seName, product.Name, true);
        await _slugService.SaveSlug(product, seName, "");
        product.SeName = seName;
        await _productService.UpdateProduct(product);

        product.LowStock = product.MinStockQuantity > 0 && product.MinStockQuantity >= product.StockQuantity;

        if (!string.IsNullOrEmpty(productDto.CategoryIds))
            await PrepareProductCategories(product, productDto.CategoryIds);

        if (!string.IsNullOrEmpty(productDto.CollectionIds))
            await PrepareProductCollections(product, productDto.CollectionIds);

        //pictures
        await PrepareProductPictures(productDto, product, isNew, budget, warnings);
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
        foreach (var id in categoryIds.Split([';'], StringSplitOptions.RemoveEmptyEntries)
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
        foreach (var id in collectionIds.Split([';'], StringSplitOptions.RemoveEmptyEntries)
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

    private async Task PrepareProductPictures(ProductDto productDto, Product product, bool isNew,
        PictureBudget budget, List<string> warnings)
    {
        //PictureUrls, when sent, replaces Picture1..3
        IEnumerable<string> urls = productDto.PictureUrls is { Count: > 0 }
            ? productDto.PictureUrls
            : new[] { productDto.Picture1, productDto.Picture2, productDto.Picture3 };

        foreach (var pictureUrl in urls)
        {
            if (string.IsNullOrEmpty(pictureUrl))
                continue;

            if (budget is { Downloads: >= MaxPictureDownloadsPerBatch })
            {
                warnings.Add($"Picture '{pictureUrl}' was not downloaded: picture limit for one batch reached.");
                continue;
            }

            if (budget != null) budget.Downloads++;
            var image = await _downloadImage(pictureUrl);
            if (image == null)
            {
                warnings.Add($"Picture '{pictureUrl}' could not be downloaded.");
                continue;
            }

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
                    var validatedPictureBinary = _pictureService.ValidatePicture(image.Binary, image.MimeType);
                    if (!existingBinary.SequenceEqual(validatedPictureBinary) &&
                        !existingBinary.SequenceEqual(image.Binary)) continue;
                    //the same picture content
                    pictureAlreadyExists = true;
                    break;
                }
            }

            if (pictureAlreadyExists) continue;
            var picture = await _pictureService.InsertPicture(image.Binary, image.MimeType,
                _pictureService.GetPictureSeName(product.Name), "", "", false,
                Reference.Product, product.Id);
            var productPicture = new ProductPicture {
                PictureId = picture.Id,
                DisplayOrder = 1
            };
            await _productService.InsertProductPicture(productPicture, product.Id);
        }
    }

    private static bool ValidProduct(Product product)
    {
        return !string.IsNullOrEmpty(product.Name);
    }
}