using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Business.System.Utilities;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Logging;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.ExportImport
{
    /// <summary>
    /// Export manager
    /// </summary>
    public partial class ExportManager : IExportManager
    {
        #region Fields

        private readonly IPictureService _pictureService;
        private readonly IServiceProvider _serviceProvider;
        #endregion

        #region Ctor

        public ExportManager(
            IPictureService pictureService,
            IServiceProvider serviceProvider)
        {
            _pictureService = pictureService;
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Export collections to XLSX
        /// </summary>
        /// <param name="brands">Brands</param>
        public virtual byte[] ExportBrandsToXlsx(IEnumerable<Brand> brands)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Brand>("Id", p => p.Id),
                new PropertyByName<Brand>("Name", p => p.Name),
                new PropertyByName<Brand>("Description", p => p.Description),
                new PropertyByName<Brand>("BrandLayoutId", p => p.BrandLayoutId),
                new PropertyByName<Brand>("MetaKeywords", p => p.MetaKeywords),
                new PropertyByName<Brand>("MetaDescription", p => p.MetaDescription),
                new PropertyByName<Brand>("MetaTitle", p => p.MetaTitle),
                new PropertyByName<Brand>("SeName", p => p.SeName),
                new PropertyByName<Brand>("Picture", p => GetPictures(p.PictureId).Result),
                new PropertyByName<Brand>("PageSize", p => p.PageSize),
                new PropertyByName<Brand>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize),
                new PropertyByName<Brand>("PageSizeOptions", p => p.PageSizeOptions),
                new PropertyByName<Brand>("Published", p => p.Published),
                new PropertyByName<Brand>("DisplayOrder", p => p.DisplayOrder)
            };

            return ExportToXlsx(properties, brands);
        }
        /// <summary>
        /// Export collections to XLSX
        /// </summary>
        /// <param name="brands">Brands</param>
        public virtual byte[] ExportCollectionsToXlsx(IEnumerable<Collection> collections)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Collection>("Id", p => p.Id),
                new PropertyByName<Collection>("Name", p => p.Name),
                new PropertyByName<Collection>("Description", p => p.Description),
                new PropertyByName<Collection>("CollectionLayoutId", p => p.CollectionLayoutId),
                new PropertyByName<Collection>("MetaKeywords", p => p.MetaKeywords),
                new PropertyByName<Collection>("MetaDescription", p => p.MetaDescription),
                new PropertyByName<Collection>("MetaTitle", p => p.MetaTitle),
                new PropertyByName<Collection>("SeName", p => p.SeName),
                new PropertyByName<Collection>("Picture", p => GetPictures(p.PictureId).Result),
                new PropertyByName<Collection>("PageSize", p => p.PageSize),
                new PropertyByName<Collection>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize),
                new PropertyByName<Collection>("PageSizeOptions", p => p.PageSizeOptions),
                new PropertyByName<Collection>("Published", p => p.Published),
                new PropertyByName<Collection>("DisplayOrder", p => p.DisplayOrder)
            };

            return ExportToXlsx(properties, collections);
        }

        /// <summary>
        /// Export categories to XLSX
        /// </summary>
        /// <param name="categories">Categories</param>
        public virtual byte[] ExportCategoriesToXlsx(IEnumerable<Category> categories)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Category>("Id", p => p.Id),
                new PropertyByName<Category>("Name", p => p.Name),
                new PropertyByName<Category>("Description", p => p.Description),
                new PropertyByName<Category>("CategoryLayoutId", p => p.CategoryLayoutId),
                new PropertyByName<Category>("MetaKeywords", p => p.MetaKeywords),
                new PropertyByName<Category>("MetaDescription", p => p.MetaDescription),
                new PropertyByName<Category>("MetaTitle", p => p.MetaTitle),
                new PropertyByName<Category>("SeName", p => p.SeName),
                new PropertyByName<Category>("ParentCategoryId", p => p.ParentCategoryId),
                new PropertyByName<Category>("Picture", p => GetPictures(p.PictureId).Result),
                new PropertyByName<Category>("PageSize", p => p.PageSize),
                new PropertyByName<Category>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize),
                new PropertyByName<Category>("PageSizeOptions", p => p.PageSizeOptions),
                new PropertyByName<Category>("ShowOnHomePage", p => p.ShowOnHomePage),
                new PropertyByName<Category>("IncludeInMenu", p => p.IncludeInMenu),
                new PropertyByName<Category>("Published", p => p.Published),
                new PropertyByName<Category>("Flag", p => p.Flag),
                new PropertyByName<Category>("FlagStyle", p => p.FlagStyle),
                new PropertyByName<Category>("Icon", p => p.Icon),
                new PropertyByName<Category>("DisplayOrder", p => p.DisplayOrder)
            };
            return ExportToXlsx(properties, categories);
        }

        /// <summary>
        /// Export products to XLSX
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="products">Products</param>
        public virtual byte[] ExportProductsToXlsx(IEnumerable<Product> products)
        {

            var properties = new[]
            {
                new PropertyByName<Product>("Id", p => p.Id),
                new PropertyByName<Product>("ProductTypeId", p => p.ProductTypeId),
                new PropertyByName<Product>("ParentGroupedProductId", p => p.ParentGroupedProductId),
                new PropertyByName<Product>("VisibleIndividually", p => p.VisibleIndividually),
                new PropertyByName<Product>("Name", p => p.Name),
                new PropertyByName<Product>("ShortDescription", p => p.ShortDescription),
                new PropertyByName<Product>("FullDescription", p => p.FullDescription),
                new PropertyByName<Product>("Flag", p => p.Flag),
                new PropertyByName<Product>("BrandId", p => p.BrandId),
                new PropertyByName<Product>("VendorId", p => p.VendorId),
                new PropertyByName<Product>("ProductLayoutId", p => p.ProductLayoutId),
                new PropertyByName<Product>("ShowOnHomePage", p => p.ShowOnHomePage),
                new PropertyByName<Product>("BestSeller", p => p.BestSeller),
                new PropertyByName<Product>("MetaKeywords", p => p.MetaKeywords),
                new PropertyByName<Product>("MetaDescription", p => p.MetaDescription),
                new PropertyByName<Product>("MetaTitle", p => p.MetaTitle),
                new PropertyByName<Product>("SeName", p => p.GetSeName("")),
                new PropertyByName<Product>("AllowCustomerReviews", p => p.AllowCustomerReviews),
                new PropertyByName<Product>("Published", p => p.Published),
                new PropertyByName<Product>("SKU", p => p.Sku),
                new PropertyByName<Product>("Mpn", p => p.Mpn),
                new PropertyByName<Product>("Gtin", p => p.Gtin),
                new PropertyByName<Product>("IsGiftVoucher", p => p.IsGiftVoucher),
                new PropertyByName<Product>("GiftVoucherTypeId", p => p.GiftVoucherTypeId),
                new PropertyByName<Product>("OverriddenGiftVoucherAmount", p => p.OverGiftAmount),
                new PropertyByName<Product>("RequireOtherProducts", p => p.RequireOtherProducts),
                new PropertyByName<Product>("RequiredProductIds", p => p.RequiredProductIds),
                new PropertyByName<Product>("AutomaticallyAddRequiredProducts", p => p.AutoAddRequiredProducts),
                new PropertyByName<Product>("IsDownload", p => p.IsDownload),
                new PropertyByName<Product>("DownloadId", p => p.DownloadId),
                new PropertyByName<Product>("UnlimitedDownloads", p => p.UnlimitedDownloads),
                new PropertyByName<Product>("MaxNumberOfDownloads", p => p.MaxNumberOfDownloads),
                new PropertyByName<Product>("DownloadActivationTypeId", p => p.DownloadActivationTypeId),
                new PropertyByName<Product>("HasSampleDownload", p => p.HasSampleDownload),
                new PropertyByName<Product>("SampleDownloadId", p => p.SampleDownloadId),
                new PropertyByName<Product>("HasUserAgreement", p => p.HasUserAgreement),
                new PropertyByName<Product>("UserAgreementText", p => p.UserAgreementText),
                new PropertyByName<Product>("IsRecurring", p => p.IsRecurring),
                new PropertyByName<Product>("RecurringCycleLength", p => p.RecurringCycleLength),
                new PropertyByName<Product>("RecurringCyclePeriodId", p => p.RecurringCyclePeriodId),
                new PropertyByName<Product>("RecurringTotalCycles", p => p.RecurringTotalCycles),
                new PropertyByName<Product>("Interval", p => p.Interval),
                new PropertyByName<Product>("IntervalUnitId", p => p.IntervalUnitId),
                new PropertyByName<Product>("IsShipEnabled", p => p.IsShipEnabled),
                new PropertyByName<Product>("IsFreeShipping", p => p.IsFreeShipping),
                new PropertyByName<Product>("ShipSeparately", p => p.ShipSeparately),
                new PropertyByName<Product>("AdditionalShippingCharge", p => p.AdditionalShippingCharge),
                new PropertyByName<Product>("DeliveryDateId", p => p.DeliveryDateId),
                new PropertyByName<Product>("IsTaxExempt", p => p.IsTaxExempt),
                new PropertyByName<Product>("TaxCategoryId", p => p.TaxCategoryId),
                new PropertyByName<Product>("IsTele", p => p.IsTele),
                new PropertyByName<Product>("ManageInventoryMethodId", p => p.ManageInventoryMethodId),
                new PropertyByName<Product>("UseMultipleWarehouses", p => p.UseMultipleWarehouses),
                new PropertyByName<Product>("WarehouseId", p => p.WarehouseId),
                new PropertyByName<Product>("StockQuantity", p => p.StockQuantity),
                new PropertyByName<Product>("ReservedQuantity", p => p.ReservedQuantity),
                new PropertyByName<Product>("DisplayStockAvailability", p => p.StockAvailability),
                new PropertyByName<Product>("DisplayStockQuantity", p => p.DisplayStockQuantity),
                new PropertyByName<Product>("MinStockQuantity", p => p.MinStockQuantity),
                new PropertyByName<Product>("LowStockActivityId", p => p.LowStockActivityId),
                new PropertyByName<Product>("NotifyAdminForQuantityBelow", p => p.NotifyAdminForQuantityBelow),
                new PropertyByName<Product>("BackorderModeId", p => p.BackorderModeId),
                new PropertyByName<Product>("AllowOutOfStockSubscriptions", p => p.AllowOutOfStockSubscriptions),
                new PropertyByName<Product>("OrderMinimumQuantity", p => p.OrderMinimumQuantity),
                new PropertyByName<Product>("OrderMaximumQuantity", p => p.OrderMaximumQuantity),
                new PropertyByName<Product>("AllowedQuantities", p => p.AllowedQuantities),
                new PropertyByName<Product>("DisableBuyButton", p => p.DisableBuyButton),
                new PropertyByName<Product>("DisableWishlistButton", p => p.DisableWishlistButton),
                new PropertyByName<Product>("AvailableForPreOrder", p => p.AvailableForPreOrder),
                new PropertyByName<Product>("PreOrderDateTimeUtc", p => p.PreOrderDateTimeUtc),
                new PropertyByName<Product>("CallForPrice", p => p.CallForPrice),
                new PropertyByName<Product>("Price", p => p.Price),
                new PropertyByName<Product>("OldPrice", p => p.OldPrice),
                new PropertyByName<Product>("CatalogPrice", p => p.CatalogPrice),
                new PropertyByName<Product>("ProductCost", p => p.ProductCost),
                new PropertyByName<Product>("EnteredPrice", p => p.EnteredPrice),
                new PropertyByName<Product>("MinEnteredPrice", p => p.MinEnteredPrice),
                new PropertyByName<Product>("MaxEnteredPrice", p => p.MaxEnteredPrice),
                new PropertyByName<Product>("BasepriceEnabled", p => p.BasepriceEnabled),
                new PropertyByName<Product>("BasepriceAmount", p => p.BasepriceAmount),
                new PropertyByName<Product>("BasepriceUnitId", p => p.BasepriceUnitId),
                new PropertyByName<Product>("BasepriceBaseAmount", p => p.BasepriceBaseAmount),
                new PropertyByName<Product>("BasepriceBaseUnitId", p => p.BasepriceBaseUnitId),
                new PropertyByName<Product>("MarkAsNew", p => p.MarkAsNew),
                new PropertyByName<Product>("MarkAsNewStartDateTimeUtc", p => p.MarkAsNewStartDateTimeUtc),
                new PropertyByName<Product>("MarkAsNewEndDateTimeUtc", p => p.MarkAsNewEndDateTimeUtc),
                new PropertyByName<Product>("UnitId", p => p.UnitId),
                new PropertyByName<Product>("Weight", p => p.Weight),
                new PropertyByName<Product>("Length", p => p.Length),
                new PropertyByName<Product>("Width", p => p.Width),
                new PropertyByName<Product>("Height", p => p.Height),
                new PropertyByName<Product>("CategoryIds", p =>  string.Join(";", p.ProductCategories.Select(n => n.CategoryId).ToArray())),
                new PropertyByName<Product>("CollectionIds", p=>  string.Join(";", p.ProductCollections.Select(n => n.CollectionId).ToArray())),
                new PropertyByName<Product>("Picture1", p => GetPictures(p).Result[0]),
                new PropertyByName<Product>("Picture2", p => GetPictures(p).Result[1]),
                new PropertyByName<Product>("Picture3", p => GetPictures(p).Result[2])
            };

            return ExportToXlsx(properties, products);
        }


        /// <summary>
        /// Export orders to XLSX
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        public virtual byte[] ExportOrdersToXlsx(IList<Order> orders)
        {
            return ExportToXlsx(PropertyByOrder(), orders);
        }

        /// <summary>
        /// Export customer list to XLSX
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="customers">Customers</param>
        public virtual byte[] ExportCustomersToXlsx(IList<Customer> customers)
        {
            return ExportToXlsx(PropertyByCustomer(), customers);
        }


        /// <summary>
        /// Export customer - personal info to XLSX
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task<byte[]> ExportCustomerToXlsx(Customer customer, string storeId)
        {
            using (var stream = new MemoryStream())
            {
                IWorkbook xlPackage = new XSSFWorkbook();

                //customer info
                ISheet worksheetCustomer = xlPackage.CreateSheet("CustomerInfo");
                var managerCustomer = PrepareCustomer(customer);
                managerCustomer.WriteToXlsx(worksheetCustomer);

                //address
                var worksheetAddress = xlPackage.CreateSheet("Address");
                var managerAddress = new PropertyManager<Address>(PropertyByAddress());
                managerAddress.WriteCaption(worksheetAddress);
                var row = 1;
                foreach (var item in customer.Addresses)
                {
                    managerAddress.CurrentObject = item;
                    managerAddress.WriteToXlsx(worksheetAddress, row++);
                }

                //orders
                var orderService = _serviceProvider.GetRequiredService<IOrderService>();
                var orders = await orderService.SearchOrders(customerId: customer.Id);
                var worksheetOrder = xlPackage.CreateSheet("Orders");
                var managerOrder = new PropertyManager<Order>(PropertyByOrder());
                managerOrder.WriteCaption(worksheetOrder);
                row = 1;
                foreach (var items in orders)
                {
                    managerOrder.CurrentObject = items;
                    managerOrder.WriteToXlsx(worksheetOrder, row++);
                }

                //activity log
                var customerActivityService = _serviceProvider.GetRequiredService<ICustomerActivityService>();
                var actlogs = await customerActivityService.GetAllActivities(customerId: customer.Id);
                var worksheetLog = xlPackage.CreateSheet("ActivityLogs");
                var managerLog = new PropertyManager<ActivityLog>(PropertyByActivityLog());
                managerLog.WriteCaption(worksheetLog);
                row = 1;
                foreach (var items in actlogs)
                {
                    managerLog.CurrentObject = items;
                    managerLog.WriteToXlsx(worksheetLog, row++);
                }


                //contact us
                var contactUsService = _serviceProvider.GetRequiredService<IContactUsService>();
                var contacts = await contactUsService.GetAllContactUs(customerId: customer.Id);
                var worksheetContact = xlPackage.CreateSheet("MessageContact");
                var managerContact = new PropertyManager<ContactUs>(PropertyByContactForm());
                managerContact.WriteCaption(worksheetContact);
                row = 1;
                foreach (var items in contacts)
                {
                    managerContact.CurrentObject = items;
                    managerContact.WriteToXlsx(worksheetContact, row++);
                }

                //emails
                var queuedEmailService = _serviceProvider.GetRequiredService<IQueuedEmailService>();
                var queuedEmails = await queuedEmailService.SearchEmails("", customer.Email, null, null, null, false, true, 100, true);
                var worksheetEmails = xlPackage.CreateSheet("Emails");
                var managerEmails = new PropertyManager<QueuedEmail>(PropertyByEmails());
                managerEmails.WriteCaption(worksheetEmails);
                row = 1;
                foreach (var items in queuedEmails)
                {
                    managerEmails.CurrentObject = items;
                    managerEmails.WriteToXlsx(worksheetEmails, row++);
                }

                //Newsletter subscribe - history of change
                var newsletterService = _serviceProvider.GetRequiredService<INewsLetterSubscriptionService>();
                var newsletter = await newsletterService.GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, storeId);
                if (newsletter != null)
                {
                    var worksheetNewsletter = xlPackage.CreateSheet("Newsletter subscribe - history of change");
                    var managerNewsletter = new PropertyManager<NewsLetterSubscription>(PropertyByNewsLetterSubscription());
                    var newsletterhistory = await newsletter.GetHistoryObject(_serviceProvider.GetRequiredService<IHistoryService>());
                    managerNewsletter.WriteCaption(worksheetNewsletter);
                    row = 1;
                    foreach (var item in newsletterhistory)
                    {
                        var _tmp = (NewsLetterSubscription)item.Object;

                        var newslettertml = new NewsLetterSubscription()
                        {
                            Active = _tmp.Active,
                            CreatedOnUtc = item.CreatedOnUtc
                        };
                        _tmp.Categories.ToList().ForEach(x => newslettertml.Categories.Add(x));
                        managerNewsletter.CurrentObject = newslettertml;
                        managerNewsletter.WriteToXlsx(worksheetNewsletter, row++);
                    }
                }

                xlPackage.Write(stream);
                return stream.ToArray();
            }

        }

        /// <summary>
        /// Export newsletter subscribers to TXT
        /// </summary>
        /// <param name="subscriptions">Subscriptions</param>
        /// <returns>Result in TXT (string) format</returns>
        public virtual string ExportNewsletterSubscribersToTxt(IList<NewsLetterSubscription> subscriptions)
        {
            if (subscriptions == null)
                throw new ArgumentNullException(nameof(subscriptions));

            const string separator = ",";
            var sb = new StringBuilder();
            foreach (var subscription in subscriptions)
            {
                sb.Append(subscription.Email);
                sb.Append(separator);
                sb.Append(subscription.Active);
                sb.Append(separator);
                sb.Append(subscription.CreatedOnUtc.ToString("dd.MM.yyyy HH:mm:ss"));
                sb.Append(separator);
                sb.Append(subscription.StoreId);
                sb.Append(separator);
                sb.Append(string.Join(';', subscription.Categories));
                sb.Append(Environment.NewLine);  //new line
            }
            return sb.ToString();
        }

        /// <summary>
        /// Export newsletter subscribers to TXT
        /// </summary>
        /// <param name="subscriptions">Subscriptions</param>
        /// <returns>Result in TXT (string) format</returns>
        public virtual string ExportNewsletterSubscribersToTxt(IList<string> subscriptions)
        {
            if (subscriptions == null)
                throw new ArgumentNullException(nameof(subscriptions));

            var sb = new StringBuilder();
            foreach (var subscription in subscriptions)
            {
                sb.Append(subscription);
                sb.Append(Environment.NewLine);  //new line
            }
            return sb.ToString();
        }
      
        /// <summary>
        /// Export objects to XLSX
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="properties">Class access to the object through its properties</param>
        /// <param name="itemsToExport">The objects to export</param>
        /// <returns></returns>
        public virtual byte[] ExportToXlsx<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
        {
            using (var stream = new MemoryStream())
            {
                IWorkbook xlPackage = new XSSFWorkbook();
                ISheet worksheet = xlPackage.CreateSheet(typeof(T).Name);
                var manager = new PropertyManager<T>(properties);
                manager.WriteCaption(worksheet);

                var row = 1;

                foreach (var items in itemsToExport)
                {
                    manager.CurrentObject = items;
                    manager.WriteToXlsx(worksheet, row++);
                }
                xlPackage.Write(stream);
                return stream.ToArray();
            }
        }

        private async Task<string[]> GetPictures(Product product)
        {
            string picture1 = null;
            string picture2 = null;
            string picture3 = null;
            int i = 0;
            foreach (var picture in product.ProductPictures.Take(3))
            {
                var pic = await _pictureService.GetPictureById(picture.PictureId);
                var pictureLocalPath = await _pictureService.GetThumbPhysicalPath(pic);
                switch (i)
                {
                    case 0:
                        picture1 = pictureLocalPath;
                        break;
                    case 1:
                        picture2 = pictureLocalPath;
                        break;
                    case 2:
                        picture3 = pictureLocalPath;
                        break;
                }
                i++;
            }

            return new[] { picture1, picture2, picture3 };
        }

        /// <summary>
        /// Returns the path to the image file by ID
        /// </summary>
        /// <param name="pictureId">Picture ID</param>
        /// <returns>Path to the image file</returns>
        protected virtual async Task<string> GetPictures(string pictureId)
        {
            var picture = await _pictureService.GetPictureById(pictureId);
            return await _pictureService.GetThumbPhysicalPath(picture);
        }


        private PropertyByName<Order>[] PropertyByOrder()
        {
            var properties = new[]
            {
                    new PropertyByName<Order>("OrderNumber", p=>p.OrderNumber),
                    new PropertyByName<Order>("OrderCode", p=>p.Code),
                    new PropertyByName<Order>("OrderId", p=>p.Id),
                    new PropertyByName<Order>("StoreId", p=>p.StoreId),
                    new PropertyByName<Order>("OrderGuid",p=>p.OrderGuid),
                    new PropertyByName<Order>("CustomerId",p=>p.CustomerId),
                    new PropertyByName<Order>("OrderStatusId", p=>p.OrderStatusId),
                    new PropertyByName<Order>("PaymentStatusId", p=>p.PaymentStatusId),
                    new PropertyByName<Order>("ShippingStatusId", p=>p.ShippingStatusId),
                    new PropertyByName<Order>("OrderSubtotalInclTax", p=>p.OrderSubtotalInclTax),
                    new PropertyByName<Order>("OrderSubtotalExclTax", p=>p.OrderSubtotalExclTax),
                    new PropertyByName<Order>("OrderSubTotalDiscountInclTax", p=>p.OrderSubTotalDiscountInclTax),
                    new PropertyByName<Order>("OrderSubTotalDiscountExclTax", p=>p.OrderSubTotalDiscountExclTax),
                    new PropertyByName<Order>("OrderShippingInclTax", p=>p.OrderShippingInclTax),
                    new PropertyByName<Order>("OrderShippingExclTax", p=>p.OrderShippingExclTax),
                    new PropertyByName<Order>("PaymentMethodAdditionalFeeInclTax", p=>p.PaymentMethodAdditionalFeeInclTax),
                    new PropertyByName<Order>("PaymentMethodAdditionalFeeExclTax", p=>p.PaymentMethodAdditionalFeeExclTax),
                    new PropertyByName<Order>("OrderTax", p=>p.OrderTax),
                    new PropertyByName<Order>("OrderTotal", p=>p.OrderTotal),
                    new PropertyByName<Order>("RefundedAmount", p=>p.RefundedAmount),
                    new PropertyByName<Order>("OrderDiscount", p=>p.OrderDiscount),
                    new PropertyByName<Order>("CurrencyRate", p=>p.CurrencyRate),
                    new PropertyByName<Order>("CustomerCurrencyCode", p=>p.CustomerCurrencyCode),
                    new PropertyByName<Order>("AffiliateId", p=>p.AffiliateId),
                    new PropertyByName<Order>("PaymentMethodSystemName", p=>p.PaymentMethodSystemName),
                    new PropertyByName<Order>("ShippingPickUpInStore", p=>p.PickUpInStore),
                    new PropertyByName<Order>("ShippingMethod", p=>p.ShippingMethod),
                    new PropertyByName<Order>("ShippingRateComputationMethodSystemName", p=>p.ShippingRateProviderSystemName),
                    new PropertyByName<Order>("VatNumber", p=>p.VatNumber),
                    new PropertyByName<Order>("CreatedOnUtc", p=>p.CreatedOnUtc.ToOADate()),
                    new PropertyByName<Order>("BillingFirstName", p=>p.BillingAddress.Return(billingAddress=>billingAddress.FirstName, "")),
                    new PropertyByName<Order>("BillingLastName", p=>p.BillingAddress.Return(billingAddress=>billingAddress.LastName, "")),
                    new PropertyByName<Order>("BillingEmail", p=>p.BillingAddress.Return(billingAddress=>billingAddress.Email, "")),
                    new PropertyByName<Order>("BillingCompany", p=>p.BillingAddress.Return(billingAddress=>billingAddress.Company, "")),
                    new PropertyByName<Order>("BillingVatNumber", p=>p.BillingAddress.Return(billingAddress=>billingAddress.VatNumber, "")),
                    new PropertyByName<Order>("BillingCountry",p=>p.BillingAddress.Return(billingAddress=>_serviceProvider.GetRequiredService<ICountryService>().GetCountryById(billingAddress.CountryId).Result, null).Return(country=>country.Name,"")),
                    new PropertyByName<Order>("BillingCity", p=>p.BillingAddress.Return(billingAddress=>billingAddress.City,"")),
                    new PropertyByName<Order>("BillingAddress1",p=>p.BillingAddress.Return(billingAddress=>billingAddress.Address1,"")),
                    new PropertyByName<Order>("BillingAddress2", p=>p.BillingAddress.Return(billingAddress=>billingAddress.Address2,"")),
                    new PropertyByName<Order>("BillingZipPostalCode", p=>p.BillingAddress.Return(billingAddress=>billingAddress.ZipPostalCode,"")),
                    new PropertyByName<Order>("BillingPhoneNumber", p=>p.BillingAddress.Return(billingAddress=>billingAddress.PhoneNumber,"")),
                    new PropertyByName<Order>("BillingFaxNumber", p=>p.BillingAddress.Return(billingAddress=>billingAddress.FaxNumber,"")),
                    new PropertyByName<Order>("ShippingFirstName", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.FirstName,"")),
                    new PropertyByName<Order>("ShippingLastName", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.LastName, "")),
                    new PropertyByName<Order>("ShippingEmail", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.Email, "")),
                    new PropertyByName<Order>("ShippingCompany", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.Company, "")),
                    new PropertyByName<Order>("ShippingVatNumber", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.VatNumber, "")),
                    new PropertyByName<Order>("ShippingCountry", p=>p.ShippingAddress.Return(shippingAddress=>_serviceProvider.GetRequiredService<ICountryService>().GetCountryById(shippingAddress.CountryId).Result, null).Return(country=>country.Name,"")),
                    new PropertyByName<Order>("ShippingCity", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.City, "")),
                    new PropertyByName<Order>("ShippingAddress1", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.Address1, "")),
                    new PropertyByName<Order>("ShippingAddress2", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.Address2, "")),
                    new PropertyByName<Order>("ShippingZipPostalCode", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.ZipPostalCode, "")),
                    new PropertyByName<Order>("ShippingPhoneNumber",p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.PhoneNumber, "")),
                    new PropertyByName<Order>("ShippingFaxNumber", p=>p.ShippingAddress.Return(shippingAddress=>shippingAddress.FaxNumber, ""))
            };
            return properties;
        }

        private PropertyByName<Address>[] PropertyByAddress()
        {
            var properties = new[]
            {
                    new PropertyByName<Address>("Email", p=>p.Email),
                    new PropertyByName<Address>("FirstName", p=>p.FirstName),
                    new PropertyByName<Address>("LastName", p=>p.LastName),
                    new PropertyByName<Address>("PhoneNumber", p=>p.PhoneNumber),
                    new PropertyByName<Address>("FaxNumber", p=>p.FaxNumber),
                    new PropertyByName<Address>("Address1", p=>p.Address1),
                    new PropertyByName<Address>("Address2", p=>p.Address2),
                    new PropertyByName<Address>("City", p=>p.City),
                    new PropertyByName<Address>("Country", p=> !string.IsNullOrEmpty(p.CountryId) ? _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(p.CountryId).Result?.Name : ""),
            };
            return properties;
        }

        private PropertyByName<Customer>[] PropertyByCustomer()
        {
            var properties = new[]
            {
                new PropertyByName<Customer>("CustomerId", p => p.Id),
                new PropertyByName<Customer>("CustomerGuid", p => p.CustomerGuid),
                new PropertyByName<Customer>("Email", p => p.Email),
                new PropertyByName<Customer>("Username", p => p.Username),
                new PropertyByName<Customer>("Password", p => p.Password),
                new PropertyByName<Customer>("PasswordFormatId", p => p.PasswordFormatId),
                new PropertyByName<Customer>("PasswordSalt", p => p.PasswordSalt),
                new PropertyByName<Customer>("IsTaxExempt", p => p.IsTaxExempt),
                new PropertyByName<Customer>("AffiliateId", p => p.AffiliateId),
                new PropertyByName<Customer>("VendorId", p => p.VendorId),
                new PropertyByName<Customer>("Active", p => p.Active),
                //attributes
                new PropertyByName<Customer>("FirstName", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName)),
                new PropertyByName<Customer>("LastName", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)),
                new PropertyByName<Customer>("Gender", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender)),
                new PropertyByName<Customer>("Company", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company)),
                new PropertyByName<Customer>("StreetAddress", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress)),
                new PropertyByName<Customer>("StreetAddress2", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2)),
                new PropertyByName<Customer>("ZipPostalCode", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode)),
                new PropertyByName<Customer>("City", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City)),
                new PropertyByName<Customer>("CountryId", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId)),
                new PropertyByName<Customer>("StateProvinceId", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId)),
                new PropertyByName<Customer>("Phone", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone)),
                new PropertyByName<Customer>("Fax", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax)),
                new PropertyByName<Customer>("VatNumber", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber)),
                new PropertyByName<Customer>("VatNumberStatusId", p => p.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId))
            };
            return properties;
        }

        private PropertyByName<ActivityLog>[] PropertyByActivityLog()
        {
            var properties = new[]
            {
                new PropertyByName<ActivityLog>("IpAddress", p => p.IpAddress),
                new PropertyByName<ActivityLog>("CreatedOnUtc", p => p.CreatedOnUtc.ToString()),
                new PropertyByName<ActivityLog>("Comment", p => p.Comment),
            };
            return properties;
        }
        private PropertyByName<ContactUs>[] PropertyByContactForm()
        {
            var properties = new[]
            {
                new PropertyByName<ContactUs>("IpAddress", p => p.IpAddress),
                new PropertyByName<ContactUs>("CreatedOnUtc", p => p.CreatedOnUtc.ToString()),
                new PropertyByName<ContactUs>("Email", p => p.Email),
                new PropertyByName<ContactUs>("FullName", p => p.FullName),
                new PropertyByName<ContactUs>("Subject", p => p.Subject),
                new PropertyByName<ContactUs>("Enquiry", p => p.Enquiry),
                new PropertyByName<ContactUs>("ContactAttributeDescription", p => p.ContactAttributeDescription),
            };
            return properties;
        }

        private PropertyByName<QueuedEmail>[] PropertyByEmails()
        {
            var properties = new[]
            {
                new PropertyByName<QueuedEmail>("SentOnUtc", p => p.SentOnUtc.ToString()),
                new PropertyByName<QueuedEmail>("From", p => p.From),
                new PropertyByName<QueuedEmail>("FromName", p => p.FromName),
                new PropertyByName<QueuedEmail>("Subject", p => p.Subject),
                new PropertyByName<QueuedEmail>("Body", p => p.Body),
            };
            return properties;
        }

        private PropertyByName<NewsLetterSubscription>[] PropertyByNewsLetterSubscription()
        {
            var newsletterCategoryService = _serviceProvider.GetRequiredService<INewsletterCategoryService>();

            string GetCategoryNames(IList<string> categoryNames, string separator = ",")
            {
                var sb = new StringBuilder();
                for (int i = 0; i < categoryNames.Count; i++)
                {
                    var category = newsletterCategoryService.GetNewsletterCategoryById(categoryNames[i]).Result;
                    if (category != null)
                    {
                        sb.Append(category.Name);
                        if (i != categoryNames.Count - 1)
                        {
                            sb.Append(separator);
                            sb.Append(" ");
                        }
                    }
                }
                return sb.ToString();
            }
            var properties = new[]
            {
                new PropertyByName<NewsLetterSubscription>("CreatedOnUtc", p => p.CreatedOnUtc.ToString()),
                new PropertyByName<NewsLetterSubscription>("Active", p => p.Active.ToString()),
                new PropertyByName<NewsLetterSubscription>("Categories", p => GetCategoryNames(p.Categories.ToList())),

            };
            return properties;
        }

        private PropertyHelperList<Customer> PrepareCustomer(Customer customer)
        {
            var helper = new PropertyHelperList<Customer>(customer);
            helper.ObjectList.Add(new PropertyHelperList<Customer>("CustomerId", p => p.Id));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("CustomerGuid", p => p.CustomerGuid));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Email", p => p.Email));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Username", p => p.Username));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Password", p => p.Password));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("PasswordFormatId", p => p.PasswordFormatId));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("PasswordSalt", p => p.PasswordSalt));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("IsTaxExempt", p => p.IsTaxExempt));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Active", p => p.Active));
            //attributes
            helper.ObjectList.Add(new PropertyHelperList<Customer>("FirstName", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("LastName", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Gender", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Company", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("StreetAddress", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("StreetAddress2", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("ZipPostalCode", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("City", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Country",
                p =>
                {
                    var countryid = p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId);
                    var countryName = "";
                    if (!string.IsNullOrEmpty(countryid))
                        countryName = _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(countryid).Result?.Name;
                    return countryName;
                }
                ));

            helper.ObjectList.Add(new PropertyHelperList<Customer>("Phone", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("Fax", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax)));
            helper.ObjectList.Add(new PropertyHelperList<Customer>("VatNumber", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber)));

            return helper;
        }

        #endregion
    }
}
