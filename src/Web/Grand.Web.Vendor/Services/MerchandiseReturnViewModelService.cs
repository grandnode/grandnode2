using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Common;
using Grand.Web.Vendor.Models.MerchandiseReturn;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Vendor.Services;

public class MerchandiseReturnViewModelService : IMerchandiseReturnViewModelService
{
    #region Constructors

    public MerchandiseReturnViewModelService(
        IOrderService orderService,
        IWorkContext workContext,
        IProductService productService,
        ICustomerService customerService,
        IDateTimeService dateTimeService,
        ITranslationService translationService,
        IMessageProviderService messageProviderService,
        LanguageSettings languageSettings,
        IMerchandiseReturnService merchandiseReturnService,
        IPriceFormatter priceFormatter,
        AddressSettings addressSettings,
        ICountryService countryService,
        IAddressAttributeService addressAttributeService,
        IAddressAttributeParser addressAttributeParser,
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
        _merchandiseReturnService = merchandiseReturnService;
        _priceFormatter = priceFormatter;
        _addressSettings = addressSettings;
        _countryService = countryService;
        _addressAttributeService = addressAttributeService;
        _addressAttributeParser = addressAttributeParser;
        _orderSettings = orderSettings;
    }

    #endregion

    public virtual async Task<MerchandiseReturnModel> PrepareMerchandiseReturnModel(MerchandiseReturnModel model,
        MerchandiseReturn merchandiseReturn, bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(merchandiseReturn);

        var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
        double unitPriceInclTaxInCustomerCurrency = 0;
        foreach (var item in merchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = order.OrderItems.First(x => x.Id == item.OrderItemId);
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
            model.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest");
        else
            model.CustomerInfo = _translationService.GetResource("Admin.Customers.Guest");

        model.MerchandiseReturnStatusStr = merchandiseReturn.MerchandiseReturnStatus.ToString();
        model.CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturn.CreatedOnUtc, DateTimeKind.Utc);
        model.PickupDate = merchandiseReturn.PickupDate;
        model.UserFields = merchandiseReturn.UserFields;

        if (!excludeProperties)
        {
            var addr = new AddressModel();
            model.PickupAddress = await PrepareAddressModel(addr, merchandiseReturn.PickupAddress, false);
            model.CustomerComments = merchandiseReturn.CustomerComments;
            model.ExternalId = merchandiseReturn.ExternalId;
            model.StaffNotes = merchandiseReturn.StaffNotes;
            model.MerchandiseReturnStatusId = merchandiseReturn.MerchandiseReturnStatusId;
        }

        return model;
    }

    public virtual async Task<(IList<MerchandiseReturnModel> merchandiseReturnModels, int totalCount)>
        PrepareMerchandiseReturnModel(MerchandiseReturnListModel model, int pageIndex, int pageSize)
    {
        var customerId = string.Empty;
        if (!string.IsNullOrEmpty(model.SearchCustomerEmail))
        {
            var customer = await _customerService.GetCustomerByEmail(model.SearchCustomerEmail);
            customerId = customer != null ? customer.Id : "00000000-0000-0000-0000-000000000000";
        }

        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone);

        var merchandiseReturns = await _merchandiseReturnService.SearchMerchandiseReturns(
            customerId: customerId,
            vendorId: _workContext.CurrentVendor.Id,
            rs: model.SearchMerchandiseReturnStatusId >= 0
                ? (MerchandiseReturnStatus?)model.SearchMerchandiseReturnStatusId
                : null,
            pageIndex: pageIndex - 1,
            pageSize: pageSize,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue);
        var merchandiseReturnModels = new List<MerchandiseReturnModel>();
        foreach (var rr in merchandiseReturns)
        {
            var merchandiseReturnModel = new MerchandiseReturnModel();
            merchandiseReturnModels.Add(await PrepareMerchandiseReturnModel(merchandiseReturnModel, rr, true));
        }

