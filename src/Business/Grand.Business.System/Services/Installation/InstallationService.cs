using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Business.Core.Utilities.System;
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
using Grand.Infrastructure.TypeSearch;
using Grand.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IRepository<RecentlyViewedProduct> _recentlyViewedProductRepository;
        private readonly IRepository<KnowledgebaseArticle> _knowledgebaseArticleRepository;
        private readonly IRepository<KnowledgebaseCategory> _knowledgebaseCategoryRepository;
        private readonly IRepository<OrderTag> _orderTagRepository;
        private readonly IRepository<OrderStatus> _orderStatusRepository;
        private readonly IRepository<PickupPoint> _pickupPointRepository;
        private readonly IRepository<OutOfStockSubscription> _outOfStockSubscriptionRepository;
        private readonly IRepository<ShipmentNote> _shipmentNoteRepository;
        private readonly IRepository<PaymentTransaction> _paymentTransactionRepository;
        private readonly IRepository<QueuedEmail> _queuedEmailRepository;
        private readonly IRepository<GiftVoucher> _giftVoucherRepository;
        private readonly IRepository<DocumentType> _documentTypeRepository;
        private readonly IRepository<Document> _documentRepository;
        private readonly IRepository<SalesEmployee> _salesRepository;
        private readonly IRepository<VendorReview> _vendorReviewRepository;
        private readonly IRepository<NewsletterCategory> _newsletterCategoryRepository;
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<CourseLevel> _courseLevelRepository;
        private readonly IRepository<RobotsTxt> _robotsTxtRepository;


        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Ctor

        public InstallationService(
            IServiceProvider serviceProvider,
            IWebHostEnvironment webHostEnvironment,
            IRepository<GrandNodeVersion> versionRepository,
            IRepository<AdminSiteMap> adminRepository,
            IRepository<Bid> bidRepository,
            IRepository<Affiliate> affiliateRepository,
            IRepository<CampaignHistory> campaignHistoryRepository,
            IRepository<Campaign> campaignRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderNote> orderNoteRepository,
            IRepository<MerchandiseReturn> merchandiseReturnRepository,
            IRepository<MerchandiseReturnNote> merchandiseReturnNoteRepository,
            IRepository<Store> storeRepository,
            IRepository<MeasureDimension> measureDimensionRepository,
            IRepository<MeasureWeight> measureWeightRepository,
            IRepository<MeasureUnit> measureUnitRepository,
            IRepository<TaxCategory> taxCategoryRepository,
            IRepository<Language> languageRepository,
            IRepository<TranslationResource> lsrRepository,
            IRepository<Log> logRepository,
            IRepository<Currency> currencyRepository,
            IRepository<Customer> customerRepository,
            IRepository<CustomerGroup> customerGroupRepository,
            IRepository<CustomerGroupProduct> customerGroupProductRepository,
            IRepository<CustomerProduct> customerProductRepository,
            IRepository<CustomerProductPrice> customerProductPriceRepository,
            IRepository<CustomerTagProduct> customerTagProductRepository,
            IRepository<CustomerHistoryPassword> customerHistoryPasswordRepository,
            IRepository<CustomerNote> customerNoteRepository,
            IRepository<UserApi> userapiRepository,
            IRepository<SpecificationAttribute> specificationAttributeRepository,
            IRepository<CheckoutAttribute> checkoutAttributeRepository,
            IRepository<ProductAttribute> productAttributeRepository,
            IRepository<AddressAttribute> addressAttributeRepository,
            IRepository<CustomerAttribute> customerAttributeRepository,
            IRepository<ContactAttribute> contactAttributeRepository,
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<Collection> collectionRepository,
            IRepository<Product> productRepository,
            IRepository<ProductReservation> productReservationRepository,
            IRepository<ProductAlsoPurchased> productalsopurchasedRepository,
            IRepository<EntityUrl> entityUrlRepository,
            IRepository<EmailAccount> emailAccountRepository,
            IRepository<MessageTemplate> messageTemplateRepository,
            IRepository<Country> countryRepository,
            IRepository<Discount> discountRepository,
            IRepository<DiscountCoupon> discountCouponRepository,
            IRepository<DiscountUsageHistory> discountusageRepository,
            IRepository<BlogPost> blogPostRepository,
            IRepository<BlogCategory> blogCategoryRepository,
            IRepository<BlogComment> blogCommentRepository,
            IRepository<Page> pageRepository,
            IRepository<NewsItem> newsItemRepository,
            IRepository<NewsLetterSubscription> newslettersubscriptionRepository,
            IRepository<ShippingMethod> shippingMethodRepository,
            IRepository<DeliveryDate> deliveryDateRepository,
            IRepository<ActivityLog> activityLogRepository,
            IRepository<ActivityLogType> activityLogTypeRepository,
            IRepository<ProductTag> productTagRepository,
            IRepository<ProductReview> productReviewRepository,
            IRepository<ProductLayout> productLayoutRepository,
            IRepository<CategoryLayout> categoryLayoutRepository,
            IRepository<BrandLayout> brandLayoutRepository,
            IRepository<CollectionLayout> collectionLayoutRepository,
            IRepository<PageLayout> pageLayoutRepository,
            IRepository<ScheduleTask> scheduleTaskRepository,
            IRepository<LoyaltyPointsHistory> loyaltypointshistoryRepository,
            IRepository<SearchTerm> searchtermRepository,
            IRepository<Setting> settingRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<Warehouse> warehouseRepository,
            IRepository<PickupPoint> pickupPointsRepository,
            IRepository<Permission> permissionRepository,
            IRepository<PermissionAction> permissionAction,
            IRepository<ExternalAuthentication> externalAuthenticationRepository,
            IRepository<MerchandiseReturnReason> merchandiseReturnReasonRepository,
            IRepository<MerchandiseReturnAction> merchandiseReturnActionRepository,
            IRepository<ContactUs> contactUsRepository,
            IRepository<RecentlyViewedProduct> recentlyViewedProductRepository,
            IRepository<KnowledgebaseArticle> knowledgebaseArticleRepository,
            IRepository<KnowledgebaseCategory> knowledgebaseCategoryRepository,
            IRepository<OrderTag> orderTagRepository,
            IRepository<OrderStatus> orderStatusRepository,
            IRepository<PickupPoint> pickupPointRepository,
            IRepository<OutOfStockSubscription> outOfStockSubscriptionRepository,
            IRepository<ShipmentNote> shipmentNoteRepository,
            IRepository<PaymentTransaction> paymentTransactionRepository,
            IRepository<QueuedEmail> queuedEmailRepository,
            IRepository<GiftVoucher> giftVoucherRepository,
            IRepository<DocumentType> documentTypeRepository,
            IRepository<Document> documentRepository,
            IRepository<SalesEmployee> salesRepository,
            IRepository<VendorReview> vendorReviewRepository,
            IRepository<NewsletterCategory> newsletterCategoryRepository,
            IRepository<Course> courseRepository,
            IRepository<CourseLevel> courseLevelRepository,
            IRepository<RobotsTxt> robotsTxtRepository)
        {

            _versionRepository = versionRepository;
            _adminRepository = adminRepository;
            _bidRepository = bidRepository;
            _affiliateRepository = affiliateRepository;
            _campaignHistoryRepository = campaignHistoryRepository;
            _campaignRepository = campaignRepository;
            _orderRepository = orderRepository;
            _orderNoteRepository = orderNoteRepository;
            _storeRepository = storeRepository;
            _measureDimensionRepository = measureDimensionRepository;
            _measureWeightRepository = measureWeightRepository;
            _measureUnitRepository = measureUnitRepository;
            _taxCategoryRepository = taxCategoryRepository;
            _languageRepository = languageRepository;
            _lsrRepository = lsrRepository;
            _logRepository = logRepository;
            _currencyRepository = currencyRepository;
            _customerRepository = customerRepository;
            _customerGroupRepository = customerGroupRepository;
            _customerProductRepository = customerProductRepository;
            _customerProductPriceRepository = customerProductPriceRepository;
            _customerGroupProductRepository = customerGroupProductRepository;
            _customerTagProductRepository = customerTagProductRepository;
            _customerHistoryPasswordRepository = customerHistoryPasswordRepository;
            _customerNoteRepository = customerNoteRepository;
            _userapiRepository = userapiRepository;
            _specificationAttributeRepository = specificationAttributeRepository;
            _checkoutAttributeRepository = checkoutAttributeRepository;
            _productAttributeRepository = productAttributeRepository;
            _addressAttributeRepository = addressAttributeRepository;
            _customerAttributeRepository = customerAttributeRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _collectionRepository = collectionRepository;
            _productRepository = productRepository;
            _productReservationRepository = productReservationRepository;
            _productalsopurchasedRepository = productalsopurchasedRepository;
            _entityUrlRepository = entityUrlRepository;
            _emailAccountRepository = emailAccountRepository;
            _messageTemplateRepository = messageTemplateRepository;
            _countryRepository = countryRepository;
            _discountRepository = discountRepository;
            _discountCouponRepository = discountCouponRepository;
            _blogPostRepository = blogPostRepository;
            _blogCommentRepository = blogCommentRepository;
            _pageRepository = pageRepository;
            _productReviewRepository = productReviewRepository;
            _newsItemRepository = newsItemRepository;
            _newslettersubscriptionRepository = newslettersubscriptionRepository;
            _shippingMethodRepository = shippingMethodRepository;
            _deliveryDateRepository = deliveryDateRepository;
            _activityLogTypeRepository = activityLogTypeRepository;
            _productTagRepository = productTagRepository;
            _productLayoutRepository = productLayoutRepository;
            _recentlyViewedProductRepository = recentlyViewedProductRepository;
            _categoryLayoutRepository = categoryLayoutRepository;
            _brandLayoutRepository = brandLayoutRepository;
            _collectionLayoutRepository = collectionLayoutRepository;
            _pageLayoutRepository = pageLayoutRepository;
            _scheduleTaskRepository = scheduleTaskRepository;
            _merchandiseReturnRepository = merchandiseReturnRepository;
            _merchandiseReturnNoteRepository = merchandiseReturnNoteRepository;
            _loyaltypointshistoryRepository = loyaltypointshistoryRepository;
            _searchtermRepository = searchtermRepository;
            _settingRepository = settingRepository;
            _shipmentRepository = shipmentRepository;
            _shipmentNoteRepository = shipmentNoteRepository;
            _warehouseRepository = warehouseRepository;
            _pickupPointsRepository = pickupPointsRepository;
            _permissionRepository = permissionRepository;
            _permissionAction = permissionAction;
            _vendorRepository = vendorRepository;
            _externalAuthenticationRepository = externalAuthenticationRepository;
            _discountusageRepository = discountusageRepository;
            _merchandiseReturnReasonRepository = merchandiseReturnReasonRepository;
            _contactUsRepository = contactUsRepository;
            _merchandiseReturnActionRepository = merchandiseReturnActionRepository;
            _knowledgebaseArticleRepository = knowledgebaseArticleRepository;
            _knowledgebaseCategoryRepository = knowledgebaseCategoryRepository;
            _orderTagRepository = orderTagRepository;
            _orderStatusRepository = orderStatusRepository;
            _pickupPointRepository = pickupPointRepository;
            _outOfStockSubscriptionRepository = outOfStockSubscriptionRepository;
            _blogCategoryRepository = blogCategoryRepository;
            _paymentTransactionRepository = paymentTransactionRepository;
            _queuedEmailRepository = queuedEmailRepository;
            _giftVoucherRepository = giftVoucherRepository;
            _documentTypeRepository = documentTypeRepository;
            _documentRepository = documentRepository;
            _salesRepository = salesRepository;
            _activityLogRepository = activityLogRepository;
            _vendorReviewRepository = vendorReviewRepository;
            _contactAttributeRepository = contactAttributeRepository;
            _newsletterCategoryRepository = newsletterCategoryRepository;
            _courseRepository = courseRepository;
            _courseLevelRepository = courseLevelRepository;
            _robotsTxtRepository = robotsTxtRepository;
            _hostingEnvironment = webHostEnvironment;
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
            await customerManagerService.ChangePassword(new ChangePasswordRequest(defaultUserEmail, PasswordFormat.Hashed, defaultUserPassword));
        }

        private async Task CreateIndexes(IDatabaseContext dbContext, DataSettings dataSettings)
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
            await dbContext.CreateIndex(_knowledgebaseCategoryRepository, OrderBuilder<KnowledgebaseCategory>.Create().Ascending(x => x.ParentCategoryId).Ascending(x => x.DisplayOrder), "ParentCategoryId_DisplayOrder");

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

            //course
            await dbContext.CreateIndex(_courseRepository, OrderBuilder<Course>.Create().Ascending(x => x.CreatedOnUtc), "CreatedOnUtc");
            await dbContext.CreateIndex(_courseLevelRepository, OrderBuilder<CourseLevel>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

            //admin site map
            await dbContext.CreateIndex(_adminRepository, OrderBuilder<AdminSiteMap>.Create().Ascending(x => x.DisplayOrder), "DisplayOrder");

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
                var dataSettings = DataSettingsManager.LoadSettings(reloadSettings: true);
                var dbContext = _serviceProvider.GetRequiredService<IDatabaseContext>();
                dbContext.SetConnection(dataSettings.ConnectionString);

                if (dbContext.InstallProcessCreateTable)
                {
                    var typeSearcher = _serviceProvider.GetRequiredService<ITypeSearcher>();
                    var q = typeSearcher.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Grand.Domain");

                    foreach (var item in q.GetTypes())
                    {
                        if (item.BaseType != null && item.IsClass && item.BaseType == typeof(BaseEntity))
                            await dbContext.CreateTable(item.Name, local);
                    }
                }

                if (dbContext.InstallProcessCreateIndex)
                    await CreateIndexes(dbContext, dataSettings);

            }
            catch (Exception ex)
            {
                throw new GrandException(ex.Message);
            }
        }

        #endregion

        #region Methods


        public virtual async Task InstallData(string httpscheme, HostString host, string defaultUserEmail,
            string defaultUserPassword, string collation, bool installSampleData = true, string companyName = "", string companyAddress = "",
            string companyPhoneNumber = "", string companyEmail = "")
        {
            defaultUserEmail = defaultUserEmail.ToLower();

            await CreateTables(collation);
            await InstallVersion();
            await InstallMenuAdminSiteMap();
            await InstallStores(httpscheme, host, companyName, companyAddress, companyPhoneNumber, companyEmail);
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
