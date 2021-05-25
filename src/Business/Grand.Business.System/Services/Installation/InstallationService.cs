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

        private readonly IRepository<GrandNodeVersion> _versionRepository;
        private readonly IRepository<AdminSiteMap> _adminRepository;
        private readonly IRepository<Bid> _bidRepository;
        private readonly IRepository<Affiliate> _affiliateRepository;
        private readonly IRepository<CampaignHistory> _campaignHistoryRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderNote> _orderNoteRepository;
        private readonly IRepository<MerchandiseReturn> _merchandiseReturnRepository;
        private readonly IRepository<MerchandiseReturnNote> _merchandiseReturnNoteRepository;
        private readonly IRepository<Store> _storeRepository;
        private readonly IRepository<MeasureDimension> _measureDimensionRepository;
        private readonly IRepository<MeasureWeight> _measureWeightRepository;
        private readonly IRepository<MeasureUnit> _measureUnitRepository;
        private readonly IRepository<TaxCategory> _taxCategoryRepository;
        private readonly IRepository<Language> _languageRepository;
        private readonly IRepository<TranslationResource> _lsrRepository;
        private readonly IRepository<Log> _logRepository;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CustomerGroup> _customerGroupRepository;
        private readonly IRepository<CustomerGroupProduct> _customerGroupProductRepository;
        private readonly IRepository<CustomerProduct> _customerProductRepository;
        private readonly IRepository<CustomerProductPrice> _customerProductPriceRepository;
        private readonly IRepository<CustomerTagProduct> _customerTagProductRepository;
        private readonly IRepository<CustomerHistoryPassword> _customerHistoryPasswordRepository;
        private readonly IRepository<CustomerNote> _customerNoteRepository;
        private readonly IRepository<UserApi> _userapiRepository;
        private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
        private readonly IRepository<CheckoutAttribute> _checkoutAttributeRepository;
        private readonly IRepository<ProductAttribute> _productAttributeRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<Collection> _collectionRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductReservation> _productReservationRepository;
        private readonly IRepository<ProductAlsoPurchased> _productalsopurchasedRepository;
        private readonly IRepository<EntityUrl> _entityUrlRepository;
        private readonly IRepository<EmailAccount> _emailAccountRepository;
        private readonly IRepository<MessageTemplate> _messageTemplateRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<Discount> _discountRepository;
        private readonly IRepository<DiscountCoupon> _discountCouponRepository;
        private readonly IRepository<DiscountUsageHistory> _discountusageRepository;
        private readonly IRepository<BlogPost> _blogPostRepository;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<NewsItem> _newsItemRepository;
        private readonly IRepository<NewsLetterSubscription> _newslettersubscriptionRepository;
        private readonly IRepository<ShippingMethod> _shippingMethodRepository;
        private readonly IRepository<DeliveryDate> _deliveryDateRepository;
        private readonly IRepository<ActivityLogType> _activityLogTypeRepository;
        private readonly IRepository<ProductTag> _productTagRepository;
        private readonly IRepository<ProductReview> _productReviewRepository;
        private readonly IRepository<ProductLayout> _productLayoutRepository;
        private readonly IRepository<CategoryLayout> _categoryLayoutRepository;
        private readonly IRepository<BrandLayout> _brandLayoutRepository;
        private readonly IRepository<CollectionLayout> _collectionLayoutRepository;
        private readonly IRepository<PageLayout> _pageLayoutRepository;
        private readonly IRepository<ScheduleTask> _scheduleTaskRepository;
        private readonly IRepository<LoyaltyPointsHistory> _loyaltypointshistoryRepository;
        private readonly IRepository<SearchTerm> _searchtermRepository;
        private readonly IRepository<Setting> _settingRepository;
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<PickupPoint> _pickupPointsRepository;
        private readonly IRepository<Permission> _permissionRepository;
        private readonly IRepository<PermissionAction> _permissionAction;
        private readonly IRepository<ExternalAuthentication> _externalAuthenticationRepository;
        private readonly IRepository<MerchandiseReturnReason> _merchandiseReturnReasonRepository;
        private readonly IRepository<MerchandiseReturnAction> _merchandiseReturnActionRepository;
        private readonly IRepository<ContactUs> _contactUsRepository;
        private readonly IRepository<CustomerAction> _customerAction;
        private readonly IRepository<CustomerActionType> _customerActionType;
        private readonly IRepository<CustomerActionHistory> _customerActionHistory;
        private readonly IRepository<PopupArchive> _popupArchive;
        private readonly IRepository<CustomerReminderHistory> _customerReminderHistoryRepository;
        private readonly IRepository<RecentlyViewedProduct> _recentlyViewedProductRepository;
        private readonly IRepository<KnowledgebaseArticle> _knowledgebaseArticleRepository;
        private readonly IRepository<KnowledgebaseCategory> _knowledgebaseCategoryRepository;
        private readonly IRepository<OrderTag> _orderTagRepository;
        private readonly IRepository<OrderStatus> _orderStatusRepository;

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Ctor

        public InstallationService(IServiceProvider serviceProvider)
        {
            var dataProviderSettings = DataSettingsManager.LoadSettings(reloadSettings: true);

            _versionRepository = new Repository<GrandNodeVersion>(dataProviderSettings.ConnectionString);
            _adminRepository = new Repository<AdminSiteMap>(dataProviderSettings.ConnectionString);
            _bidRepository = new Repository<Bid>(dataProviderSettings.ConnectionString);
            _affiliateRepository = new Repository<Affiliate>(dataProviderSettings.ConnectionString);
            _campaignHistoryRepository = new Repository<CampaignHistory>(dataProviderSettings.ConnectionString);
            _orderRepository = new Repository<Order>(dataProviderSettings.ConnectionString);
            _orderNoteRepository = new Repository<OrderNote>(dataProviderSettings.ConnectionString);
            _storeRepository = new Repository<Store>(dataProviderSettings.ConnectionString);
            _measureDimensionRepository = new Repository<MeasureDimension>(dataProviderSettings.ConnectionString);
            _measureWeightRepository = new Repository<MeasureWeight>(dataProviderSettings.ConnectionString);
            _measureUnitRepository = new Repository<MeasureUnit>(dataProviderSettings.ConnectionString);
            _taxCategoryRepository = new Repository<TaxCategory>(dataProviderSettings.ConnectionString);
            _languageRepository = new Repository<Language>(dataProviderSettings.ConnectionString);
            _lsrRepository = new Repository<TranslationResource>(dataProviderSettings.ConnectionString);
            _logRepository = new Repository<Log>(dataProviderSettings.ConnectionString);
            _currencyRepository = new Repository<Currency>(dataProviderSettings.ConnectionString);
            _customerRepository = new Repository<Customer>(dataProviderSettings.ConnectionString);
            _customerGroupRepository = new Repository<CustomerGroup>(dataProviderSettings.ConnectionString);
            _customerProductRepository = new Repository<CustomerProduct>(dataProviderSettings.ConnectionString);
            _customerProductPriceRepository = new Repository<CustomerProductPrice>(dataProviderSettings.ConnectionString);
            _customerGroupProductRepository = new Repository<CustomerGroupProduct>(dataProviderSettings.ConnectionString);
            _customerTagProductRepository = new Repository<CustomerTagProduct>(dataProviderSettings.ConnectionString);
            _customerHistoryPasswordRepository = new Repository<CustomerHistoryPassword>(dataProviderSettings.ConnectionString);
            _customerNoteRepository = new Repository<CustomerNote>(dataProviderSettings.ConnectionString);
            _userapiRepository = new Repository<UserApi>(dataProviderSettings.ConnectionString);
            _specificationAttributeRepository = new Repository<SpecificationAttribute>(dataProviderSettings.ConnectionString);
            _checkoutAttributeRepository = new Repository<CheckoutAttribute>(dataProviderSettings.ConnectionString);
            _productAttributeRepository = new Repository<ProductAttribute>(dataProviderSettings.ConnectionString);
            _categoryRepository = new Repository<Category>(dataProviderSettings.ConnectionString);
            _brandRepository = new Repository<Brand>(dataProviderSettings.ConnectionString);
            _collectionRepository = new Repository<Collection>(dataProviderSettings.ConnectionString);
            _productRepository = new Repository<Product>(dataProviderSettings.ConnectionString);
            _productReservationRepository = new Repository<ProductReservation>(dataProviderSettings.ConnectionString);
            _productalsopurchasedRepository = new Repository<ProductAlsoPurchased>(dataProviderSettings.ConnectionString);
            _entityUrlRepository = new Repository<EntityUrl>(dataProviderSettings.ConnectionString);
            _emailAccountRepository = new Repository<EmailAccount>(dataProviderSettings.ConnectionString);
            _messageTemplateRepository = new Repository<MessageTemplate>(dataProviderSettings.ConnectionString);
            _countryRepository = new Repository<Country>(dataProviderSettings.ConnectionString);
            _discountRepository = new Repository<Discount>(dataProviderSettings.ConnectionString);
            _discountCouponRepository = new Repository<DiscountCoupon>(dataProviderSettings.ConnectionString);
            _blogPostRepository = new Repository<BlogPost>(dataProviderSettings.ConnectionString);
            _pageRepository = new Repository<Page>(dataProviderSettings.ConnectionString);
            _productReviewRepository = new Repository<ProductReview>(dataProviderSettings.ConnectionString);
            _newsItemRepository = new Repository<NewsItem>(dataProviderSettings.ConnectionString);
            _newslettersubscriptionRepository = new Repository<NewsLetterSubscription>(dataProviderSettings.ConnectionString);
            _shippingMethodRepository = new Repository<ShippingMethod>(dataProviderSettings.ConnectionString);
            _deliveryDateRepository = new Repository<DeliveryDate>(dataProviderSettings.ConnectionString);
            _activityLogTypeRepository = new Repository<ActivityLogType>(dataProviderSettings.ConnectionString);
            _productTagRepository = new Repository<ProductTag>(dataProviderSettings.ConnectionString);
            _productLayoutRepository = new Repository<ProductLayout>(dataProviderSettings.ConnectionString);
            _recentlyViewedProductRepository = new Repository<RecentlyViewedProduct>(dataProviderSettings.ConnectionString);
            _categoryLayoutRepository = new Repository<CategoryLayout>(dataProviderSettings.ConnectionString);
            _brandLayoutRepository = new Repository<BrandLayout>(dataProviderSettings.ConnectionString);
            _collectionLayoutRepository = new Repository<CollectionLayout>(dataProviderSettings.ConnectionString);
            _pageLayoutRepository = new Repository<PageLayout>(dataProviderSettings.ConnectionString);
            _scheduleTaskRepository = new Repository<ScheduleTask>(dataProviderSettings.ConnectionString);
            _merchandiseReturnRepository = new Repository<MerchandiseReturn>(dataProviderSettings.ConnectionString);
            _merchandiseReturnNoteRepository = new Repository<MerchandiseReturnNote>(dataProviderSettings.ConnectionString);
            _loyaltypointshistoryRepository = new Repository<LoyaltyPointsHistory>(dataProviderSettings.ConnectionString);
            _searchtermRepository = new Repository<SearchTerm>(dataProviderSettings.ConnectionString);
            _settingRepository = new Repository<Setting>(dataProviderSettings.ConnectionString);
            _shipmentRepository = new Repository<Shipment>(dataProviderSettings.ConnectionString);
            _warehouseRepository = new Repository<Warehouse>(dataProviderSettings.ConnectionString);
            _pickupPointsRepository = new Repository<PickupPoint>(dataProviderSettings.ConnectionString);
            _permissionRepository = new Repository<Permission>(dataProviderSettings.ConnectionString);
            _permissionAction = new Repository<PermissionAction>(dataProviderSettings.ConnectionString);
            _vendorRepository = new Repository<Vendor>(dataProviderSettings.ConnectionString);
            _externalAuthenticationRepository = new Repository<ExternalAuthentication>(dataProviderSettings.ConnectionString);
            _discountusageRepository = new Repository<DiscountUsageHistory>(dataProviderSettings.ConnectionString);
            _merchandiseReturnReasonRepository = new Repository<MerchandiseReturnReason>(dataProviderSettings.ConnectionString);
            _contactUsRepository = new Repository<ContactUs>(dataProviderSettings.ConnectionString);
            _merchandiseReturnActionRepository = new Repository<MerchandiseReturnAction>(dataProviderSettings.ConnectionString);
            _customerAction = new Repository<CustomerAction>(dataProviderSettings.ConnectionString);
            _customerActionType = new Repository<CustomerActionType>(dataProviderSettings.ConnectionString);
            _customerActionHistory = new Repository<CustomerActionHistory>(dataProviderSettings.ConnectionString);
            _customerReminderHistoryRepository = new Repository<CustomerReminderHistory>(dataProviderSettings.ConnectionString);
            _knowledgebaseArticleRepository = new Repository<KnowledgebaseArticle>(dataProviderSettings.ConnectionString);
            _knowledgebaseCategoryRepository = new Repository<KnowledgebaseCategory>(dataProviderSettings.ConnectionString);
            _popupArchive = new Repository<PopupArchive>(dataProviderSettings.ConnectionString);
            _orderTagRepository = new Repository<OrderTag>(dataProviderSettings.ConnectionString);
            _orderStatusRepository = new Repository<OrderStatus>(dataProviderSettings.ConnectionString);
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
                var mongoDBContext = new MongoDBContext(mongodb);

                var typeSearcher = _serviceProvider.GetRequiredService<ITypeSearcher>();
                var q = typeSearcher.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Grand.Domain");
                foreach (var item in q.GetTypes())
                {
                    if (item.BaseType != null && item.IsClass && item.BaseType == typeof(BaseEntity))
                        await mongoDBContext.Database().CreateCollectionAsync(item.Name, options);
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
