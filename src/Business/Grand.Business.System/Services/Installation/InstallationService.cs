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
using Grand.Domain.Courses;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Documents;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Logging;
using Grand.Domain.Messages;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Pages;
using Grand.Domain.Payments;
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
        private readonly IRepository<Campaign> _campaignRepository;
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
        private readonly IRepository<AddressAttribute> _addressAttributeRepository;
        private readonly IRepository<CustomerAttribute> _customerAttributeRepository;
        private readonly IRepository<ContactAttribute> _contactAttributeRepository;
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
        private readonly IRepository<BlogCategory> _blogCategoryRepository;
        private readonly IRepository<BlogComment> _blogCommentRepository;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<NewsItem> _newsItemRepository;
        private readonly IRepository<NewsLetterSubscription> _newslettersubscriptionRepository;
        private readonly IRepository<ShippingMethod> _shippingMethodRepository;
        private readonly IRepository<DeliveryDate> _deliveryDateRepository;
        private readonly IRepository<ActivityLog> _activityLogRepository;
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
        private readonly IRepository<PopupActive> _popupActiveRepository;
        private readonly IRepository<PickupPoint> _pickupPointRepository;
        private readonly IRepository<OutOfStockSubscription> _outOfStockSubscriptionRepository;
        private readonly IRepository<ShipmentNote> _shipmentNoteRepository;
        private readonly IRepository<PaymentTransaction> _paymentTransactionRepository;
        private readonly IRepository<QueuedEmail> _queuedEmailRepository;
        private readonly IRepository<GiftVoucher> _giftVoucherRepository;
        private readonly IRepository<CustomerReminder> _customerReminderRepository;
        private readonly IRepository<DocumentType> _documentTypeRepository;
        private readonly IRepository<Document> _documentRepository;
        private readonly IRepository<SalesEmployee> _salesRepository;
        private readonly IRepository<VendorReview> _vendorReviewRepository;
        private readonly IRepository<NewsletterCategory> _newsletterCategoryRepository;
        private readonly IRepository<InteractiveForm> _formRepository;
        private readonly IRepository<Banner> _bannerRepository;
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<CourseLevel> _courseLevelRepository;

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
            _campaignRepository = new MongoRepository<Campaign>(dataProviderSettings.ConnectionString);
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
            _addressAttributeRepository = new MongoRepository<AddressAttribute>(dataProviderSettings.ConnectionString);
            _customerAttributeRepository = new MongoRepository<CustomerAttribute>(dataProviderSettings.ConnectionString);
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
            _blogCommentRepository = new MongoRepository<BlogComment>(dataProviderSettings.ConnectionString);
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
            _shipmentNoteRepository = new MongoRepository<ShipmentNote>(dataProviderSettings.ConnectionString);
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
            _popupActiveRepository = new MongoRepository<PopupActive>(dataProviderSettings.ConnectionString);
            _pickupPointRepository = new MongoRepository<PickupPoint>(dataProviderSettings.ConnectionString);
            _outOfStockSubscriptionRepository = new MongoRepository<OutOfStockSubscription>(dataProviderSettings.ConnectionString);
            _blogCategoryRepository = new MongoRepository<BlogCategory>(dataProviderSettings.ConnectionString);
            _paymentTransactionRepository = new MongoRepository<PaymentTransaction>(dataProviderSettings.ConnectionString);
            _queuedEmailRepository = new MongoRepository<QueuedEmail>(dataProviderSettings.ConnectionString);
            _giftVoucherRepository = new MongoRepository<GiftVoucher>(dataProviderSettings.ConnectionString);
            _customerReminderRepository = new MongoRepository<CustomerReminder>(dataProviderSettings.ConnectionString);
            _documentTypeRepository = new MongoRepository<DocumentType>(dataProviderSettings.ConnectionString);
            _documentRepository = new MongoRepository<Document>(dataProviderSettings.ConnectionString);
            _salesRepository = new MongoRepository<SalesEmployee>(dataProviderSettings.ConnectionString);
            _activityLogRepository = new MongoRepository<ActivityLog>(dataProviderSettings.ConnectionString);
            _vendorReviewRepository = new MongoRepository<VendorReview>(dataProviderSettings.ConnectionString);
            _contactAttributeRepository = new MongoRepository<ContactAttribute>(dataProviderSettings.ConnectionString);
            _newsletterCategoryRepository = new MongoRepository<NewsletterCategory>(dataProviderSettings.ConnectionString);
            _formRepository = new MongoRepository<InteractiveForm>(dataProviderSettings.ConnectionString);
            _bannerRepository = new MongoRepository<Banner>(dataProviderSettings.ConnectionString);
            _courseRepository = new MongoRepository<Course>(dataProviderSettings.ConnectionString);
            _courseLevelRepository = new MongoRepository<CourseLevel>(dataProviderSettings.ConnectionString);
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

        private async Task CreateIndexes(MongoDBContext dbContext, DataSettings dataSettings)
        {
            //version
            await dbContext.CreateIndex(_versionRepository, OrderBuilder<GrandNodeVersion>.Create().Ascending(x => x.DataBaseVersion), "DataBaseVersion", true);

            //Store
            await dbContext.CreateIndex(_storeRepository, OrderBuilder<Store>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //Language
            await dbContext.CreateIndex(_lsrRepository, OrderBuilder<TranslationResource>.Create().Ascending(x => x.LanguageId).Ascending(x => x.Name), "Language");
            await dbContext.CreateIndex(_lsrRepository, OrderBuilder<TranslationResource>.Create().Ascending(x => x.Name), "ResourceName");
            await dbContext.CreateIndex(_languageRepository, OrderBuilder<Language>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //Currency
            await dbContext.CreateIndex(_currencyRepository, OrderBuilder<Currency>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //customer
            await dbContext.CreateIndex(_customerRepository, OrderBuilder<Customer>.Create().Descending(x => x.CreatedOnUtc).Ascending(x => x.Deleted), "CreatedOnUtc_1");
            await dbContext.CreateIndex(_customerRepository, OrderBuilder<Customer>.Create().Ascending(x => x.LastActivityDateUtc), "LastActivityDateUtc_1");
            await dbContext.CreateIndex(_customerRepository, OrderBuilder<Customer>.Create().Ascending(x => x.CustomerGuid), "CustomerGuid_1");
            await dbContext.CreateIndex(_customerRepository, OrderBuilder<Customer>.Create().Ascending(x => x.Email), "Email_1");
            await dbContext.CreateIndex(_customerRepository, OrderBuilder<Customer>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            await dbContext.CreateIndex(_customerGroupRepository, OrderBuilder<CustomerGroup>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_customerGroupRepository, OrderBuilder<CustomerGroup>.Create().Ascending(x => x.Name), "Name");
            await dbContext.CreateIndex(_customerGroupRepository, OrderBuilder<CustomerGroup>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "DisplayOrder_Name");

            await dbContext.CreateIndex(_vendorRepository, OrderBuilder<Vendor>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "DisplayOrder_Name");
            await dbContext.CreateIndex(_vendorReviewRepository, OrderBuilder<VendorReview>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");
            

            await dbContext.CreateIndex(_addressAttributeRepository, OrderBuilder<AddressAttribute>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_customerAttributeRepository, OrderBuilder<CustomerAttribute>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            await dbContext.CreateIndex(_customerHistoryPasswordRepository, OrderBuilder<CustomerHistoryPassword>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");
            
            //product customer group
            await dbContext.CreateIndex(_customerGroupProductRepository, OrderBuilder<CustomerGroupProduct>.Create().Ascending(x => x.CustomerGroupId).Ascending(x => x.DisplayOrder), "CustomerGroupId_DisplayOrder");
            await dbContext.CreateIndex(_customerGroupProductRepository, OrderBuilder<CustomerGroupProduct>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //customer personalize product 
            await dbContext.CreateIndex(_customerProductRepository, OrderBuilder<CustomerProduct>.Create().Ascending(x => x.CustomerId).Ascending(x => x.DisplayOrder), "CustomerProduct");
            await dbContext.CreateIndex(_customerProductRepository, OrderBuilder<CustomerProduct>.Create().Ascending(x => x.CustomerId).Ascending(x => x.ProductId), "CustomerProduct_Unique", true);
            await dbContext.CreateIndex(_customerProductRepository, OrderBuilder<CustomerProduct>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //customer product price
            await dbContext.CreateIndex(_customerProductPriceRepository, OrderBuilder<CustomerProductPrice>.Create().Ascending(x => x.CustomerId).Ascending(x => x.ProductId), "CustomerProduct", true);

            //customer tag history
            await dbContext.CreateIndex(_customerTagProductRepository, OrderBuilder<CustomerTagProduct>.Create().Ascending(x => x.Id).Ascending(x => x.DisplayOrder), "CustomerTagId_DisplayOrder");
            await dbContext.CreateIndex(_customerTagProductRepository, OrderBuilder<CustomerTagProduct>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //customer history password
            await dbContext.CreateIndex(_customerHistoryPasswordRepository, OrderBuilder<CustomerHistoryPassword>.Create().Ascending(x => x.CustomerId).Descending(x => x.CreatedOnUtc), "CustomerId");

            //customer note
            await dbContext.CreateIndex(_customerNoteRepository, OrderBuilder<CustomerNote>.Create().Ascending(x => x.CustomerId).Descending(x => x.CreatedOnUtc), "CustomerId");
            await dbContext.CreateIndex(_customerNoteRepository, OrderBuilder<CustomerNote>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //customer reminder 
            await dbContext.CreateIndex(_customerReminderRepository, OrderBuilder<CustomerReminder>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //user api
            await dbContext.CreateIndex(_userapiRepository, OrderBuilder<UserApi>.Create().Ascending(x => x.Email), "Email", true);

            //specificationAttribute
            await dbContext.CreateIndex(_specificationAttributeRepository, OrderBuilder<SpecificationAttribute>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //checkoutAttribute
            await dbContext.CreateIndex(_checkoutAttributeRepository, OrderBuilder<CheckoutAttribute>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //contact attr
            await dbContext.CreateIndex(_contactAttributeRepository, OrderBuilder<ContactAttribute>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //category
            await dbContext.CreateIndex(_categoryRepository, OrderBuilder<Category>.Create().Ascending(x => x.Published).Ascending(x => x.ShowOnHomePage).Ascending(x => x.DisplayOrder), "ShowOnHomePage_DisplayOrder_1");
            await dbContext.CreateIndex(_categoryRepository, OrderBuilder<Category>.Create().Ascending(x => x.ParentCategoryId).Ascending(x => x.Published).Ascending(x => x.DisplayOrder), "ParentCategoryId_1_DisplayOrder_1");
            await dbContext.CreateIndex(_categoryRepository, OrderBuilder<Category>.Create().Ascending(x => x.FeaturedProductsOnHomePage).Ascending(x => x.Published).Ascending(x => x.DisplayOrder), "FeaturedProductsOnHomePage_DisplayOrder_1");
            await dbContext.CreateIndex(_categoryRepository, OrderBuilder<Category>.Create().Ascending(x => x.SearchBoxDisplayOrder), "SearchBoxDisplayOrder");
            await dbContext.CreateIndex(_categoryRepository, OrderBuilder<Category>.Create().Ascending(x => x.ParentCategoryId).Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "ParentCategoryId_1_DisplayOrder_1_Name_1");

            await dbContext.CreateIndex(_categoryRepository, OrderBuilder<Category>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder_1");

            //collection
            await dbContext.CreateIndex(_collectionRepository, OrderBuilder<Collection>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "DisplayOrder_Name");
            await dbContext.CreateIndex(_collectionRepository, OrderBuilder<Collection>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_collectionRepository, OrderBuilder<Collection>.Create().Ascending(x => x.ShowOnHomePage).Ascending(x => x.Published).Ascending(x => x.DisplayOrder), "ShowOnHomePage_DisplayOrder_1");
            await dbContext.CreateIndex(_collectionRepository, OrderBuilder<Collection>.Create().Ascending(x => x.AppliedDiscounts), "AppliedDiscounts");

            //brands
            await dbContext.CreateIndex(_brandRepository, OrderBuilder<Brand>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "DisplayOrder_Name");
            await dbContext.CreateIndex(_brandRepository, OrderBuilder<Brand>.Create().Ascending(x => x.ShowOnHomePage).Ascending(x => x.Published).Ascending(x => x.DisplayOrder), "ShowOnHomePage_DisplayOrder_1");
            await dbContext.CreateIndex(_brandRepository, OrderBuilder<Brand>.Create().Ascending(x => x.AppliedDiscounts), "AppliedDiscounts");

            //Product
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.MarkAsNew).Ascending(x => x.CreatedOnUtc), "MarkAsNew_1_CreatedOnUtc_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.ShowOnHomePage).Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "ShowOnHomePage_1_Published_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.BestSeller).Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "ShowOnBestSeller_1_Published_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.ParentGroupedProductId).Ascending(x => x.DisplayOrder), "ParentGroupedProductId_1_DisplayOrder_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.ProductTags).Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Name), "ProductTags._id_1_Name_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Name), "Name_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending("ProductCategories.DisplayOrder"), "CategoryId_1_DisplayOrder_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.DisplayOrderCategory).Ascending("ProductCategories.CategoryId"), "ProductCategories.CategoryId_1_OrderCategory_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Name).Ascending("ProductCategories.CategoryId"), "ProductCategories.CategoryId_1_Name_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Price).Ascending("ProductCategories.CategoryId"), "ProductCategories.CategoryId_1_Price_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Sold).Ascending("ProductCategories.CategoryId"), "ProductCategories.CategoryId_1_Sold_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending("ProductCategories.CategoryId").Ascending("ProductCategories.IsFeaturedProduct"), "ProductCategories.CategoryId_1_IsFeaturedProduct_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending("ProductCollections.CollectionId").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.DisplayOrderCollection), "ProductCollections.CollectionId_1_OrderCategory_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending("ProductCollections.CollectionId").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Name), "ProductCollections.CollectionId_1_Name_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending("ProductCollections.CollectionId").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.Sold), "ProductCollections.CollectionId_1_Sold_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending("ProductCollections.CollectionId").Ascending("ProductCollections.IsFeaturedProduct").Ascending(x => x.Published).Ascending(x => x.VisibleIndividually), "ProductCollections.CollectionId_1_IsFeaturedProduct_1");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending(x => x.BrandId).Ascending(x => x.DisplayOrderBrand), "ProductBrand");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Published).Ascending(x => x.VisibleIndividually).Ascending("ProductSpecificationAttributes.SpecificationAttributeOptionId").Ascending("ProductSpecificationAttributes.AllowFiltering"), "ProductSpecificationAttributes");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "DisplayOrder_Name");

            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.DisplayOrderCategory), "DisplayOrderCategory");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.DisplayOrderBrand), "DisplayOrderBrand");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.DisplayOrderCollection), "DisplayOrderCollection");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Price), "Price");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.OnSale), "OnSale");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Viewed), "Viewed");
            await dbContext.CreateIndex(_productRepository, OrderBuilder<Product>.Create().Ascending(x => x.Sold), "Sold");

            //product attribute
            await dbContext.CreateIndex(_productAttributeRepository, OrderBuilder<ProductAttribute>.Create().Ascending(x => x.Name), "Name");

            //Product layout
            await dbContext.CreateIndex(_productLayoutRepository, OrderBuilder<ProductLayout>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_categoryLayoutRepository, OrderBuilder<CategoryLayout>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_brandLayoutRepository, OrderBuilder<BrandLayout>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_collectionLayoutRepository, OrderBuilder<CollectionLayout>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_pageLayoutRepository, OrderBuilder<PageLayout>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //Delivery date
            await dbContext.CreateIndex(_deliveryDateRepository, OrderBuilder<DeliveryDate>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //Tax
            await dbContext.CreateIndex(_taxCategoryRepository, OrderBuilder<TaxCategory>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //Measure
            await dbContext.CreateIndex(_measureWeightRepository, OrderBuilder<MeasureWeight>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_measureUnitRepository, OrderBuilder<MeasureUnit>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_measureDimensionRepository, OrderBuilder<MeasureDimension>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //productreseration
            await dbContext.CreateIndex(_productReservationRepository, OrderBuilder<ProductReservation>.Create().Ascending(x => x.ProductId).Ascending(x => x.Date), "ProductReservation");
            await dbContext.CreateIndex(_productReservationRepository, OrderBuilder<ProductReservation>.Create().Ascending(x => x.Date), "Date");

            //bid
            await dbContext.CreateIndex(_bidRepository, OrderBuilder<Bid>.Create().Ascending(x => x.ProductId).Ascending(x => x.CustomerId).Descending(x => x.Date), "ProductCustomer");
            await dbContext.CreateIndex(_bidRepository, OrderBuilder<Bid>.Create().Ascending(x => x.ProductId).Descending(x => x.Date), "ProductDate");
            await dbContext.CreateIndex(_bidRepository, OrderBuilder<Bid>.Create().Descending(x => x.Date), "Date");

            //ProductReview
            await dbContext.CreateIndex(_productReviewRepository, OrderBuilder<ProductReview>.Create().Ascending(x => x.ProductId).Ascending(x => x.CreatedOnUtc), "ProductId");
            await dbContext.CreateIndex(_productReviewRepository, OrderBuilder<ProductReview>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //Recently Viewed Products
            await dbContext.CreateIndex(_recentlyViewedProductRepository, OrderBuilder<RecentlyViewedProduct>.Create().Ascending(x => x.CustomerId).Ascending(x => x.ProductId).Descending(x => x.CreatedOnUtc), "CustomerId.ProductId");
            await dbContext.CreateIndex(_recentlyViewedProductRepository, OrderBuilder<RecentlyViewedProduct>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //Product also purchased
            await dbContext.CreateIndex(_productalsopurchasedRepository, OrderBuilder<ProductAlsoPurchased>.Create().Ascending(x => x.ProductId), "ProductId");

            //url record
            await dbContext.CreateIndex(_entityUrlRepository, OrderBuilder<EntityUrl>.Create().Ascending(x => x.Slug), "Slug");
            await dbContext.CreateIndex(_entityUrlRepository, OrderBuilder<EntityUrl>.Create().Ascending(x => x.Slug).Ascending(x => x.IsActive), "SlugActive");
            await dbContext.CreateIndex(_entityUrlRepository, OrderBuilder<EntityUrl>.Create().Ascending(x => x.EntityId).Ascending(x => x.EntityName).Ascending(x => x.LanguageId).Ascending(x => x.IsActive), "UrlEntity");
            await dbContext.CreateIndex(_entityUrlRepository, OrderBuilder<EntityUrl>.Create().Ascending(x => x.IsActive), "IsActive");

            //message template
            await dbContext.CreateIndex(_messageTemplateRepository, OrderBuilder<MessageTemplate>.Create().Ascending(x => x.Name), "Name");

            // Country and Stateprovince
            await dbContext.CreateIndex(_countryRepository, OrderBuilder<Country>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //discount
            await dbContext.CreateIndex(_discountRepository, OrderBuilder<Discount>.Create().Ascending(x => x.Name), "Name)");

            await dbContext.CreateIndex(_discountCouponRepository, OrderBuilder<DiscountCoupon>.Create().Ascending(x => x.CouponCode), "CouponCode", true);
            await dbContext.CreateIndex(_discountCouponRepository, OrderBuilder<DiscountCoupon>.Create().Ascending(x => x.DiscountId), "DiscountId");

            await dbContext.CreateIndex(_discountusageRepository, OrderBuilder<DiscountUsageHistory>.Create().Ascending(x => x.CustomerId), "CustomerId");
            await dbContext.CreateIndex(_discountusageRepository, OrderBuilder<DiscountUsageHistory>.Create().Ascending(x => x.DiscountId), "DiscountId");
            await dbContext.CreateIndex(_discountusageRepository, OrderBuilder<DiscountUsageHistory>.Create().Ascending(x => x.OrderId), "OrderId");

            await dbContext.CreateIndex(_discountusageRepository, OrderBuilder<DiscountUsageHistory>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //blog 
            await dbContext.CreateIndex(_blogPostRepository, OrderBuilder<BlogPost>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");
            await dbContext.CreateIndex(_blogCategoryRepository, OrderBuilder<BlogCategory>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_blogCommentRepository, OrderBuilder<BlogComment>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");
            
            //knowledgebase
            await dbContext.CreateIndex(_knowledgebaseArticleRepository, OrderBuilder<KnowledgebaseArticle>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_knowledgebaseCategoryRepository, OrderBuilder<KnowledgebaseCategory>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_knowledgebaseCategoryRepository, OrderBuilder<KnowledgebaseCategory>.Create().Ascending(x => x.ParentCategoryId).Ascending(x=>x.DisplayOrder), "ParentCategoryId_DisplayOrder");

            //page
            await dbContext.CreateIndex(_pageRepository, OrderBuilder<Page>.Create().Ascending(x => x.SystemName), "SystemName");

            //news
            await dbContext.CreateIndex(_newsItemRepository, OrderBuilder<NewsItem>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //newsletter
            await dbContext.CreateIndex(_newslettersubscriptionRepository, OrderBuilder<NewsLetterSubscription>.Create().Ascending(x => x.CustomerId), "CustomerId");
            await dbContext.CreateIndex(_newslettersubscriptionRepository, OrderBuilder<NewsLetterSubscription>.Create().Ascending(x => x.Email), "Email");

            //Log
            await dbContext.CreateIndex(_logRepository, OrderBuilder<Log>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //Campaign 
            await dbContext.CreateIndex(_campaignRepository, OrderBuilder<Campaign>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");
            await dbContext.CreateIndex(_campaignHistoryRepository, OrderBuilder<CampaignHistory>.Create().Ascending(x => x.CampaignId).Descending(x => x.CreatedDateUtc), "CampaignId");
            
            //loyalty points
            await dbContext.CreateIndex(_loyaltypointshistoryRepository, OrderBuilder<LoyaltyPointsHistory>.Create().Ascending(x => x.CustomerId), "CustomerId");
            await dbContext.CreateIndex(_loyaltypointshistoryRepository, OrderBuilder<LoyaltyPointsHistory>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //giftvoucher
            await dbContext.CreateIndex(_giftVoucherRepository, OrderBuilder<GiftVoucher>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //search term
            await dbContext.CreateIndex(_searchtermRepository, OrderBuilder<SearchTerm>.Create().Descending(x => x.Count), "Count");

            //setting
            await dbContext.CreateIndex(_settingRepository, OrderBuilder<Setting>.Create().Ascending(x => x.Name), "Name");

            //shipment
            await dbContext.CreateIndex(_shipmentRepository, OrderBuilder<Shipment>.Create().Ascending(x => x.ShipmentNumber), "ShipmentNumber", true);
            await dbContext.CreateIndex(_shipmentRepository, OrderBuilder<Shipment>.Create().Ascending(x => x.OrderId), "OrderId");
            await dbContext.CreateIndex(_shipmentRepository, OrderBuilder<Shipment>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            await dbContext.CreateIndex(_shipmentNoteRepository, OrderBuilder<ShipmentNote>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //order
            await dbContext.CreateIndex(_orderRepository, OrderBuilder<Order>.Create().Ascending(x => x.CustomerId).Descending(x => x.CreatedOnUtc), "CustomerId_1_CreatedOnUtc_-1");
            await dbContext.CreateIndex(_orderRepository, OrderBuilder<Order>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc_-1");
            await dbContext.CreateIndex(_orderRepository, OrderBuilder<Order>.Create().Descending(x => x.OrderNumber), "OrderNumber");
            await dbContext.CreateIndex(_orderRepository, OrderBuilder<Order>.Create().Ascending(x => x.Code), "OrderCode");
            await dbContext.CreateIndex(_orderRepository, OrderBuilder<Order>.Create().Ascending("OrderItems.ProductId"), "OrderItemProductId");
            await dbContext.CreateIndex(_orderRepository, OrderBuilder<Order>.Create().Ascending("OrderItems.Id"), "OrderItems._id");

            await dbContext.CreateIndex(_orderStatusRepository, OrderBuilder<OrderStatus>.Create().Ascending(x => x.StatusId), "StatusId", true);

            await dbContext.CreateIndex(_orderNoteRepository, OrderBuilder<OrderNote>.Create().Ascending(x => x.OrderId).Descending(x => x.CreatedOnUtc), "OrderId_Created");
            await dbContext.CreateIndex(_orderNoteRepository, OrderBuilder<OrderNote>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //payments 
            await dbContext.CreateIndex(_paymentTransactionRepository, OrderBuilder<PaymentTransaction>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //queuemail
            await dbContext.CreateIndex(_queuedEmailRepository, OrderBuilder<QueuedEmail>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");
            await dbContext.CreateIndex(_queuedEmailRepository, OrderBuilder<QueuedEmail>.Create().Descending(x => x.PriorityId).Ascending(x => x.CreatedOnUtc), "PriorityId_CreatedOnUtc");

            //permision
            await dbContext.CreateIndex(_permissionRepository, OrderBuilder<Permission>.Create().Ascending(x => x.SystemName), "SystemName", true);
            await dbContext.CreateIndex(_permissionRepository, OrderBuilder<Permission>.Create().Ascending(x => x.Name), "Name", false);

            await dbContext.CreateIndex(_permissionAction, OrderBuilder<PermissionAction>.Create().Ascending(x => x.SystemName), "SystemName");

            //externalauth
            await dbContext.CreateIndex(_externalAuthenticationRepository, OrderBuilder<ExternalAuthentication>.Create().Ascending(x => x.CustomerId), "CustomerId");

            //merchandise return
            await dbContext.CreateIndex(_merchandiseReturnRepository, OrderBuilder<MerchandiseReturn>.Create().Ascending(x => x.ReturnNumber), "ReturnNumber", true);
            await dbContext.CreateIndex(_merchandiseReturnRepository, OrderBuilder<MerchandiseReturn>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc", false);
            await dbContext.CreateIndex(_merchandiseReturnActionRepository, OrderBuilder<MerchandiseReturnAction>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_merchandiseReturnReasonRepository, OrderBuilder<MerchandiseReturnReason>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            await dbContext.CreateIndex(_merchandiseReturnNoteRepository, OrderBuilder<MerchandiseReturnNote>.Create().Ascending(x => x.MerchandiseReturnId).Descending(x => x.CreatedOnUtc), "MerchandiseReturnId_CreatedOn");
            await dbContext.CreateIndex(_merchandiseReturnNoteRepository, OrderBuilder<MerchandiseReturnNote>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //contactus
            await dbContext.CreateIndex(_contactUsRepository, OrderBuilder<ContactUs>.Create().Ascending(x => x.Email), "Email");
            await dbContext.CreateIndex(_contactUsRepository, OrderBuilder<ContactUs>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //customer action
            await dbContext.CreateIndex(_customerAction, OrderBuilder<CustomerAction>.Create().Ascending(x => x.ActionTypeId), "ActionTypeId");
            await dbContext.CreateIndex(_customerActionHistory, OrderBuilder<CustomerActionHistory>.Create().Ascending(x => x.CustomerId).Ascending(x => x.CustomerActionId), "Customer_Action");

            //banner
            await dbContext.CreateIndex(_popupArchive, OrderBuilder<PopupArchive>.Create().Ascending(x => x.CustomerActionId), "CustomerActionId");
            await dbContext.CreateIndex(_popupActiveRepository, OrderBuilder<PopupActive>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //customer reminder
            await dbContext.CreateIndex(_customerReminderHistoryRepository, OrderBuilder<CustomerReminderHistory>.Create().Ascending(x => x.CustomerId).Ascending(x => x.CustomerReminderId), "CustomerId");

            //page
            await dbContext.CreateIndex(_pageRepository, OrderBuilder<Page>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.SystemName), "DisplayOrder_SystemName");

            //customeractivity 
            await dbContext.CreateIndex(_activityLogTypeRepository, OrderBuilder<ActivityLogType>.Create().Ascending(x => x.Name), "Name");
            await dbContext.CreateIndex(_activityLogRepository, OrderBuilder<ActivityLog>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //warehouse
            await dbContext.CreateIndex(_warehouseRepository, OrderBuilder<Warehouse>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //shipping method
            await dbContext.CreateIndex(_shippingMethodRepository, OrderBuilder<ShippingMethod>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //order status
            await dbContext.CreateIndex(_orderStatusRepository, OrderBuilder<OrderStatus>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //country
            await dbContext.CreateIndex(_countryRepository, OrderBuilder<Country>.Create().Ascending(x => x.DisplayOrder).Ascending(x => x.Name), "DisplayOrder_Name");

            //picpup points
            await dbContext.CreateIndex(_pickupPointRepository, OrderBuilder<PickupPoint>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //outofstock
            await dbContext.CreateIndex(_outOfStockSubscriptionRepository, OrderBuilder<OutOfStockSubscription>.Create().Descending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //document
            await dbContext.CreateIndex(_documentTypeRepository, OrderBuilder<DocumentType>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            await dbContext.CreateIndex(_documentTypeRepository, OrderBuilder<DocumentType>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //sales
            await dbContext.CreateIndex(_salesRepository, OrderBuilder<SalesEmployee>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //newslettercategory
            await dbContext.CreateIndex(_newsletterCategoryRepository, OrderBuilder<NewsletterCategory>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //interactive form
            await dbContext.CreateIndex(_formRepository, OrderBuilder<InteractiveForm>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //banner
            await dbContext.CreateIndex(_bannerRepository, OrderBuilder<Banner>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");

            //course
            await dbContext.CreateIndex(_courseRepository, OrderBuilder<Course>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");
            await dbContext.CreateIndex(_courseLevelRepository, OrderBuilder<CourseLevel>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");
            
            //if(dataSettings.DbProvider == DbProvider.CosmosDB)
            //{
            //    //
            //    //db.fs.chunks.createIndex({'n': 1})
            //    //To Fix problem with download files from GridFSBucket
            //    //
            //}

        }

        private async Task CreateTables(string local)
        {
            try
            {
                var dataSettings = DataSettingsManager.LoadSettings();
                var dbContext = new MongoDBContext(dataSettings.ConnectionString);

                var typeSearcher = _serviceProvider.GetRequiredService<ITypeSearcher>();
                var q = typeSearcher.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Grand.Domain");

                foreach (var item in q.GetTypes())
                {
                    if (item.BaseType != null && item.IsClass && item.BaseType == typeof(BaseEntity))
                        await dbContext.CreateTable(item.Name, local);
                }
                await CreateIndexes(dbContext, dataSettings);

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
