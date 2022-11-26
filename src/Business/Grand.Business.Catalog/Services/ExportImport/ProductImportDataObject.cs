using Grand.Business.Catalog.Services.ExportImport.Dto;
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
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Business.Catalog.Services.ExportImport
{
    public class ProductImportDataObject : IImportDataObject<ProductDto>
    {
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly IProductLayoutService _productLayoutService;
        private readonly IDeliveryDateService _deliveryDateService;
        private readonly ITaxCategoryService _taxService;
        private readonly IWarehouseService _warehouseService;
        private readonly IMeasureService _measureService;
        private readonly ICategoryService _categoryService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IBrandService _brandService;
        private readonly ICollectionService _collectionService;
        private readonly IProductCollectionService _productCollectionService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;

        private readonly SeoSettings _seoSetting;

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
            foreach (var item in data)
            {
                await Import(item);
            }
        }

        protected async Task Import(ProductDto productDto)
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

        protected async Task<Product> UpdateProductData(ProductDto productDto, Product product, bool isNew)
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

            if (!string.IsNullOrEmpty(productDto.CategoryIds)) await PrepareProductCategories(product, productDto.CategoryIds);

            if (!string.IsNullOrEmpty(productDto.CollectionIds)) await PrepareProductCollections(product, productDto.CollectionIds);

            //pictures
            await PrepareProductPictures(productDto, product, isNew);

            return product;
        }
        protected async Task<Product> UpdateProductDataLayout(Product product)
        {
            if (string.IsNullOrEmpty(product.ProductLayoutId))
                product.ProductLayoutId = (await _productLayoutService.GetAllProductLayouts()).FirstOrDefault().Id;
            else
            {
                var layout = await _productLayoutService.GetProductLayoutById(product.ProductLayoutId);
                if (layout == null)
                    product.ProductLayoutId = (await _productLayoutService.GetAllProductLayouts()).FirstOrDefault().Id;
            }
            return product;
        }
        protected async Task<Product> UpdateProductDataDeliveryDate(Product product)
        {
            if (!string.IsNullOrEmpty(product.DeliveryDateId))
            {
                var deliveryDate = await _deliveryDateService.GetDeliveryDateById(product.DeliveryDateId);
                if (deliveryDate == null)
                    product.DeliveryDateId = "";
            }
            return product;
        }
        protected async Task<Product> UpdateProductDataTaxCategory(Product product)
        {
            if (!string.IsNullOrEmpty(product.TaxCategoryId))
            {
                var taxCategory = await _taxService.GetTaxCategoryById(product.TaxCategoryId);
                if (taxCategory == null)
                    product.TaxCategoryId = "";
            }
            return product;
        }
        protected async Task<Product> UpdateProductDataWarehouse(Product product)
        {
            if (!string.IsNullOrEmpty(product.WarehouseId))
            {
                var warehouse = await _warehouseService.GetWarehouseById(product.WarehouseId);
                if (warehouse == null)
                    product.WarehouseId = "";
            }
            return product;
        }
        protected async Task<Product> UpdateProductDataUnit(Product product)
        {
            if (!string.IsNullOrEmpty(product.UnitId))
            { 
                var unit = await _measureService.GetMeasureUnitById(product.UnitId);
                if (unit == null)
                    product.UnitId = "";
            }
            return product;
        }
        protected async Task<Product> UpdateProductDataBrand(Product product)
        {
            if (!string.IsNullOrEmpty(product.BrandId))
            { 
                var brand = await _brandService.GetBrandById(product.BrandId);
                if (brand == null)
                    product.BrandId = "";
            }
            return product;
        }
        protected virtual async Task PrepareProductCategories(Product product, string categoryIds)
        {
            foreach (var id in categoryIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            {
                if (product.ProductCategories.FirstOrDefault(x => x.CategoryId == id) == null)
                {
                    //ensure that category exists
                    var category = await _categoryService.GetCategoryById(id);
                    if (category != null)
                    {
                        var productCategory = new ProductCategory {
                            CategoryId = category.Id,
                            IsFeaturedProduct = false,
                            DisplayOrder = 1
                        };
                        await _productCategoryService.InsertProductCategory(productCategory, product.Id);
                    }
                }
            }
        }

        protected virtual async Task PrepareProductCollections(Product product, string collectionIds)
        {
            foreach (var id in collectionIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            {
                if (product.ProductCollections.FirstOrDefault(x => x.CollectionId == id) == null)
                {
                    //ensure that collection exists
                    var collection = await _collectionService.GetCollectionById(id);
                    if (collection != null)
                    {
                        var productCollection = new ProductCollection {
                            CollectionId = collection.Id,
                            IsFeaturedProduct = false,
                            DisplayOrder = 1
                        };
                        await _productCollectionService.InsertProductCollection(productCollection, product.Id);
                    }
                }
            }
        }

        protected virtual async Task PrepareProductPictures(ProductDto productDto, Product product, bool isNew)
        {
            var picture1 = productDto.Picture1;
            var picture2 = productDto.Picture2;
            var picture3 = productDto.Picture3;

            foreach (var picturePath in new[] { picture1, picture2, picture3 })
            {
                if (string.IsNullOrEmpty(picturePath))
                    continue;
                if (!picturePath.ToLower().StartsWith(("http".ToLower())))
                {
                    var mimeType = GetMimeTypeFromFilePath(picturePath);
                    var newPictureBinary = File.ReadAllBytes(picturePath);
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
                            if (existingBinary.SequenceEqual(validatedPictureBinary) || existingBinary.SequenceEqual(newPictureBinary))
                            {
                                //the same picture content
                                pictureAlreadyExists = true;
                                break;
                            }
                        }
                    }

                    if (!pictureAlreadyExists)
                    {
                        var picture = await _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(product.Name), "", "", false,
                            Domain.Common.Reference.Product, product.Id);
                        var productPicture = new ProductPicture {
                            PictureId = picture.Id,
                            DisplayOrder = 1,
                        };
                        await _productService.InsertProductPicture(productPicture, product.Id);
                    }
                }
                else
                {
                    byte[] fileBinary = await DownloadUrl.DownloadFile(picturePath);
                    if (fileBinary != null)
                    {
                        var mimeType = GetMimeTypeFromFilePath(picturePath);
                        var picture = await _pictureService.InsertPicture(fileBinary, mimeType, _pictureService.GetPictureSeName(product.Name), "", "", false, Domain.Common.Reference.Product, product.Id);
                        var productPicture = new ProductPicture {
                            PictureId = picture.Id,
                            DisplayOrder = 1,
                        };
                        await _productService.InsertProductPicture(productPicture, product.Id);
                    }
                }
            }

        }


        protected virtual bool ValidProduct(Product product)
        {
            if (string.IsNullOrEmpty(product.Name))
                return false;

            return true;
        }

        /// <summary>
        /// Creates or loads the image
        /// </summary>
        /// <param name="picturePath">The path to the image file</param>
        /// <param name="name">The name of the object</param>
        /// <param name="picId">Image identifier, may be null</param>
        /// <returns>The image or null if the image has not changed</returns>
        protected virtual async Task<Picture> LoadPicture(string picturePath, string name, string picId = "")
        {
            if (string.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
                return null;

            var mimeType = GetMimeTypeFromFilePath(picturePath);
            var newPictureBinary = File.ReadAllBytes(picturePath);
            var pictureAlreadyExists = false;
            if (!string.IsNullOrEmpty(picId))
            {
                //compare with existing product pictures
                var existingPicture = await _pictureService.GetPictureById(picId);

                var existingBinary = await _pictureService.LoadPictureBinary(existingPicture);
                //picture binary after validation (like in database)
                var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
                if (existingBinary.SequenceEqual(validatedPictureBinary) ||
                    existingBinary.SequenceEqual(newPictureBinary))
                {
                    pictureAlreadyExists = true;
                }
            }

            if (pictureAlreadyExists) return null;

            var newPicture = await _pictureService.InsertPicture(newPictureBinary, mimeType,
                _pictureService.GetPictureSeName(name));
            return newPicture;
        }
        protected virtual string GetMimeTypeFromFilePath(string filePath)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(filePath, out string mimeType);
            //set to jpeg in case mime type cannot be found
            if (mimeType == null)
                mimeType = "image/jpeg";
            return mimeType;
        }
    }
}
