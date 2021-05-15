using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Business.System.Utilities;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Media;
using Grand.Domain.Messages;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.SharedKernel;
using Microsoft.AspNetCore.StaticFiles;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.Catalog.Interfaces.Brands;

namespace Grand.Business.System.Services.ExportImport
{
    /// <summary>
    /// Import manager
    /// </summary>
    public partial class ImportManager : IImportManager
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IBrandService _brandService;
        private readonly ICollectionService _collectionService;
        private readonly IProductCollectionService _productCollectionService;
        private readonly IPictureService _pictureService;
        private readonly ISlugService _slugService;
        private readonly IWorkContext _workContext;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly INewsletterCategoryService _newsletterCategoryService;
        private readonly ICountryService _countryService;
        private readonly IVendorService _vendorService;
        private readonly ICategoryLayoutService _categoryLayoutService;
        private readonly ICollectionLayoutService _collectionLayoutService;
        private readonly IBrandLayoutService _brandLayoutService;
        private readonly IProductLayoutService _productLayoutService;
        private readonly IDownloadService _downloadService;
        private readonly IWarehouseService _warehouseService;
        private readonly IDeliveryDateService _deliveryDateService;
        private readonly ITaxCategoryService _taxService;
        private readonly IMeasureService _measureService;
        private readonly ILanguageService _languageService;
        private readonly SeoSettings _seoSetting;

        #endregion

        #region Ctor

        public ImportManager(IProductService productService,
            ICategoryService categoryService,
            IProductCategoryService productCategoryService,
            IBrandService brandService,
            ICollectionService collectionService,
            IProductCollectionService productCollectionService,
            IPictureService pictureService,
            ISlugService slugService,
            IWorkContext workContext,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            INewsletterCategoryService newsletterCategoryService,
            ICountryService countryService,
            IVendorService vendorService,
            ICategoryLayoutService categoryLayoutService,
            ICollectionLayoutService collectionLayoutService,
            IBrandLayoutService brandLayoutService,
            IProductLayoutService productLayoutService,
            IDownloadService downloadService,
            IWarehouseService warehouseService,
            IDeliveryDateService deliveryDateService,
            ITaxCategoryService taxService,
            IMeasureService measureService,
            ILanguageService languageService,
            SeoSettings seoSetting)
        {
            _productService = productService;
            _categoryService = categoryService;
            _productCategoryService = productCategoryService;
            _brandService = brandService;
            _collectionService = collectionService;
            _productCollectionService = productCollectionService;
            _pictureService = pictureService;
            _slugService = slugService;
            _workContext = workContext;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _newsletterCategoryService = newsletterCategoryService;
            _countryService = countryService;
            _vendorService = vendorService;
            _categoryLayoutService = categoryLayoutService;
            _collectionLayoutService = collectionLayoutService;
            _brandLayoutService = brandLayoutService;
            _productLayoutService = productLayoutService;
            _downloadService = downloadService;
            _warehouseService = warehouseService;
            _deliveryDateService = deliveryDateService;
            _taxService = taxService;
            _measureService = measureService;
            _languageService = languageService;
            _seoSetting = seoSetting;
        }

        #endregion

        #region Utilities

        protected virtual PropertyManager<T> GetPropertyManager<T>(NPOI.SS.UserModel.ISheet worksheet)
        {
            var properties = new List<PropertyByName<T>>();
            var poz = 0;
            while (true)
            {
                try
                {
                    var cell = worksheet.GetRow(0).Cells[poz];

                    if (cell == null || string.IsNullOrEmpty(cell.StringCellValue))
                        break;

                    poz += 1;
                    properties.Add(new PropertyByName<T>(cell.StringCellValue.ToLower()));
                }
                catch
                {
                    break;
                }
            }
            return new PropertyManager<T>(properties.ToArray());
        }

