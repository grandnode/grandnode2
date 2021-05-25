using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Utilities;
using Grand.Business.System.Interfaces.Installation;
using Grand.Business.System.Utilities;
using Grand.Domain;
using Grand.Domain.Admin;
using Grand.Domain.Affiliates;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Configuration;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Logging;
using Grand.Domain.Messages;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Pages;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tasks;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.TypeSearchers;
using Grand.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        #region Fields

        private readonly MongoRepository<GrandNodeVersion> _versionRepository;
        private readonly MongoRepository<AdminSiteMap> _adminRepository;
        private readonly MongoRepository<Bid> _bidRepository;
        private readonly MongoRepository<Affiliate> _affiliateRepository;
        private readonly MongoRepository<CampaignHistory> _campaignHistoryRepository;
        private readonly MongoRepository<Order> _orderRepository;
        private readonly MongoRepository<OrderNote> _orderNoteRepository;
        private readonly MongoRepository<MerchandiseReturn> _merchandiseReturnRepository;
        private readonly MongoRepository<MerchandiseReturnNote> _merchandiseReturnNoteRepository;
        private readonly MongoRepository<Store> _storeRepository;
        private readonly MongoRepository<MeasureDimension> _measureDimensionRepository;
        private readonly MongoRepository<MeasureWeight> _measureWeightRepository;
        private readonly MongoRepository<MeasureUnit> _measureUnitRepository;
        private readonly MongoRepository<TaxCategory> _taxCategoryRepository;
        private readonly MongoRepository<Language> _languageRepository;
        private readonly MongoRepository<TranslationResource> _lsrRepository;
        private readonly MongoRepository<Log> _logRepository;
        private readonly MongoRepository<Currency> _currencyRepository;
        private readonly MongoRepository<Customer> _customerRepository;
        private readonly MongoRepository<CustomerGroup> _customerGroupRepository;
        private readonly MongoRepository<CustomerGroupProduct> _customerGroupProductRepository;
        private readonly MongoRepository<CustomerProduct> _customerProductRepository;
        private readonly MongoRepository<CustomerProductPrice> _customerProductPriceRepository;
        private readonly MongoRepository<CustomerTagProduct> _customerTagProductRepository;
        private readonly MongoRepository<CustomerHistoryPassword> _customerHistoryPasswordRepository;
        private readonly MongoRepository<CustomerNote> _customerNoteRepository;
        private readonly MongoRepository<UserApi> _userapiRepository;
        private readonly MongoRepository<SpecificationAttribute> _specificationAttributeRepository;
        private readonly MongoRepository<CheckoutAttribute> _checkoutAttributeRepository;
        private readonly MongoRepository<ProductAttribute> _productAttributeRepository;
        private readonly MongoRepository<Category> _categoryRepository;
        private readonly MongoRepository<Brand> _brandRepository;
        private readonly MongoRepository<Vendor> _vendorRepository;
        private readonly MongoRepository<Collection> _collectionRepository;
        private readonly MongoRepository<Product> _productRepository;
        private readonly MongoRepository<ProductReservation> _productReservationRepository;
        private readonly MongoRepository<ProductAlsoPurchased> _productalsopurchasedRepository;
        private readonly MongoRepository<EntityUrl> _entityUrlRepository;
        private readonly MongoRepository<EmailAccount> _emailAccountRepository;
        private readonly MongoRepository<MessageTemplate> _messageTemplateRepository;
        private readonly MongoRepository<Country> _countryRepository;
        private readonly MongoRepository<Discount> _discountRepository;
        private readonly MongoRepository<DiscountCoupon> _discountCouponRepository;
        private readonly MongoRepository<DiscountUsageHistory> _discountusageRepository;
        private readonly MongoRepository<BlogPost> _blogPostRepository;
        private readonly MongoRepository<Page> _pageRepository;
        private readonly MongoRepository<NewsItem> _newsItemRepository;
        private readonly MongoRepository<NewsLetterSubscription> _newslettersubscriptionRepository;
        private readonly MongoRepository<ShippingMethod> _shippingMethodRepository;
        private readonly MongoRepository<DeliveryDate> _deliveryDateRepository;
        private readonly MongoRepository<ActivityLogType> _activityLogTypeRepository;
        private readonly MongoRepository<ProductTag> _productTagRepository;
        private readonly MongoRepository<ProductReview> _productReviewRepository;
        private readonly MongoRepository<ProductLayout> _productLayoutRepository;
        private readonly MongoRepository<CategoryLayout> _categoryLayoutRepository;
        private readonly MongoRepository<BrandLayout> _brandLayoutRepository;
        private readonly MongoRepository<CollectionLayout> _collectionLayoutRepository;
        private readonly MongoRepository<PageLayout> _pageLayoutRepository;
        private readonly MongoRepository<ScheduleTask> _scheduleTaskRepository;
        private readonly MongoRepository<LoyaltyPointsHistory> _loyaltypointshistoryRepository;
        private readonly MongoRepository<SearchTerm> _searchtermRepository;
        private readonly MongoRepository<Setting> _settingRepository;
        private readonly MongoRepository<Shipment> _shipmentRepository;
        private readonly MongoRepository<Warehouse> _warehouseRepository;
        private readonly MongoRepository<PickupPoint> _pickupPointsRepository;
        private readonly MongoRepository<Permission> _permissionRepository;
        private readonly MongoRepository<PermissionAction> _permissionAction;
        private readonly MongoRepository<ExternalAuthentication> _externalAuthenticationRepository;
        private readonly MongoRepository<MerchandiseReturnReason> _merchandiseReturnReasonRepository;
        private readonly MongoRepository<MerchandiseReturnAction> _merchandiseReturnActionRepository;
        private readonly MongoRepository<ContactUs> _contactUsRepository;
        private readonly MongoRepository<CustomerAction> _customerAction;
        private readonly MongoRepository<CustomerActionType> _customerActionType;
        private readonly MongoRepository<CustomerActionHistory> _customerActionHistory;
        private readonly MongoRepository<PopupArchive> _popupArchive;
        private readonly MongoRepository<CustomerReminderHistory> _customerReminderHistoryRepository;
        private readonly MongoRepository<RecentlyViewedProduct> _recentlyViewedProductRepository;
        private readonly MongoRepository<KnowledgebaseArticle> _knowledgebaseArticleRepository;
        private readonly MongoRepository<KnowledgebaseCategory> _knowledgebaseCategoryRepository;
        private readonly MongoRepository<OrderTag> _orderTagRepository;
        private readonly MongoRepository<OrderStatus> _orderStatusRepository;

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Ctor

        public InstallationService(IServiceProvider serviceProvider)
        {
            var dataProviderSettings = DataSettingsManager.LoadSettings(reloadSettings: true);

            _versionRepository = new MongoRepository<GrandNodeVersion>(dataProviderSettings.ConnectionString);
            _adminRepository = new MongoRepository<AdminSiteMap>(dataProviderSettings.ConnectionString);
            _bidRepository = new MongoRepository<Bid>(dataProviderSettings.ConnectionString);
            _affiliateRepository = new MongoRepository<Affiliate>(dataProviderSettings.ConnectionString);
            _campaignHistoryRepository = new MongoRepository<CampaignHistory>(dataProviderSettings.ConnectionString);
            _orderRepository = new MongoRepository<Order>(dataProviderSettings.ConnectionString);
            _orderNoteRepository = new MongoRepository<OrderNote>(dataProviderSettings.ConnectionString);
            _storeRepository = new MongoRepository<Store>(dataProviderSettings.ConnectionString);
            _measureDimensionRepository = new MongoRepository<MeasureDimension>(dataProviderSettings.ConnectionString);
            _measureWeightRepository = new MongoRepository<MeasureWeight>(dataProviderSettings.ConnectionString);
            _measureUnitRepository = new MongoRepository<MeasureUnit>(dataProviderSettings.ConnectionString);
            _taxCategoryRepository = new MongoRepository<TaxCategory>(dataProviderSettings.ConnectionString);
            _languageRepository = new MongoRepository<Language>(dataProviderSettings.ConnectionString);
            _lsrRepository = new MongoRepository<TranslationResource>(dataProviderSettings.ConnectionString);
            _logRepository = new MongoRepository<Log>(dataProviderSettings.ConnectionString);
            _currencyRepository = new MongoRepository<Currency>(dataProviderSettings.ConnectionString);
            _customerRepository = new MongoRepository<Customer>(dataProviderSettings.ConnectionString);
            _customerGroupRepository = new MongoRepository<CustomerGroup>(dataProviderSettings.ConnectionString);
            _customerProductRepository = new MongoRepository<CustomerProduct>(dataProviderSettings.ConnectionString);
            _customerProductPriceRepository = new MongoRepository<CustomerProductPrice>(dataProviderSettings.ConnectionString);
            _customerGroupProductRepository = new MongoRepository<CustomerGroupProduct>(dataProviderSettings.ConnectionString);
            _customerTagProductRepository = new MongoRepository<CustomerTagProduct>(dataProviderSettings.ConnectionString);
            _customerHistoryPasswordRepository = new MongoRepository<CustomerHistoryPassword>(dataProviderSettings.ConnectionString);
            _customerNoteRepository = new MongoRepository<CustomerNote>(dataProviderSettings.ConnectionString);
            _userapiRepository = new MongoRepository<UserApi>(dataProviderSettings.ConnectionString);
            _specificationAttributeRepository = new MongoRepository<SpecificationAttribute>(dataProviderSettings.ConnectionString);
            _checkoutAttributeRepository = new MongoRepository<CheckoutAttribute>(dataProviderSettings.ConnectionString);
            _productAttributeRepository = new MongoRepository<ProductAttribute>(dataProviderSettings.ConnectionString);
            _categoryRepository = new MongoRepository<Category>(dataProviderSettings.ConnectionString);
            _brandRepository = new MongoRepository<Brand>(dataProviderSettings.ConnectionString);
            _collectionRepository = new MongoRepository<Collection>(dataProviderSettings.ConnectionString);
            _productRepository = new MongoRepository<Product>(dataProviderSettings.ConnectionString);
            _productReservationRepository = new MongoRepository<ProductReservation>(dataProviderSettings.ConnectionString);
            _productalsopurchasedRepository = new MongoRepository<ProductAlsoPurchased>(dataProviderSettings.ConnectionString);
            _entityUrlRepository = new MongoRepository<EntityUrl>(dataProviderSettings.ConnectionString);
            _emailAccountRepository = new MongoRepository<EmailAccount>(dataProviderSettings.ConnectionString);
            _messageTemplateRepository = new MongoRepository<MessageTemplate>(dataProviderSettings.ConnectionString);
            _countryRepository = new MongoRepository<Country>(dataProviderSettings.ConnectionString);
            _discountRepository = new MongoRepository<Discount>(dataProviderSettings.ConnectionString);
            _discountCouponRepository = new MongoRepository<DiscountCoupon>(dataProviderSettings.ConnectionString);
            _blogPostRepository = new MongoRepository<BlogPost>(dataProviderSettings.ConnectionString);
            _pageRepository = new MongoRepository<Page>(dataProviderSettings.ConnectionString);
            _productReviewRepository = new MongoRepository<ProductReview>(dataProviderSettings.ConnectionString);
            _newsItemRepository = new MongoRepository<NewsItem>(dataProviderSettings.ConnectionString);
            _newslettersubscriptionRepository = new MongoRepository<NewsLetterSubscription>(dataProviderSettings.ConnectionString);
            _shippingMethodRepository = new MongoRepository<ShippingMethod>(dataProviderSettings.ConnectionString);
            _deliveryDateRepository = new MongoRepository<DeliveryDate>(dataProviderSettings.ConnectionString);
            _activityLogTypeRepository = new MongoRepository<ActivityLogType>(dataProviderSettings.ConnectionString);
            _productTagRepository = new MongoRepository<ProductTag>(dataProviderSettings.ConnectionString);
            _productLayoutRepository = new MongoRepository<ProductLayout>(dataProviderSettings.ConnectionString);
            _recentlyViewedProductRepository = new MongoRepository<RecentlyViewedProduct>(dataProviderSettings.ConnectionString);
            _categoryLayoutRepository = new MongoRepository<CategoryLayout>(dataProviderSettings.ConnectionString);
            _brandLayoutRepository = new MongoRepository<BrandLayout>(dataProviderSettings.ConnectionString);
            _collectionLayoutRepository = new MongoRepository<CollectionLayout>(dataProviderSettings.ConnectionString);
            _pageLayoutRepository = new MongoRepository<PageLayout>(dataProviderSettings.ConnectionString);
            _scheduleTaskRepository = new MongoRepository<ScheduleTask>(dataProviderSettings.ConnectionString);
            _merchandiseReturnRepository = new MongoRepository<MerchandiseReturn>(dataProviderSettings.ConnectionString);
            _merchandiseReturnNoteRepository = new MongoRepository<MerchandiseReturnNote>(dataProviderSettings.ConnectionString);
            _loyaltypointshistoryRepository = new MongoRepository<LoyaltyPointsHistory>(dataProviderSettings.ConnectionString);
            _searchtermRepository = new MongoRepository<SearchTerm>(dataProviderSettings.ConnectionString);
            _settingRepository = new MongoRepository<Setting>(dataProviderSettings.ConnectionString);
            _shipmentRepository = new MongoRepository<Shipment>(dataProviderSettings.ConnectionString);
            _warehouseRepository = new MongoRepository<Warehouse>(dataProviderSettings.ConnectionString);
            _pickupPointsRepository = new MongoRepository<PickupPoint>(dataProviderSettings.ConnectionString);
            _permissionRepository = new MongoRepository<Permission>(dataProviderSettings.ConnectionString);
            _permissionAction = new MongoRepository<PermissionAction>(dataProviderSettings.ConnectionString);
            _vendorRepository = new MongoRepository<Vendor>(dataProviderSettings.ConnectionString);
            _externalAuthenticationRepository = new MongoRepository<ExternalAuthentication>(dataProviderSettings.ConnectionString);
            _discountusageRepository = new MongoRepository<DiscountUsageHistory>(dataProviderSettings.ConnectionString);
            _merchandiseReturnReasonRepository = new MongoRepository<MerchandiseReturnReason>(dataProviderSettings.ConnectionString);
            _contactUsRepository = new MongoRepository<ContactUs>(dataProviderSettings.ConnectionString);
            _merchandiseReturnActionRepository = new MongoRepository<MerchandiseReturnAction>(dataProviderSettings.ConnectionString);
            _customerAction = new MongoRepository<CustomerAction>(dataProviderSettings.ConnectionString);
            _customerActionType = new MongoRepository<CustomerActionType>(dataProviderSettings.ConnectionString);
            _customerActionHistory = new MongoRepository<CustomerActionHistory>(dataProviderSettings.ConnectionString);
            _customerReminderHistoryRepository = new MongoRepository<CustomerReminderHistory>(dataProviderSettings.ConnectionString);
            _knowledgebaseArticleRepository = new MongoRepository<KnowledgebaseArticle>(dataProviderSettings.ConnectionString);
            _knowledgebaseCategoryRepository = new MongoRepository<KnowledgebaseCategory>(dataProviderSettings.ConnectionString);
            _popupArchive = new MongoRepository<PopupArchive>(dataProviderSettings.ConnectionString);
            _orderTagRepository = new MongoRepository<OrderTag>(dataProviderSettings.ConnectionString);
            _orderStatusRepository = new MongoRepository<OrderStatus>(dataProviderSettings.ConnectionString);
            _hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Utilities

        protected virtual string GetSamplesPath()
        {
            return Path.Combine(_hostingEnvironment.WebRootPath, "assets/samples/");
        }


        protected virtual async Task InstallVersion()
        {
            var version = new GrandNodeVersion {
                DataBaseVersion = GrandVersion.SupportedDBVersion
            };
            await _versionRepository.InsertAsync(version);
        }

        protected virtual async Task InstallMenuAdminSiteMap()
        {
            await _adminRepository.InsertManyAsync(StandardAdminSiteMap.SiteMap);
        }

        protected virtual async Task HashDefaultCustomerPassword(string defaultUserEmail, string defaultUserPassword)
        {
            var customerManagerService = _serviceProvider.GetRequiredService<ICustomerManagerService>();
            await customerManagerService.ChangePassword(new ChangePasswordRequest(defaultUserEmail, false, PasswordFormat.Hashed, defaultUserPassword));
        }

        private async Task CreateIndexes()
        {
            //version
            await _versionRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<GrandNodeVersion>((Builders<GrandNodeVersion>.IndexKeys.Ascending(x => x.DataBaseVersion)), new CreateIndexOptions() { Name = "DataBaseVersion", Unique = true }));

            //Store
            await _storeRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Store>((Builders<Store>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder" }));

            //Language
            await _lsrRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<TranslationResource>((Builders<TranslationResource>.IndexKeys.Ascending(x => x.LanguageId).Ascending(x => x.Name)), new CreateIndexOptions() { Name = "Language" }));
            await _lsrRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<TranslationResource>((Builders<TranslationResource>.IndexKeys.Ascending(x => x.Name)), new CreateIndexOptions() { Name = "ResourceName" }));

            //customer
            await _customerRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Customer>((Builders<Customer>.IndexKeys.Descending(x => x.CreatedOnUtc).Ascending(x => x.Deleted).Ascending("CustomerGroups._id")), new CreateIndexOptions() { Name = "CreatedOnUtc_1_CustomerGroups._id_1", Unique = false }));
            await _customerRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Customer>((Builders<Customer>.IndexKeys.Ascending(x => x.LastActivityDateUtc)), new CreateIndexOptions() { Name = "LastActivityDateUtc_1", Unique = false }));
            await _customerRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Customer>((Builders<Customer>.IndexKeys.Ascending(x => x.CustomerGuid)), new CreateIndexOptions() { Name = "CustomerGuid_1", Unique = false }));
            await _customerRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Customer>((Builders<Customer>.IndexKeys.Ascending(x => x.Email)), new CreateIndexOptions() { Name = "Email_1", Unique = false }));

            //customer group
            await _customerGroupProductRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerGroupProduct>((Builders<CustomerGroupProduct>.IndexKeys.Ascending(x => x.Id).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "CustomerGroupId_DisplayOrder", Unique = false }));

            //customer personalize product 
            await _customerProductRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerProduct>((Builders<CustomerProduct>.IndexKeys.Ascending(x => x.CustomerId).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "CustomerProduct", Unique = false }));
            await _customerProductRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerProduct>((Builders<CustomerProduct>.IndexKeys.Ascending(x => x.CustomerId).Ascending(x => x.ProductId)), new CreateIndexOptions() { Name = "CustomerProduct_Unique", Unique = true }));

            //customer product price
            await _customerProductPriceRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerProductPrice>((Builders<CustomerProductPrice>.IndexKeys.Ascending(x => x.CustomerId).Ascending(x => x.ProductId)), new CreateIndexOptions() { Name = "CustomerProduct", Unique = true }));

            //customer tag history
            await _customerTagProductRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerTagProduct>((Builders<CustomerTagProduct>.IndexKeys.Ascending(x => x.Id).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "CustomerTagId_DisplayOrder", Unique = false }));

            //customer history password
            await _customerHistoryPasswordRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerHistoryPassword>((Builders<CustomerHistoryPassword>.IndexKeys.Ascending(x => x.CustomerId).Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CustomerId", Unique = false }));

            //customer note
            await _customerNoteRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerNote>((Builders<CustomerNote>.IndexKeys.Ascending(x => x.CustomerId).Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CustomerId", Unique = false, Background = true }));

            //user api
            await _userapiRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<UserApi>((Builders<UserApi>.IndexKeys.Ascending(x => x.Email)), new CreateIndexOptions() { Name = "Email", Unique = true, Background = true }));

            //specificationAttribute
            await _specificationAttributeRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<SpecificationAttribute>((Builders<SpecificationAttribute>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder" }));

            //checkoutAttribute
            await _checkoutAttributeRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CheckoutAttribute>((Builders<CheckoutAttribute>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder" }));

            //category
            await _categoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Category>((Builders<Category>.IndexKeys.Ascending(x => x.ShowOnHomePage).Ascending(x => x.Published).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "ShowOnHomePage_DisplayOrder_1", Unique = false }));
            await _categoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Category>((Builders<Category>.IndexKeys.Ascending(x => x.ParentCategoryId).Ascending(x => x.Published).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "ParentCategoryId_1_DisplayOrder_1", Unique = false }));
            await _categoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Category>((Builders<Category>.IndexKeys.Ascending(x => x.FeaturedProductsOnHomePage).Ascending(x => x.Published).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "FeaturedProductsOnHomePage_DisplayOrder_1", Unique = false }));

            //collection
            await _collectionRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Collection>((Builders<Collection>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder_1", Unique = false }));
            await _collectionRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Collection>((Builders<Collection>.IndexKeys.Ascending("AppliedDiscounts")), new CreateIndexOptions() { Name = "AppliedDiscounts._id_1", Unique = false }));

            //brands
            await _brandRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Brand>((Builders<Brand>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder_1", Unique = false }));
            await _brandRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Brand>((Builders<Brand>.IndexKeys.Ascending("AppliedDiscounts")), new CreateIndexOptions() { Name = "AppliedDiscounts._id_1", Unique = false }));

            //Product
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.MarkAsNew).Ascending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "MarkAsNew_1_CreatedOnUtc_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.ShowOnHomePage).Ascending(x => x.DisplayOrder).Ascending(x => x.Name)), new CreateIndexOptions() { Name = "ShowOnHomePage_1_Published_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.BestSeller).Ascending(x => x.DisplayOrder).Ascending(x => x.Name)), new CreateIndexOptions() { Name = "ShowOnBestSeller_1_Published_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.ParentGroupedProductId).Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "ParentGroupedProductId_1_DisplayOrder_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.ProductTags).Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Name)), new CreateIndexOptions() { Name = "ProductTags._id_1_Name_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Name)), new CreateIndexOptions() { Name = "Name_1", Unique = false }));

            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending("ProductCategories.DisplayOrder")), new CreateIndexOptions() { Name = "ProductCategories.CategoryId_1_DisplayOrder_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.DisplayOrderCategory).Ascending("ProductCategories.CategoryId")), new CreateIndexOptions() { Name = "ProductCategories.CategoryId_1_OrderCategory_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Name).Ascending("ProductCategories.CategoryId")), new CreateIndexOptions() { Name = "ProductCategories.CategoryId_1_Name_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Price).Ascending("ProductCategories.CategoryId")), new CreateIndexOptions() { Name = "ProductCategories.CategoryId_1_Price_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Sold).Ascending("ProductCategories.CategoryId")), new CreateIndexOptions() { Name = "ProductCategories.CategoryId_1_Sold_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending("ProductCategories.CategoryId").Ascending("ProductCategories.IsFeaturedProduct")), new CreateIndexOptions() { Name = "ProductCategories.CategoryId_1_IsFeaturedProduct_1", Unique = false }));

            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending("ProductCollections.CollectionId").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.DisplayOrderCollection)), new CreateIndexOptions() { Name = "ProductCollections.CollectionId_1_OrderCategory_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending("ProductCollections.CollectionId").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Name)), new CreateIndexOptions() { Name = "ProductCollections.CollectionId_1_Name_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending("ProductCollections.CollectionId").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Sold)), new CreateIndexOptions() { Name = "ProductCollections.CollectionId_1_Sold_1", Unique = false }));
            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending("ProductCollections.CollectionId").Ascending("ProductCollections.IsFeaturedProduct").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually)), new CreateIndexOptions() { Name = "ProductCollections.CollectionId_1_IsFeaturedProduct_1", Unique = false }));

            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.BrandId).Ascending(x => x.DisplayOrderBrand)), new CreateIndexOptions() { Name = "ProductBrand", Unique = false }));

            await _productRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending("ProductSpecificationAttributes.SpecificationAttributeOptionId").Ascending("ProductSpecificationAttributes.AllowFiltering")), new CreateIndexOptions() { Name = "ProductSpecificationAttributes", Unique = false }));

            //productreseration
            await _productReservationRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<ProductReservation>((Builders<ProductReservation>.IndexKeys.Ascending(x => x.ProductId).Ascending(x => x.Date)), new CreateIndexOptions() { Name = "ProductReservation", Unique = false }));

            //bid
            await _bidRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Bid>((Builders<Bid>.IndexKeys.Ascending(x => x.ProductId).Ascending(x => x.CustomerId).Descending(x => x.Date)), new CreateIndexOptions() { Name = "ProductCustomer", Unique = false }));
            await _bidRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Bid>((Builders<Bid>.IndexKeys.Ascending(x => x.ProductId).Descending(x => x.Date)), new CreateIndexOptions() { Name = "ProductDate", Unique = false }));

            //ProductReview
            await _productReviewRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<ProductReview>((Builders<ProductReview>.IndexKeys.Ascending(x => x.ProductId).Ascending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "ProductId", Unique = false }));

            //Recently Viewed Products
            await _recentlyViewedProductRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<RecentlyViewedProduct>((Builders<RecentlyViewedProduct>.IndexKeys.Ascending(x => x.CustomerId).Ascending(x => x.ProductId).Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CustomerId.ProductId" }));

            //Product also purchased
            await _productalsopurchasedRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<ProductAlsoPurchased>((Builders<ProductAlsoPurchased>.IndexKeys.Ascending(x => x.ProductId)), new CreateIndexOptions() { Name = "ProductId", Unique = false, Background = true }));

            //url record
            await _entityUrlRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<EntityUrl>((Builders<EntityUrl>.IndexKeys.Ascending(x => x.Slug).Ascending(x => x.IsActive)), new CreateIndexOptions() { Name = "Slug" }));
            await _entityUrlRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<EntityUrl>((Builders<EntityUrl>.IndexKeys.Ascending(x => x.EntityId).Ascending(x => x.EntityName).Ascending(x => x.LanguageId).Ascending(x => x.IsActive)), new CreateIndexOptions() { Name = "UrlEntity" }));


            //message template
            await _messageTemplateRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<MessageTemplate>((Builders<MessageTemplate>.IndexKeys.Ascending(x => x.Name)), new CreateIndexOptions() { Name = "Name", Unique = false }));

            // Country and Stateprovince
            await _countryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Country>((Builders<Country>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder" }));

            //discount
            await _discountCouponRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<DiscountCoupon>((Builders<DiscountCoupon>.IndexKeys.Ascending(x => x.CouponCode)), new CreateIndexOptions() { Name = "CouponCode", Unique = true }));
            await _discountCouponRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<DiscountCoupon>((Builders<DiscountCoupon>.IndexKeys.Ascending(x => x.DiscountId)), new CreateIndexOptions() { Name = "DiscountId", Unique = false }));

            await _discountusageRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<DiscountUsageHistory>((Builders<DiscountUsageHistory>.IndexKeys.Ascending(x => x.CustomerId)), new CreateIndexOptions() { Name = "CustomerId" }));
            await _discountusageRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<DiscountUsageHistory>((Builders<DiscountUsageHistory>.IndexKeys.Ascending(x => x.DiscountId)), new CreateIndexOptions() { Name = "DiscountId" }));
            await _discountusageRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<DiscountUsageHistory>((Builders<DiscountUsageHistory>.IndexKeys.Ascending(x => x.OrderId)), new CreateIndexOptions() { Name = "OrderId" }));

            //knowledgebase
            await _knowledgebaseArticleRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<KnowledgebaseArticle>((Builders<KnowledgebaseArticle>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder", Unique = false }));
            await _knowledgebaseCategoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<KnowledgebaseCategory>((Builders<KnowledgebaseCategory>.IndexKeys.Ascending(x => x.DisplayOrder)), new CreateIndexOptions() { Name = "DisplayOrder", Unique = false }));

            //page
            await _pageRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Page>((Builders<Page>.IndexKeys.Ascending(x => x.SystemName)), new CreateIndexOptions() { Name = "SystemName", Unique = false }));

            //news
            await _newsItemRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<NewsItem>((Builders<NewsItem>.IndexKeys.Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CreatedOnUtc", Unique = false }));

            //newsletter
            await _newslettersubscriptionRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<NewsLetterSubscription>((Builders<NewsLetterSubscription>.IndexKeys.Ascending(x => x.CustomerId)), new CreateIndexOptions() { Name = "CustomerId", Unique = false }));
            await _newslettersubscriptionRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<NewsLetterSubscription>((Builders<NewsLetterSubscription>.IndexKeys.Ascending(x => x.Email)), new CreateIndexOptions() { Name = "Email", Unique = false }));

            //Log
            await _logRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Log>((Builders<Log>.IndexKeys.Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CreatedOnUtc", Unique = false }));

            //Campaign history
            await _campaignHistoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignHistory>((Builders<CampaignHistory>.IndexKeys.Ascending(x => x.CampaignId).Descending(x => x.CreatedDateUtc)), new CreateIndexOptions() { Name = "CampaignId", Unique = false }));

            //loyalty points
            await _loyaltypointshistoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<LoyaltyPointsHistory>((Builders<LoyaltyPointsHistory>.IndexKeys.Ascending(x => x.CustomerId)), new CreateIndexOptions() { Name = "CustomerId" }));

            //search term
            await _searchtermRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<SearchTerm>((Builders<SearchTerm>.IndexKeys.Descending(x => x.Count)), new CreateIndexOptions() { Name = "Count", Unique = false }));

            //setting
            await _settingRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Setting>((Builders<Setting>.IndexKeys.Ascending(x => x.Name)), new CreateIndexOptions() { Name = "Name", Unique = false }));

            //shipment
            await _shipmentRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Shipment>((Builders<Shipment>.IndexKeys.Ascending(x => x.ShipmentNumber)), new CreateIndexOptions() { Name = "ShipmentNumber", Unique = true }));
            await _shipmentRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Shipment>((Builders<Shipment>.IndexKeys.Ascending(x => x.OrderId)), new CreateIndexOptions() { Name = "OrderId" }));

            //order
            await _orderRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>((Builders<Order>.IndexKeys.Ascending(x => x.CustomerId).Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CustomerId_1_CreatedOnUtc_-1", Unique = false }));
            await _orderRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>((Builders<Order>.IndexKeys.Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CreatedOnUtc_-1", Unique = false }));
            await _orderRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>((Builders<Order>.IndexKeys.Descending(x => x.OrderNumber)), new CreateIndexOptions() { Name = "OrderNumber", Unique = false }));
            await _orderRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>((Builders<Order>.IndexKeys.Ascending(x => x.Code)), new CreateIndexOptions() { Name = "OrderCode", Unique = false }));
            await _orderRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>((Builders<Order>.IndexKeys.Ascending("OrderItems.ProductId")), new CreateIndexOptions() { Name = "OrderItemsProductId" }));
            await _orderRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>((Builders<Order>.IndexKeys.Ascending("OrderItems._id")), new CreateIndexOptions() { Name = "OrderItemId" }));

            await _orderStatusRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<OrderStatus>((Builders<OrderStatus>.IndexKeys.Ascending(x => x.StatusId)), new CreateIndexOptions() { Name = "StatusId", Unique = true }));

            await _orderNoteRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<OrderNote>((Builders<OrderNote>.IndexKeys.Ascending(x => x.OrderId).Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "Id", Unique = false, Background = true }));

            //permision
            await _permissionRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Permission>((Builders<Permission>.IndexKeys.Ascending(x => x.SystemName)), new CreateIndexOptions() { Name = "SystemName", Unique = true }));
            await _permissionAction.Collection.Indexes.CreateOneAsync(new CreateIndexModel<PermissionAction>((Builders<PermissionAction>.IndexKeys.Ascending(x => x.SystemName)), new CreateIndexOptions() { Name = "SystemName", Unique = false }));

            //externalauth
            await _externalAuthenticationRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<ExternalAuthentication>((Builders<ExternalAuthentication>.IndexKeys.Ascending(x => x.CustomerId)), new CreateIndexOptions() { Name = "CustomerId" }));

            //merchandise return
            await _merchandiseReturnRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<MerchandiseReturn>((Builders<MerchandiseReturn>.IndexKeys.Ascending(x => x.ReturnNumber)), new CreateIndexOptions() { Name = "ReturnNumber", Unique = true }));
            await _merchandiseReturnNoteRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<MerchandiseReturnNote>((Builders<MerchandiseReturnNote>.IndexKeys.Ascending(x => x.MerchandiseReturnId).Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "Id", Unique = false, Background = true }));

            //contactus
            await _contactUsRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<ContactUs>((Builders<ContactUs>.IndexKeys.Ascending(x => x.Email)), new CreateIndexOptions() { Name = "Email", Unique = false }));
            await _contactUsRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<ContactUs>((Builders<ContactUs>.IndexKeys.Descending(x => x.CreatedOnUtc)), new CreateIndexOptions() { Name = "CreatedOnUtc", Unique = false }));

            //customer action
            await _customerAction.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerAction>((Builders<CustomerAction>.IndexKeys.Ascending(x => x.ActionTypeId)), new CreateIndexOptions() { Name = "ActionTypeId", Unique = false }));

            await _customerActionHistory.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerActionHistory>((Builders<CustomerActionHistory>.IndexKeys.Ascending(x => x.CustomerId).Ascending(x => x.CustomerActionId)), new CreateIndexOptions() { Name = "Customer_Action", Unique = false }));

            //banner
            await _popupArchive.Collection.Indexes.CreateOneAsync(new CreateIndexModel<PopupArchive>((Builders<PopupArchive>.IndexKeys.Ascending(x => x.CustomerActionId)), new CreateIndexOptions() { Name = "CustomerActionId", Unique = false }));

            //customer reminder
            await _customerReminderHistoryRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<CustomerReminderHistory>((Builders<CustomerReminderHistory>.IndexKeys.Ascending(x => x.CustomerId).Ascending(x => x.CustomerReminderId)), new CreateIndexOptions() { Name = "CustomerId", Unique = false }));
        }

        private async Task CreateTables(string local)
        {
            if (string.IsNullOrEmpty(local))
                local = "en";

            try
            {
                var options = new CreateCollectionOptions();
                var collation = new Collation(local);
                options.Collation = collation;
                var connectionString = DataSettingsManager.LoadSettings().ConnectionString;

                var mongourl = new MongoUrl(connectionString);
                var databaseName = mongourl.DatabaseName;
                var mongodb = new MongoClient(connectionString).GetDatabase(databaseName);
                var dbContext = new MongoDBContext(mongodb);

                var typeSearcher = _serviceProvider.GetRequiredService<ITypeSearcher>();
                var q = typeSearcher.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Grand.Domain");
                foreach (var item in q.GetTypes())
                {
                    if (item.BaseType != null && item.IsClass && item.BaseType == typeof(BaseEntity))
                        await dbContext.Database().CreateCollectionAsync(item.Name, options);
                }
            }
            catch (Exception ex)
            {
                throw new GrandException(ex.Message);
            }
        }

        #endregion

        #region Methods


        public virtual async Task InstallData(string defaultUserEmail,
            string defaultUserPassword, string collation, bool installSampleData = true, string companyName = "", string companyAddress = "",
            string companyPhoneNumber = "", string companyEmail = "")
        {
            defaultUserEmail = defaultUserEmail.ToLower();

            await CreateTables(collation);
            await CreateIndexes();
            await InstallVersion();
            await InstallMenuAdminSiteMap();
            await InstallStores(companyName, companyAddress, companyPhoneNumber, companyEmail);
            await InstallMeasures();
            await InstallTaxCategories();
            await InstallLanguages();
            await InstallCurrencies();
            await InstallCountriesAndStates();
            await InstallShippingMethods();
            await InstallDeliveryDates();
            await InstallCustomersAndUsers(defaultUserEmail, defaultUserPassword);
            await InstallEmailAccounts();
            await InstallMessageTemplates();
            await InstallCustomerAction();
            await InstallSettings(installSampleData);
            await InstallPageLayouts();
            await InstallPages();
            await InstallLocaleResources();
            await InstallActivityLogTypes();
            await HashDefaultCustomerPassword(defaultUserEmail, defaultUserPassword);
            await InstallProductLayouts();
            await InstallCategoryLayouts();
            await InstallBrandLayouts();
            await InstallCollectionLayouts();
            await InstallScheduleTasks();
            await InstallMerchandiseReturnReasons();
            await InstallMerchandiseReturnActions();
            await InstallOrderStatus();
            if (installSampleData)
            {
                await InstallCheckoutAttributes();
                await InstallSpecificationAttributes();
                await InstallProductAttributes();
                await InstallCategories();
                await InstallBrands();
                await InstallProducts(defaultUserEmail);
                await InstallDiscounts();
                await InstallBlogPosts();
                await InstallNews();
                await InstallWarehouses();
                await InstallPickupPoints();
                await InstallVendors();
                await InstallAffiliates();
                await InstallOrderTags();
            }
        }


        #endregion

    }
}