        return (merchandiseReturnModels, merchandiseReturns.TotalCount);
    }

    public virtual MerchandiseReturnListModel PrepareReturnRequestListModel()
    {
        var model = new MerchandiseReturnListModel {
            //Merchandise return status
            MerchandiseReturnStatus = MerchandiseReturnStatus.Pending
                .ToSelectList(_translationService, _workContext, false).ToList()
        };
        model.MerchandiseReturnStatus.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "-1" });

        return model;
    }

    public virtual async Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>>
        PrepareMerchandiseReturnItemModel(string merchandiseReturnId)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        var items = new List<MerchandiseReturnModel.MerchandiseReturnItemModel>();
        var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);

        foreach (var item in merchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            items.Add(new MerchandiseReturnModel.MerchandiseReturnItemModel {
                ProductId = orderItem?.ProductId,
                ProductName = (await _productService.GetProductByIdIncludeArch(orderItem?.ProductId)).Name,
                ProductSku = orderItem?.Sku,
                Quantity = item.Quantity,
                UnitPrice = _priceFormatter.FormatPrice(orderItem!.UnitPriceInclTax),
                ReasonForReturn = item.ReasonForReturn,
                RequestedAction = item.RequestedAction
            });
        }

        return items;
    }

    public virtual async Task<MerchandiseReturn> UpdateMerchandiseReturnModel(MerchandiseReturn merchandiseReturn,
        MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes)
    {
        merchandiseReturn.CustomerComments = model.CustomerComments;
        merchandiseReturn.StaffNotes = model.StaffNotes;
        merchandiseReturn.MerchandiseReturnStatusId = model.MerchandiseReturnStatusId;
        merchandiseReturn.ExternalId = model.ExternalId;
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
        if (model.NotifyCustomer)
            await NotifyCustomer(merchandiseReturn);
        return merchandiseReturn;
    }

    public virtual async Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn)
    {
        await _merchandiseReturnService.DeleteMerchandiseReturn(merchandiseReturn);
    }

    public virtual async Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(
        MerchandiseReturn merchandiseReturn)
    {
        //merchandise return notes
        var merchandiseReturnNoteModels = new List<MerchandiseReturnModel.MerchandiseReturnNote>();
        foreach (var merchandiseReturnNote in (await _merchandiseReturnService.GetMerchandiseReturnNotes(
                     merchandiseReturn.Id))
                 .OrderByDescending(on => on.CreatedOnUtc))
            merchandiseReturnNoteModels.Add(new MerchandiseReturnModel.MerchandiseReturnNote {
                Id = merchandiseReturnNote.Id,
                MerchandiseReturnId = merchandiseReturn.Id,
                DisplayToCustomer = merchandiseReturnNote.DisplayToCustomer,
                Note = merchandiseReturnNote.Note,
                CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturnNote.CreatedOnUtc, DateTimeKind.Utc),
                CreatedByCustomer = merchandiseReturnNote.CreatedByCustomer
            });
        return merchandiseReturnNoteModels;
    }

    public virtual async Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, bool displayToCustomer,
        string message)
    {
        var merchandiseReturnNote = new MerchandiseReturnNote {
            DisplayToCustomer = displayToCustomer,
            Note = message,
            MerchandiseReturnId = merchandiseReturn.Id
        };
        await _merchandiseReturnService.InsertMerchandiseReturnNote(merchandiseReturnNote);

        //new merchandise return notification
        if (displayToCustomer)
        {
            //email
            var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
            await _messageProviderService.SendNewMerchandiseReturnNoteAddedCustomerMessage(merchandiseReturn,
                merchandiseReturnNote, order);
        }
    }

    public virtual async Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id)
    {
        var merchandiseReturnNote =
            (await _merchandiseReturnService.GetMerchandiseReturnNotes(merchandiseReturn.Id)).FirstOrDefault(on =>
                on.Id == id);
        if (merchandiseReturnNote == null)
            throw new ArgumentException("No merchandise return note found with the specified id");

        merchandiseReturnNote.MerchandiseReturnId = merchandiseReturn.Id;
        await _merchandiseReturnService.DeleteMerchandiseReturnNote(merchandiseReturnNote);
    }

    private async Task<AddressModel> PrepareAddressModel(AddressModel model, Address address, bool excludeProperties)
    {
        if (address != null)
            if (!excludeProperties)
                model = await address.ToModel(_countryService);

        model ??= new AddressModel();

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
        model.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == model.CountryId });
        //states
        var states = !string.IsNullOrEmpty(model.CountryId)
            ? (await _countryService.GetCountryById(model.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states is { Count: > 0 })
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == model.StateProvinceId });
        //customer attribute services
        await model.PrepareCustomAddressAttributes(address, _addressAttributeService, _addressAttributeParser);

        return model;
    }

    private async Task NotifyCustomer(MerchandiseReturn merchandiseReturn)
    {
        var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
        await _messageProviderService.SendMerchandiseReturnStatusChangedCustomerMessage(merchandiseReturn, order,
            _languageSettings.DefaultAdminLanguageId);
    }

    #region Fields

    private readonly IOrderService _orderService;
    private readonly IWorkContext _workContext;
    private readonly IProductService _productService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICustomerService _customerService;
    private readonly ITranslationService _translationService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly AddressSettings _addressSettings;
    private readonly OrderSettings _orderSettings;
    private readonly ICountryService _countryService;
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly IAddressAttributeParser _addressAttributeParser;

    #endregion Fields
}