        protected virtual void PrepareProductMapping(Product product, PropertyManager<Product> manager,
            IList<ProductLayout> layouts,
            IList<DeliveryDate> deliveryDates,
            IList<Warehouse> warehouses,
            IList<MeasureUnit> units,
            IList<TaxCategory> taxes)
        {

            foreach (var property in manager.GetProperties)
            {
                switch (property.PropertyName.ToLower())
                {
                    case "producttypeid":
                        product.ProductTypeId = (ProductType)property.IntValue;
                        break;
                    case "parentgroupedproductid":
                        var parentgroupedproductid = property.StringValue;
                        if (_productService.GetProductById(parentgroupedproductid) != null)
                            product.ParentGroupedProductId = property.StringValue;
                        break;
                    case "visibleindividually":
                        product.VisibleIndividually = property.BooleanValue;
                        break;
                    case "name":
                        product.Name = property.StringValue;
                        break;
                    case "shortdescription":
                        product.ShortDescription = property.StringValue;
                        break;
                    case "fulldescription":
                        product.FullDescription = property.StringValue;
                        break;
                    case "brandid":
                        var brandid = property.StringValue;
                        if (_brandService.GetBrandById(brandid) != null)
                            product.BrandId = property.StringValue;
                        break;
                    case "vendorid":
                        var vendorid = property.StringValue;
                        if (_vendorService.GetVendorById(vendorid) != null)
                            product.VendorId = property.StringValue;
                        break;
                    case "ProductLayoutId":
                        var layoutid = property.StringValue;
                        if (layouts.FirstOrDefault(x => x.Id == layoutid) != null)
                            product.ProductLayoutId = property.StringValue;
                        break;
                    case "showonhomepage":
                        product.ShowOnHomePage = property.BooleanValue;
                        break;
                    case "bestseller":
                        product.BestSeller = property.BooleanValue;
                        break;
                    case "metakeywords":
                        product.MetaKeywords = property.StringValue;
                        break;
                    case "metadescription":
                        product.MetaDescription = property.StringValue;
                        break;
                    case "metatitle":
                        product.MetaTitle = property.StringValue;
                        break;
                    case "allowcustomerreviews":
                        product.AllowCustomerReviews = property.BooleanValue;
                        break;
                    case "published":
                        product.Published = property.BooleanValue;
                        break;
                    case "sku":
                        product.Sku = property.StringValue;
                        break;
                    case "mpn":
                        product.Mpn = property.StringValue;
                        break;
                    case "gtin":
                        product.Gtin = property.StringValue;
                        break;
                    case "isgiftvoucher":
                        product.IsGiftVoucher = property.BooleanValue;
                        break;
                    case "giftvouchertypeid":
                        product.GiftVoucherTypeId = (GiftVoucherType)property.IntValue;
                        break;
                    case "overriddengiftvoucheramount":
                        product.OverGiftAmount = property.DecimalValue;
                        break;
                    case "requireotherproducts":
                        product.RequireOtherProducts = property.BooleanValue;
                        break;
                    case "requiredproductids":
                        product.RequiredProductIds = property.StringValue;
                        break;
                    case "automaticallyaddrequiredproducts":
                        product.AutoAddRequiredProducts = property.BooleanValue;
                        break;
                    case "isdownload":
                        product.IsDownload = property.BooleanValue;
                        break;
                    case "downloadid":
                        var downloadid = property.StringValue;
                        if (_downloadService.GetDownloadById(downloadid) != null)
                            product.DownloadId = downloadid;
                        break;
                    case "unlimiteddownloads":
                        product.UnlimitedDownloads = property.BooleanValue;
                        break;
                    case "maxnumberofdownloads":
                        product.MaxNumberOfDownloads = property.IntValue;
                        break;
                    case "downloadactivationtypeid":
                        product.DownloadActivationTypeId = (DownloadActivationType)property.IntValue;
                        break;
                    case "hassampledownload":
                        product.HasSampleDownload = property.BooleanValue;
                        break;
                    case "sampledownloadid":
                        var sampledownloadid = property.StringValue;
                        if (_downloadService.GetDownloadById(sampledownloadid) != null)
                            product.SampleDownloadId = property.StringValue;
                        break;
                    case "hasuseragreement":
                        product.HasUserAgreement = property.BooleanValue;
                        break;
                    case "useragreementtext":
                        product.UserAgreementText = property.StringValue;
                        break;
                    case "isrecurring":
                        product.IsRecurring = property.BooleanValue;
                        break;
                    case "recurringcyclelength":
                        product.RecurringCycleLength = property.IntValue;
                        break;
                    case "recurringcycleperiodid":
                        product.RecurringCyclePeriodId = (RecurringCyclePeriod)property.IntValue;
                        break;
                    case "recurringtotalcycles":
                        product.RecurringTotalCycles = property.IntValue;
                        break;
                    case "interval":
                        product.Interval = property.IntValue;
                        break;
                    case "intervalunitId":
                        product.IntervalUnitId = (IntervalUnit)property.IntValue;
                        break;
                    case "isshipenabled":
                        product.IsShipEnabled = property.BooleanValue;
                        break;
                    case "isfreeshipping":
                        product.IsFreeShipping = property.BooleanValue;
                        break;
                    case "shipseparately":
                        product.ShipSeparately = property.BooleanValue;
                        break;
                    case "additionalshippingcharge":
                        product.AdditionalShippingCharge = property.DecimalValue;
                        break;
                    case "deliverydateId":
                        var deliverydateid = property.StringValue;
                        if (deliveryDates.FirstOrDefault(x => x.Id == deliverydateid) != null)
                            product.DeliveryDateId = deliverydateid;
                        break;
                    case "istaxexempt":
                        product.IsTaxExempt = property.BooleanValue;
                        break;
                    case "taxcategoryid":
                        var taxcategoryid = property.StringValue;
                        if (taxes.FirstOrDefault(x => x.Id == taxcategoryid) != null)
                            product.TaxCategoryId = property.StringValue;
                        break;
                    case "istele":
                        product.IsTele = property.BooleanValue;
                        break;
                    case "manageinventorymethodid":
                        product.ManageInventoryMethodId =(ManageInventoryMethod)property.IntValue;
                        break;
                    case "usemultiplewarehouses":
                        product.UseMultipleWarehouses = property.BooleanValue;
                        break;
                    case "warehouseid":
                        var warehouseid = property.StringValue;
                        if (warehouses.FirstOrDefault(x => x.Id == warehouseid) != null)
                            product.WarehouseId = property.StringValue;
                        break;
                    case "stockquantity":
                        product.StockQuantity = property.IntValue;
                        break;
                    case "displaystockavailability":
                        product.StockAvailability = property.BooleanValue;
                        break;
                    case "displaystockquantity":
                        product.DisplayStockQuantity = property.BooleanValue;
                        break;
                    case "minstockquantity":
                        product.MinStockQuantity = property.IntValue;
                        break;
                    case "lowstockactivityid":
                        product.LowStockActivityId = (LowStockActivity)property.IntValue;
                        break;
                    case "notifyadminforquantitybelow":
                        product.NotifyAdminForQuantityBelow = property.IntValue;
                        break;
                    case "admincomment":
                        product.AdminComment = property.StringValue;
                        break;
                    case "flag":
                        product.Flag = property.StringValue;
                        break;
                    case "backordermodeid":
                        product.BackorderModeId = (BackorderMode)property.IntValue;
                        break;
                    case "allowoutofstocksubscriptions":
                        product.AllowOutOfStockSubscriptions = property.BooleanValue;
                        break;
                    case "orderminimumquantity":
                        product.OrderMinimumQuantity = property.IntValue;
                        break;
                    case "ordermaximumquantity":
                        product.OrderMaximumQuantity = property.IntValue;
                        break;
                    case "allowedquantities":
                        product.AllowedQuantities = property.StringValue;
                        break;
                    case "disablebuybutton":
                        product.DisableBuyButton = property.BooleanValue;
                        break;
                    case "disablewishlistbutton":
                        product.DisableWishlistButton = property.BooleanValue;
                        break;
                    case "availableforpreorder":
                        product.AvailableForPreOrder = property.BooleanValue;
                        break;
                    case "preorderavailabilitystartdatetimeutc":
                        product.PreOrderDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "callforprice":
                        product.CallForPrice = property.BooleanValue;
                        break;
                    case "price":
                        product.Price = property.DecimalValue;
                        break;
                    case "oldprice":
                        product.OldPrice = property.DecimalValue;
                        break;
                    case "catalogprice":
                        product.CatalogPrice = property.DecimalValue;
                        break;
                    case "startprice":
                        product.StartPrice = property.DecimalValue;
                        break;
                    case "productcost":
                        product.ProductCost = property.DecimalValue;
                        break;
                    case "customerentersprice":
                        product.EnteredPrice = property.BooleanValue;
                        break;
                    case "minimumcustomerenteredprice":
                        product.MinEnteredPrice = property.DecimalValue;
                        break;
                    case "maximumcustomerenteredprice":
                        product.MaxEnteredPrice = property.DecimalValue;
                        break;
                    case "basepriceenabled":
                        product.BasepriceEnabled = property.BooleanValue;
                        break;
                    case "basepriceamount":
                        product.BasepriceAmount = property.DecimalValue;
                        break;
                    case "basepriceunitid":
                        product.BasepriceUnitId = property.StringValue;
                        break;
                    case "basepricebaseamount":
                        product.BasepriceBaseAmount = property.DecimalValue;
                        break;
                    case "basepricebaseunitid":
                        product.BasepriceBaseUnitId = property.StringValue;
                        break;
                    case "markasnew":
                        product.MarkAsNew = property.BooleanValue;
                        break;
                    case "markasnewstartdatetimeutc":
                        product.MarkAsNewStartDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "markasnewenddatetimeutc":
                        product.MarkAsNewEndDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "unitid":
                        var unitid = property.StringValue;
                        if (units.FirstOrDefault(x => x.Id == unitid) != null)
                            product.UnitId = property.StringValue;
                        break;
                    case "weight":
                        product.Weight = property.DecimalValue;
                        break;
                    case "length":
                        product.Length = property.DecimalValue;
                        break;
                    case "width":
                        product.Width = property.DecimalValue;
                        break;
                    case "height":
                        product.Height = property.DecimalValue;
                        break;
                }
            }
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
                        var productCategory = new ProductCategory
                        {
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
                        var productCollection = new ProductCollection
                        {
                            CollectionId = collection.Id,
                            IsFeaturedProduct = false,
                            DisplayOrder = 1
                        };
                        await _productCollectionService.InsertProductCollection(productCollection, product.Id);
                    }
                }
            }
        }

        protected virtual async Task PrepareProductPictures(Product product, PropertyManager<Product> manager, bool isNew)
        {
            var picture1 = manager.GetProperty("picture1") != null ? manager.GetProperty("picture1").StringValue : string.Empty;
            var picture2 = manager.GetProperty("picture2") != null ? manager.GetProperty("picture2").StringValue : string.Empty;
            var picture3 = manager.GetProperty("picture3") != null ? manager.GetProperty("picture3").StringValue : string.Empty;

            foreach (var picturePath in new[] { picture1, picture2, picture3 })
            {
                if (String.IsNullOrEmpty(picturePath))
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
                        var picture = await _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(product.Name),"","", false, 
                            Domain.Common.Reference.Product, product.Id);
                        var productPicture = new ProductPicture
                        {
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
                        var productPicture = new ProductPicture
                        {
                            PictureId = picture.Id,
                            DisplayOrder = 1,
                        };
                        await _productService.InsertProductPicture(productPicture, product.Id);
                    }
                }
            }

        }

        protected virtual void PrepareCategoryMapping(Category category, PropertyManager<Category> manager, IList<CategoryLayout> layouts)
        {
            foreach (var property in manager.GetProperties)
            {
                switch (property.PropertyName.ToLower())
                {
                    case "name":
                        category.Name = property.StringValue;
                        break;
                    case "description":
                        category.Description = property.StringValue;
                        break;
                    case "CategoryLayoutId":
                        var CategoryLayoutId = property.StringValue;
                        if (layouts.FirstOrDefault(x => x.Id == CategoryLayoutId) != null)
                            category.CategoryLayoutId = property.StringValue;
                        break;
                    case "metakeywords":
                        category.MetaKeywords = property.StringValue;
                        break;
                    case "metadescription":
                        category.MetaDescription = property.StringValue;
                        break;
                    case "metatitle":
                        category.MetaTitle = property.StringValue;
                        break;
                    case "pagesize":
                        category.PageSize = property.IntValue > 0 ? property.IntValue : 10;
                        break;
                    case "allowcustomerstoselectpageSize":
                        category.AllowCustomersToSelectPageSize = property.BooleanValue;
                        break;
                    case "pagesizeoptions":
                        category.PageSizeOptions = property.StringValue;
                        break;
                    case "published":
                        category.Published = property.BooleanValue;
                        break;
                    case "displayorder":
                        category.DisplayOrder = property.IntValue;
                        break;
                    case "showonhomepage":
                        category.ShowOnHomePage = property.BooleanValue;
                        break;
                    case "includeinmenu":
                        category.IncludeInMenu = property.BooleanValue;
                        break;
                    case "showonsearchbox":
                        category.ShowOnSearchBox = property.BooleanValue;
                        break;
                    case "searchboxdisplayorder":
                        category.SearchBoxDisplayOrder = property.IntValue;
                        break;
                    case "flag":
                        category.Flag = property.StringValue;
                        break;
                    case "flagstyle":
                        category.FlagStyle = property.StringValue;
                        break;
                    case "icon":
                        category.Icon = property.StringValue;
                        break;
                    case "parentcategoryid":
                        if (!string.IsNullOrEmpty(property.StringValue) && property.StringValue != "0")
                            category.ParentCategoryId = property.StringValue;
                        break;
                }
            }
        }

        protected virtual void PrepareBrandMapping(Brand brand, PropertyManager<Brand> manager, IList<BrandLayout> layouts)
        {
            foreach (var property in manager.GetProperties)
            {
                switch (property.PropertyName.ToLower())
                {
                    case "name":
                        brand.Name = property.StringValue;
                        break;
                    case "description":
                        brand.Description = property.StringValue;
                        break;
                    case "brandlayoutid":
                        var brandLayoutId = property.StringValue;
                        if (layouts.FirstOrDefault(x => x.Id == brandLayoutId) != null)
                            brand.BrandLayoutId = property.StringValue;
                        break;
                    case "metakeywords":
                        brand.MetaKeywords = property.StringValue;
                        break;
                    case "metadescription":
                        brand.MetaDescription = property.StringValue;
                        break;
                    case "metatitle":
                        brand.MetaTitle = property.StringValue;
                        break;
                    case "pagesize":
                        brand.PageSize = property.IntValue > 0 ? property.IntValue : 10;
                        break;
                    case "allowcustomerstoselectpageSize":
                        brand.AllowCustomersToSelectPageSize = property.BooleanValue;
                        break;
                    case "pagesizeoptions":
                        brand.PageSizeOptions = property.StringValue;
                        break;
                    case "published":
                        brand.Published = property.BooleanValue;
                        break;
                    case "showonhomepage":
                        brand.ShowOnHomePage = property.BooleanValue;
                        break;
                    case "includeinmenu":
                        brand.IncludeInMenu = property.BooleanValue;
                        break;
                    case "displayorder":
                        brand.DisplayOrder = property.IntValue;
                        break;
                }
            }
        }

        protected virtual void PrepareCollectionMapping(Collection collection, PropertyManager<Collection> manager, IList<CollectionLayout> layouts)
        {
            foreach (var property in manager.GetProperties)
            {
                switch (property.PropertyName.ToLower())
                {
                    case "name":
                        collection.Name = property.StringValue;
                        break;
                    case "description":
                        collection.Description = property.StringValue;
                        break;
                    case "CollectionLayoutId":
                        var collectionLayoutId = property.StringValue;
                        if (layouts.FirstOrDefault(x => x.Id == collectionLayoutId) != null)
                            collection.CollectionLayoutId = property.StringValue;
                        break;
                    case "metakeywords":
                        collection.MetaKeywords = property.StringValue;
                        break;
                    case "metadescription":
                        collection.MetaDescription = property.StringValue;
                        break;
                    case "metatitle":
                        collection.MetaTitle = property.StringValue;
                        break;
                    case "pagesize":
                        collection.PageSize = property.IntValue > 0 ? property.IntValue : 10;
                        break;
                    case "allowcustomerstoselectpageSize":
                        collection.AllowCustomersToSelectPageSize = property.BooleanValue;
                        break;
                    case "pagesizeoptions":
                        collection.PageSizeOptions = property.StringValue;
                        break;
                    case "published":
                        collection.Published = property.BooleanValue;
                        break;
                    case "showonhomepage":
                        collection.ShowOnHomePage = property.BooleanValue;
                        break;
                    case "includeinmenu":
                        collection.IncludeInMenu = property.BooleanValue;
                        break;
                    case "displayorder":
                        collection.DisplayOrder = property.IntValue;
                        break;
                }
            }
        }

        protected virtual async Task ImportSubscription(string email, string storeId, bool isActive, bool iscategories, List<string> categories)
        {
            var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(email, storeId);
            if (subscription != null)
            {
                subscription.Email = email;
                subscription.Active = isActive;
                if (iscategories)
                {
                    subscription.Categories.Clear();
                    foreach (var item in categories)
                    {
                        subscription.Categories.Add(item);
                    }
                }
                await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);
            }
            else
            {
                subscription = new NewsLetterSubscription
                {
                    Active = isActive,
                    CreatedOnUtc = DateTime.UtcNow,
                    Email = email,
                    StoreId = storeId,
                    NewsLetterSubscriptionGuid = Guid.NewGuid()
                };
                foreach (var item in categories)
                {
                    subscription.Categories.Add(item);
                }
                await _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);
            }
        }
        protected virtual string GetMimeTypeFromFilePath(string filePath)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(filePath, out string mimeType);
            //set to jpeg in case mime type cannot be found
            if (mimeType == null)
                mimeType = "image/jpeg";
            return mimeType;
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
            if (String.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
                return null;

            var mimeType = GetMimeTypeFromFilePath(picturePath);
            var newPictureBinary = File.ReadAllBytes(picturePath);
            var pictureAlreadyExists = false;
            if (!String.IsNullOrEmpty(picId))
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

        #endregion

        #region Methods

        /// <summary>
        /// Import products from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual async Task ImportProductsFromXlsx(Stream stream)
        {
            var workbook = new XSSFWorkbook(stream);
            var worksheet = workbook.GetSheetAt(0);
            if (worksheet == null)
                throw new GrandException("No worksheet found");

            var manager = GetPropertyManager<Product>(worksheet);

            var layouts = await _productLayoutService.GetAllProductLayouts();
            var deliveryDates = await _deliveryDateService.GetAllDeliveryDates();
            var taxes = await _taxService.GetAllTaxCategories();
            var warehouses = await _warehouseService.GetAllWarehouses();
            var units = await _measureService.GetAllMeasureUnits();

            for (var iRow = 1; iRow < worksheet.PhysicalNumberOfRows; iRow++)
            {

                manager.ReadFromXlsx(worksheet, iRow);
                var sku = manager.GetProperty("sku") != null ? manager.GetProperty("sku").StringValue : string.Empty;
                var productid = manager.GetProperty("id") != null ? manager.GetProperty("id").StringValue : string.Empty;

                Product product = null;

                if (!string.IsNullOrEmpty(sku))
                    product = await _productService.GetProductBySku(sku);

                if (!string.IsNullOrEmpty(productid))
                    product = await _productService.GetProductById(productid);

                var isNew = product == null;

                product ??= new Product();

                if (isNew)
                {
                    product.CreatedOnUtc = DateTime.UtcNow;
                    product.ProductLayoutId = layouts.FirstOrDefault()?.Id;
                    product.DeliveryDateId = deliveryDates.FirstOrDefault()?.Id;
                    product.TaxCategoryId = taxes.FirstOrDefault()?.Id;
                    product.WarehouseId = warehouses.FirstOrDefault()?.Id;
                    product.UnitId = units.FirstOrDefault().Id;
                    if (!string.IsNullOrEmpty(productid))
                        product.Id = productid;
                }

                PrepareProductMapping(product, manager, layouts, deliveryDates, warehouses, units, taxes);

                if (isNew && manager.GetProperties.All(p => p.PropertyName.ToLower() != "producttypeid"))
                    product.ProductTypeId = ProductType.SimpleProduct;

                product.LowStock = product.MinStockQuantity > 0 && product.MinStockQuantity >= product.StockQuantity;

                product.UpdatedOnUtc = DateTime.UtcNow;

                if (isNew)
                {
                    await _productService.InsertProduct(product);
                }
                else
                {
                    await _productService.UpdateProduct(product);
                }

                //search engine name
                var seName = manager.GetProperty("sename") != null ? manager.GetProperty("sename").StringValue : product.Name;
                await _slugService.SaveSlug(product, await product.ValidateSeName(seName, product.Name, true, _seoSetting, _slugService, _languageService), "");
                var _seName = await product.ValidateSeName(seName, product.Name, true, _seoSetting, _slugService, _languageService);
                //search engine name
                await _slugService.SaveSlug(product, _seName, "");
                product.SeName = _seName;
                await _productService.UpdateProduct(product);

                //category mappings
                var categoryIds = manager.GetProperty("categoryids") != null ? manager.GetProperty("categoryids").StringValue : string.Empty;
                if (!string.IsNullOrEmpty(categoryIds))
                {
                    await PrepareProductCategories(product, categoryIds);
                }

                //collection mappings
                var collectionIds = manager.GetProperty("collectionids") != null ? manager.GetProperty("collectionids").StringValue : string.Empty;
                if (!string.IsNullOrEmpty(collectionIds))
                {
                    await PrepareProductCollections(product, collectionIds);
                }

                //pictures
                await PrepareProductPictures(product, manager, isNew);

            }

        }

        /// <summary>
        /// Import newsletter subscribers from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported subscribers</returns>
        public virtual async Task<int> ImportNewsletterSubscribersFromTxt(Stream stream)
        {
            int count = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    string[] tmp = line.Split(',');

                    var email = "";
                    bool isActive = true;
                    var categories = new List<string>();
                    bool iscategories = false;
                    string storeId = _workContext.CurrentStore.Id;
                    //parse
                    if (tmp.Length == 1)
                    {
                        //"email" only
                        email = tmp[0].Trim();
                    }
                    else if (tmp.Length == 2)
                    {
                        //"email" and "active" fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                    }
                    else if (tmp.Length == 3)
                    {
                        //"email" and "active" and "storeId" fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                        storeId = tmp[2].Trim();
                    }
                    else if (tmp.Length == 4)
                    {
                        //"email" and "active" and "storeId" and categories fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                        storeId = tmp[2].Trim();
                        try
                        {
                            var items = tmp[3].Trim().Split(';').ToList();
                            foreach (var item in items)
                            {
                                if (!string.IsNullOrEmpty(item))
                                {
                                    if (_newsletterCategoryService.GetNewsletterCategoryById(item) != null)
                                        categories.Add(item);
                                }
                            }
                            iscategories = true;
                        }
                        catch { };
                    }
                    else
                        throw new GrandException("Wrong file format");

                    //import
                    await ImportSubscription(email, storeId, isActive, iscategories, categories);

                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Import states from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported states</returns>
        public virtual async Task<int> ImportStatesFromTxt(Stream stream)
        {
            int count = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    string[] tmp = line.Split(',');

                    if (tmp.Length != 5)
                        throw new GrandException("Wrong file format");

                    //parse
                    var countryTwoLetterIsoCode = tmp[0].Trim();
                    var name = tmp[1].Trim();
                    var abbreviation = tmp[2].Trim();
                    bool published = Boolean.Parse(tmp[3].Trim());
                    int displayOrder = Int32.Parse(tmp[4].Trim());

                    var country = await _countryService.GetCountryByTwoLetterIsoCode(countryTwoLetterIsoCode);
                    if (country == null)
                    {
                        //country cannot be loaded. skip
                        continue;
                    }

                    //import
                    var states = country.StateProvinces.ToList();
                    var state = states.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (state != null)
                    {
                        state.Abbreviation = abbreviation;
                        state.Published = published;
                        state.DisplayOrder = displayOrder;
                        await _countryService.UpdateStateProvince(state, country.Id);
                    }
                    else
                    {
                        state = new StateProvince
                        {
                            Name = name,
                            Abbreviation = abbreviation,
                            Published = published,
                            DisplayOrder = displayOrder,
                        };
                        await _countryService.InsertStateProvince(state, country.Id);
                    }
                    count++;
                }
            }

            return count;
        }
        /// <summary>
        /// Import brands from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual async Task ImportBrandFromXlsx(Stream stream)
        {
            var workbook = new XSSFWorkbook(stream);
            var worksheet = workbook.GetSheetAt(0);
            if (worksheet == null)
                throw new GrandException("No worksheet found");

            var manager = GetPropertyManager<Brand>(worksheet);

            var layouts = await _brandLayoutService.GetAllBrandLayouts();

            for (var iRow = 1; iRow < worksheet.PhysicalNumberOfRows; iRow++)
            {

                manager.ReadFromXlsx(worksheet, iRow);
                var brandId = manager.GetProperty("id") != null ? manager.GetProperty("id").StringValue : string.Empty;
                var brand = string.IsNullOrEmpty(brandId) ? null : await _brandService.GetBrandById(brandId);

                var isNew = brand == null;

                brand ??= new Brand();

                if (isNew)
                {
                    brand.CreatedOnUtc = DateTime.UtcNow;
                    brand.BrandLayoutId = layouts.FirstOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(brandId))
                        brand.Id = brandId;
                }

                PrepareBrandMapping(brand, manager, layouts);


                var picture = manager.GetProperty("picture") != null ? manager.GetProperty("sename").StringValue : "";
                if (!string.IsNullOrEmpty(picture))
                {
                    var _picture = await LoadPicture(picture, brand.Name,
                        isNew ? "" : brand.PictureId);
                    if (_picture != null)
                        brand.PictureId = _picture.Id;
                }
                brand.UpdatedOnUtc = DateTime.UtcNow;

                if (isNew)
                    await _brandService.InsertBrand(brand);
                else
                    await _brandService.UpdateBrand(brand);

                var sename = manager.GetProperty("sename") != null ? manager.GetProperty("sename").StringValue : brand.Name;
                sename = await brand.ValidateSeName(sename, brand.Name, true, _seoSetting, _slugService, _languageService);
                brand.SeName = sename;
                await _brandService.UpdateBrand(brand);
                await _slugService.SaveSlug(brand, brand.SeName, "");

            }

        }
        /// <summary>
        /// Import collections from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual async Task ImportCollectionFromXlsx(Stream stream)
        {
            var workbook = new XSSFWorkbook(stream);
            var worksheet = workbook.GetSheetAt(0);
            if (worksheet == null)
                throw new GrandException("No worksheet found");

            var manager = GetPropertyManager<Collection>(worksheet);

            var layouts = await _collectionLayoutService.GetAllCollectionLayouts();

            for (var iRow = 1; iRow < worksheet.PhysicalNumberOfRows; iRow++)
            {

                manager.ReadFromXlsx(worksheet, iRow);
                var collectionid = manager.GetProperty("id") != null ? manager.GetProperty("id").StringValue : string.Empty;
                var collection = string.IsNullOrEmpty(collectionid) ? null : await _collectionService.GetCollectionById(collectionid);

                var isNew = collection == null;

                collection ??= new Collection();

                if (isNew)
                {
                    collection.CreatedOnUtc = DateTime.UtcNow;
                    collection.CollectionLayoutId = layouts.FirstOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(collectionid))
                        collection.Id = collectionid;
                }

                PrepareCollectionMapping(collection, manager, layouts);


                var picture = manager.GetProperty("picture") != null ? manager.GetProperty("sename").StringValue : "";
                if (!string.IsNullOrEmpty(picture))
                {
                    var _picture = await LoadPicture(picture, collection.Name,
                        isNew ? "" : collection.PictureId);
                    if (_picture != null)
                        collection.PictureId = _picture.Id;
                }
                collection.UpdatedOnUtc = DateTime.UtcNow;

                if (isNew)
                    await _collectionService.InsertCollection(collection);
                else
                    await _collectionService.UpdateCollection(collection);

                var sename = manager.GetProperty("sename") != null ? manager.GetProperty("sename").StringValue : collection.Name;
                sename = await collection.ValidateSeName(sename, collection.Name, true, _seoSetting, _slugService, _languageService);
                collection.SeName = sename;
                await _collectionService.UpdateCollection(collection);
                await _slugService.SaveSlug(collection, collection.SeName, "");

            }

        }

        /// <summary>
        /// Import categories from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual async Task ImportCategoryFromXlsx(Stream stream)
        {
            var workbook = new XSSFWorkbook(stream);
            var worksheet = workbook.GetSheetAt(0);
            if (worksheet == null)
                throw new GrandException("No worksheet found");

            var manager = GetPropertyManager<Category>(worksheet);

            var layouts = await _categoryLayoutService.GetAllCategoryLayouts();

            for (var iRow = 1; iRow < worksheet.PhysicalNumberOfRows; iRow++)
            {
                manager.ReadFromXlsx(worksheet, iRow);

                var categoryid = manager.GetProperty("id") != null ? manager.GetProperty("id").StringValue : string.Empty;
                var category = string.IsNullOrEmpty(categoryid) ? null : await _categoryService.GetCategoryById(categoryid);

                var isNew = category == null;

                category ??= new Category();

                if (isNew)
                {
                    category.CreatedOnUtc = DateTime.UtcNow;
                    category.CategoryLayoutId = layouts.FirstOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(categoryid))
                        category.Id = categoryid;
                }

                PrepareCategoryMapping(category, manager, layouts);
                category.UpdatedOnUtc = DateTime.UtcNow;

                if (isNew)
                    await _categoryService.InsertCategory(category);
                else
                    await _categoryService.UpdateCategory(category);

                var picture = manager.GetProperty("picture") != null ? manager.GetProperty("sename").StringValue : "";
                if (!string.IsNullOrEmpty(picture))
                {
                    var _picture = await LoadPicture(picture, category.Name, isNew ? "" : category.PictureId);
                    if (_picture != null)
                        category.PictureId = _picture.Id;
                }

                var sename = manager.GetProperty("sename") != null ? manager.GetProperty("sename").StringValue : category.Name;
                sename = await category.ValidateSeName(sename, category.Name, true, _seoSetting, _slugService, _languageService);
                category.SeName = sename;
                await _categoryService.UpdateCategory(category);
                await _slugService.SaveSlug(category, sename, "");

            }

        }

        #endregion
    }
}