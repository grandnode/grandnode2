using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Web.Common.Extensions;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class MerchandiseReturnViewModelService : IMerchandiseReturnViewModelService
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICustomerService _customerService;
        private readonly ITranslationService _translationService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly LanguageSettings _languageSettings;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICurrencyService _currencyService;
        private readonly AddressSettings _addressSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ICountryService _countryService;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IDownloadService _downloadService;
        #endregion Fields

        #region Constructors

        public MerchandiseReturnViewModelService(IOrderService orderService,
            IWorkContext workContext,
            IProductService productService,
            ICustomerService customerService, IDateTimeService dateTimeService,
            ITranslationService translationService,
            IMessageProviderService messageProviderService, LanguageSettings languageSettings,
            ICustomerActivityService customerActivityService,
            IMerchandiseReturnService merchandiseReturnService,
            IPriceFormatter priceFormatter,
            ICurrencyService currencyService,
            AddressSettings addressSettings,
            ICountryService countryService,
            IAddressAttributeService addressAttributeService,
            IAddressAttributeParser addressAttributeParser,
            IDownloadService downloadService,
            OrderSettings orderSettings)
        {
            _orderService = orderService;
            _workContext = workContext;
            _productService = productService;
            _customerService = customerService;
            _dateTimeService = dateTimeService;
            _translationService = translationService;
            _messageProviderService = messageProviderService;
            _languageSettings = languageSettings;
            _customerActivityService = customerActivityService;
            _merchandiseReturnService = merchandiseReturnService;
            _priceFormatter = priceFormatter;
            _currencyService = currencyService;
            _addressSettings = addressSettings;
            _countryService = countryService;
            _addressAttributeService = addressAttributeService;
            _addressAttributeParser = addressAttributeParser;
            _downloadService = downloadService;
            _orderSettings = orderSettings;
        }

        #endregion

        public virtual async Task<MerchandiseReturnModel> PrepareMerchandiseReturnModel(MerchandiseReturnModel model,
            MerchandiseReturn merchandiseReturn, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
            double unitPriceInclTaxInCustomerCurrency = 0;
            foreach (var item in merchandiseReturn.MerchandiseReturnItems)
            {
                var orderItem = order.OrderItems.Where(x => x.Id == item.OrderItemId).First();
                unitPriceInclTaxInCustomerCurrency += orderItem.UnitPriceInclTax * item.Quantity;
            }

            model.Total = _priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency);
            model.Quantity = merchandiseReturn.MerchandiseReturnItems.Sum(x => x.Quantity);
            model.Id = merchandiseReturn.Id;
            model.OrderId = order.Id;
            model.OrderNumber = order.OrderNumber;
            model.OrderCode = order.Code;
            model.ReturnNumber = merchandiseReturn.ReturnNumber;
            model.CustomerId = merchandiseReturn.CustomerId;
            model.NotifyCustomer = merchandiseReturn.NotifyCustomer;
            var customer = await _customerService.GetCustomerById(merchandiseReturn.CustomerId);
            if (customer != null)
                model.CustomerInfo = !string.IsNullOrEmpty(customer.Email) ? customer.Email : _translationService.GetResource("Admin.Customers.Guest");
            else
                model.CustomerInfo = _translationService.GetResource("Admin.Customers.Guest");

            model.MerchandiseReturnStatusStr = merchandiseReturn.MerchandiseReturnStatus.ToString();
            model.CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturn.CreatedOnUtc, DateTimeKind.Utc);
            model.PickupDate = merchandiseReturn.PickupDate;
            model.UserFields = merchandiseReturn.UserFields;

            if (!excludeProperties)
            {
                var addr = new AddressModel();
                model.PickupAddress = await PrepareAddressModel(addr, merchandiseReturn.PickupAddress, excludeProperties);
                model.CustomerComments = merchandiseReturn.CustomerComments;
                model.ExternalId = merchandiseReturn.ExternalId;
                model.StaffNotes = merchandiseReturn.StaffNotes;
                model.MerchandiseReturnStatusId = merchandiseReturn.MerchandiseReturnStatusId;
            }

            return model;
        }
        public virtual async Task<(IList<MerchandiseReturnModel> merchandiseReturnModels, int totalCount)> PrepareMerchandiseReturnModel(MerchandiseReturnListModel model, int pageIndex, int pageSize)
        {
            string customerId = string.Empty;
            if (!string.IsNullOrEmpty(model.SearchCustomerEmail))
            {
                var customer = await _customerService.GetCustomerByEmail(model.SearchCustomerEmail.ToLowerInvariant());
                if (customer != null)
                    customerId = customer.Id;
                else
                    customerId = "00000000-0000-0000-0000-000000000000";
            }
            DateTime? startDateValue = (model.StartDate == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone);

            var merchandiseReturns = await _merchandiseReturnService.SearchMerchandiseReturns(model.StoreId,
                customerId,
                "",
                _workContext.CurrentVendor?.Id,
                "",
                (model.SearchMerchandiseReturnStatusId >= 0 ? (MerchandiseReturnStatus?)model.SearchMerchandiseReturnStatusId : null),
                pageIndex - 1,
                pageSize,
                startDateValue,
                endDateValue);
            var merchandiseReturnModels = new List<MerchandiseReturnModel>();
            foreach (var rr in merchandiseReturns)
            {
                var rrmodel = new MerchandiseReturnModel();
                merchandiseReturnModels.Add(await PrepareMerchandiseReturnModel(rrmodel, rr, true));
            }
            return (merchandiseReturnModels, merchandiseReturns.TotalCount);
        }
        public virtual async Task<AddressModel> PrepareAddressModel(AddressModel model, Address address, bool excludeProperties)
        {
            if (address != null)
            {
                if (!excludeProperties)
                {
                    model = await address.ToModel(_countryService);
                }
            }

            if (model == null)
                model = new AddressModel();

            model.FirstNameEnabled = true;
            model.FirstNameRequired = true;
            model.LastNameEnabled = true;
            model.LastNameRequired = true;
            model.EmailEnabled = true;
            model.EmailRequired = true;
            model.CompanyEnabled = _addressSettings.CompanyEnabled;
            model.CompanyRequired = _addressSettings.CompanyRequired;
            model.VatNumberEnabled = _addressSettings.VatNumberEnabled;
            model.VatNumberRequired = _addressSettings.VatNumberRequired;
            model.CountryEnabled = _addressSettings.CountryEnabled;
            model.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
            model.CityEnabled = _addressSettings.CityEnabled;
            model.CityRequired = _addressSettings.CityRequired;
            model.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _addressSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _addressSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
            model.PhoneEnabled = _addressSettings.PhoneEnabled;
            model.PhoneRequired = _addressSettings.PhoneRequired;
            model.FaxEnabled = _addressSettings.FaxEnabled;
            model.FaxRequired = _addressSettings.FaxRequired;
            model.NoteEnabled = _addressSettings.NoteEnabled;

            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (c.Id == model.CountryId) });
            //states
            var states = !String.IsNullOrEmpty(model.CountryId) ? (await _countryService.GetCountryById(model.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == model.StateProvinceId) });
            }
            //customer attribute services
            await model.PrepareCustomAddressAttributes(address, _addressAttributeService, _addressAttributeParser);

            return model;
        }

        public virtual async Task NotifyCustomer(MerchandiseReturn merchandiseReturn)
        {
            var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
            await _messageProviderService.SendMerchandiseReturnStatusChangedCustomerMessage(merchandiseReturn, order, _languageSettings.DefaultAdminLanguageId);
        }
        public virtual MerchandiseReturnListModel PrepareReturnReqestListModel()
        {
            var model = new MerchandiseReturnListModel
            {
                //Merchandise return status
                MerchandiseReturnStatus = MerchandiseReturnStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList()
            };
            model.MerchandiseReturnStatus.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "-1" });

            return model;
        }
        public virtual async Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>> PrepareMerchandiseReturnItemModel(string merchandiseReturnId)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
            var items = new List<MerchandiseReturnModel.MerchandiseReturnItemModel>();
            var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);

            foreach (var item in merchandiseReturn.MerchandiseReturnItems)
            {
                var orderItem = order.OrderItems.Where(x => x.Id == item.OrderItemId).FirstOrDefault();

                items.Add(new MerchandiseReturnModel.MerchandiseReturnItemModel
                {
                    ProductId = orderItem.ProductId,
                    ProductName = (await _productService.GetProductByIdIncludeArch(orderItem.ProductId)).Name,
                    Quantity = item.Quantity,
                    UnitPrice = _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax),
                    ReasonForReturn = item.ReasonForReturn,
                    RequestedAction = item.RequestedAction
                });
            }
            return items;
        }
        public virtual async Task<MerchandiseReturn> UpdateMerchandiseReturnModel(MerchandiseReturn merchandiseReturn, MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes)
        {
            merchandiseReturn.CustomerComments = model.CustomerComments;
            merchandiseReturn.StaffNotes = model.StaffNotes;
            merchandiseReturn.MerchandiseReturnStatusId = model.MerchandiseReturnStatusId;
            merchandiseReturn.ExternalId = model.ExternalId;
            merchandiseReturn.UpdatedOnUtc = DateTime.UtcNow;
            merchandiseReturn.UserFields = model.UserFields;

            if (_orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate)
                merchandiseReturn.PickupDate = model.PickupDate;
            if (_orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            {
                merchandiseReturn.PickupAddress = model.PickupAddress.ToEntity();
                if (merchandiseReturn.PickupAddress != null)
                    merchandiseReturn.PickupAddress.Attributes = customAddressAttributes;
            }
            merchandiseReturn.NotifyCustomer = model.NotifyCustomer;
            await _merchandiseReturnService.UpdateMerchandiseReturn(merchandiseReturn);
            //activity log
            await _customerActivityService.InsertActivity("EditMerchandiseReturn", merchandiseReturn.Id, _translationService.GetResource("ActivityLog.EditMerchandiseReturn"), merchandiseReturn.Id);

            if (model.NotifyCustomer)
                await NotifyCustomer(merchandiseReturn);
            return merchandiseReturn;
        }
        public virtual async Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn)
        {
            await _merchandiseReturnService.DeleteMerchandiseReturn(merchandiseReturn);
            //activity log
            await _customerActivityService.InsertActivity("DeleteMerchandiseReturn", merchandiseReturn.Id, _translationService.GetResource("ActivityLog.DeleteMerchandiseReturn"), merchandiseReturn.Id);
        }

        public virtual async Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(MerchandiseReturn merchandiseReturn)
        {
            //merchandise return notes
            var merchandiseReturnNoteModels = new List<MerchandiseReturnModel.MerchandiseReturnNote>();
            foreach (var merchandiseReturnNote in (await _merchandiseReturnService.GetMerchandiseReturnNotes(merchandiseReturn.Id))
                .OrderByDescending(on => on.CreatedOnUtc))
            {
                var download = await _downloadService.GetDownloadById(merchandiseReturnNote.DownloadId);
                merchandiseReturnNoteModels.Add(new MerchandiseReturnModel.MerchandiseReturnNote
                {
                    Id = merchandiseReturnNote.Id,
                    MerchandiseReturnId = merchandiseReturn.Id,
                    DownloadId = String.IsNullOrEmpty(merchandiseReturnNote.DownloadId) ? "" : merchandiseReturnNote.DownloadId,
                    DownloadGuid = download != null ? download.DownloadGuid : Guid.Empty,
                    DisplayToCustomer = merchandiseReturnNote.DisplayToCustomer,
                    Note = merchandiseReturnNote.Note,
                    CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturnNote.CreatedOnUtc, DateTimeKind.Utc),
                    CreatedByCustomer = merchandiseReturnNote.CreatedByCustomer
                });
            }
            return merchandiseReturnNoteModels;
        }

        public virtual async Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, Order order, string downloadId, bool displayToCustomer, string message)
        {
            var merchandiseReturnNote = new MerchandiseReturnNote
            {
                DisplayToCustomer = displayToCustomer,
                Note = message,
                DownloadId = downloadId,
                MerchandiseReturnId = merchandiseReturn.Id,
                CreatedOnUtc = DateTime.UtcNow,
            };
            await _merchandiseReturnService.InsertMerchandiseReturnNote(merchandiseReturnNote);

            //new merchandise return notification
            if (displayToCustomer)
            {
                //email
                await _messageProviderService.SendNewMerchandiseReturnNoteAddedCustomerMessage(merchandiseReturn, merchandiseReturnNote, order);
            }
        }

        public virtual async Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id)
        {
            var merchandiseReturnNote = (await _merchandiseReturnService.GetMerchandiseReturnNotes(merchandiseReturn.Id)).FirstOrDefault(on => on.Id == id);
            if (merchandiseReturnNote == null)
                throw new ArgumentException("No merchandise return note found with the specified id");

            merchandiseReturnNote.MerchandiseReturnId = merchandiseReturn.Id;
            await _merchandiseReturnService.DeleteMerchandiseReturnNote(merchandiseReturnNote);
        }
    }
}
