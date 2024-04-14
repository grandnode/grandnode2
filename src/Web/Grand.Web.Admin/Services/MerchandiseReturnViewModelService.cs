using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class MerchandiseReturnViewModelService(
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
    IDownloadService downloadService,
    OrderSettings orderSettings)
    : IMerchandiseReturnViewModelService
{
    public virtual async Task<MerchandiseReturnModel> PrepareMerchandiseReturnModel(MerchandiseReturnModel model,
        MerchandiseReturn merchandiseReturn, bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(merchandiseReturn);

        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        double unitPriceInclTaxInCustomerCurrency = 0;
        foreach (var item in merchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = order.OrderItems.First(x => x.Id == item.OrderItemId);
            unitPriceInclTaxInCustomerCurrency += orderItem.UnitPriceInclTax * item.Quantity;
        }

        model.Total = priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency);
        model.Quantity = merchandiseReturn.MerchandiseReturnItems.Sum(x => x.Quantity);
        model.Id = merchandiseReturn.Id;
        model.OrderId = order.Id;
        model.OrderNumber = order.OrderNumber;
        model.OrderCode = order.Code;
        model.ReturnNumber = merchandiseReturn.ReturnNumber;
        model.CustomerId = merchandiseReturn.CustomerId;
        model.NotifyCustomer = merchandiseReturn.NotifyCustomer;
        var customer = await customerService.GetCustomerById(merchandiseReturn.CustomerId);
        if (customer != null)
            model.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : translationService.GetResource("Admin.Customers.Guest");
        else
            model.CustomerInfo = translationService.GetResource("Admin.Customers.Guest");

        model.MerchandiseReturnStatusStr = merchandiseReturn.MerchandiseReturnStatus.ToString();
        model.CreatedOn = dateTimeService.ConvertToUserTime(merchandiseReturn.CreatedOnUtc, DateTimeKind.Utc);
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
            var customer = await customerService.GetCustomerByEmail(model.SearchCustomerEmail.ToLowerInvariant());
            customerId = customer != null ? customer.Id : "00000000-0000-0000-0000-000000000000";
        }

        DateTime? startDateValue = model.StartDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.StartDate.Value, dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.EndDate.Value, dateTimeService.CurrentTimeZone);

        var merchandiseReturns = await merchandiseReturnService.SearchMerchandiseReturns(model.StoreId,
            customerId,
            "",
            "",
            "",
            model.SearchMerchandiseReturnStatusId >= 0
                ? (MerchandiseReturnStatus?)model.SearchMerchandiseReturnStatusId
                : null,
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

    public virtual async Task<AddressModel> PrepareAddressModel(AddressModel model, Address address,
        bool excludeProperties)
    {
        if (address != null)
            if (!excludeProperties)
                model = await address.ToModel(countryService);

        model ??= new AddressModel();

        model.FirstNameEnabled = true;
        model.FirstNameRequired = true;
        model.LastNameEnabled = true;
        model.LastNameRequired = true;
        model.EmailEnabled = true;
        model.EmailRequired = true;
        model.CompanyEnabled = addressSettings.CompanyEnabled;
        model.CompanyRequired = addressSettings.CompanyRequired;
        model.VatNumberEnabled = addressSettings.VatNumberEnabled;
        model.VatNumberRequired = addressSettings.VatNumberRequired;
        model.CountryEnabled = addressSettings.CountryEnabled;
        model.StateProvinceEnabled = addressSettings.StateProvinceEnabled;
        model.CityEnabled = addressSettings.CityEnabled;
        model.CityRequired = addressSettings.CityRequired;
        model.StreetAddressEnabled = addressSettings.StreetAddressEnabled;
        model.StreetAddressRequired = addressSettings.StreetAddressRequired;
        model.StreetAddress2Enabled = addressSettings.StreetAddress2Enabled;
        model.StreetAddress2Required = addressSettings.StreetAddress2Required;
        model.ZipPostalCodeEnabled = addressSettings.ZipPostalCodeEnabled;
        model.ZipPostalCodeRequired = addressSettings.ZipPostalCodeRequired;
        model.PhoneEnabled = addressSettings.PhoneEnabled;
        model.PhoneRequired = addressSettings.PhoneRequired;
        model.FaxEnabled = addressSettings.FaxEnabled;
        model.FaxRequired = addressSettings.FaxRequired;
        model.NoteEnabled = addressSettings.NoteEnabled;

        //countries
        model.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == model.CountryId });
        //states
        var states = !string.IsNullOrEmpty(model.CountryId)
            ? (await countryService.GetCountryById(model.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states.Count > 0)
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == model.StateProvinceId });
        //customer attribute services
        await model.PrepareCustomAddressAttributes(address, addressAttributeService, addressAttributeParser);

        return model;
    }

    public virtual async Task NotifyCustomer(MerchandiseReturn merchandiseReturn)
    {
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        await messageProviderService.SendMerchandiseReturnStatusChangedCustomerMessage(merchandiseReturn, order,
            languageSettings.DefaultAdminLanguageId);
    }

    public virtual MerchandiseReturnListModel PrepareReturnReqestListModel()
    {
        var model = new MerchandiseReturnListModel {
            //Merchandise return status
            MerchandiseReturnStatus = MerchandiseReturnStatus.Pending
                .ToSelectList(translationService, workContext, false).ToList()
        };
        model.MerchandiseReturnStatus.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "-1" });

        return model;
    }

    public virtual async Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>>
        PrepareMerchandiseReturnItemModel(string merchandiseReturnId)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        var items = new List<MerchandiseReturnModel.MerchandiseReturnItemModel>();
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);

        foreach (var item in merchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            items.Add(new MerchandiseReturnModel.MerchandiseReturnItemModel {
                ProductId = orderItem.ProductId,
                ProductName = (await productService.GetProductByIdIncludeArch(orderItem.ProductId)).Name,
                ProductSku = orderItem.Sku,
                Quantity = item.Quantity,
                UnitPrice = priceFormatter.FormatPrice(orderItem.UnitPriceInclTax),
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

        if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate)
            merchandiseReturn.PickupDate = model.PickupDate;
        if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
        {
            merchandiseReturn.PickupAddress = model.PickupAddress.ToEntity();
            if (merchandiseReturn.PickupAddress != null)
                merchandiseReturn.PickupAddress.Attributes = customAddressAttributes;
        }

        merchandiseReturn.NotifyCustomer = model.NotifyCustomer;
        await merchandiseReturnService.UpdateMerchandiseReturn(merchandiseReturn);
        if (model.NotifyCustomer)
            await NotifyCustomer(merchandiseReturn);
        return merchandiseReturn;
    }

    public virtual async Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn)
    {
        await merchandiseReturnService.DeleteMerchandiseReturn(merchandiseReturn);
    }

    public virtual async Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(
        MerchandiseReturn merchandiseReturn)
    {
        //merchandise return notes
        var merchandiseReturnNoteModels = new List<MerchandiseReturnModel.MerchandiseReturnNote>();
        foreach (var merchandiseReturnNote in (await merchandiseReturnService.GetMerchandiseReturnNotes(
                     merchandiseReturn.Id))
                 .OrderByDescending(on => on.CreatedOnUtc))
        {
            var download = await downloadService.GetDownloadById(merchandiseReturnNote.DownloadId);
            merchandiseReturnNoteModels.Add(new MerchandiseReturnModel.MerchandiseReturnNote {
                Id = merchandiseReturnNote.Id,
                MerchandiseReturnId = merchandiseReturn.Id,
                DownloadId = string.IsNullOrEmpty(merchandiseReturnNote.DownloadId)
                    ? ""
                    : merchandiseReturnNote.DownloadId,
                DownloadGuid = download?.DownloadGuid ?? Guid.Empty,
                DisplayToCustomer = merchandiseReturnNote.DisplayToCustomer,
                Note = merchandiseReturnNote.Note,
                CreatedOn = dateTimeService.ConvertToUserTime(merchandiseReturnNote.CreatedOnUtc, DateTimeKind.Utc),
                CreatedByCustomer = merchandiseReturnNote.CreatedByCustomer
            });
        }

        return merchandiseReturnNoteModels;
    }

    public virtual async Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, Order order,
        string downloadId, bool displayToCustomer, string message)
    {
        var merchandiseReturnNote = new MerchandiseReturnNote {
            DisplayToCustomer = displayToCustomer,
            Note = message,
            DownloadId = downloadId,
            MerchandiseReturnId = merchandiseReturn.Id
        };
        await merchandiseReturnService.InsertMerchandiseReturnNote(merchandiseReturnNote);

        //new merchandise return notification
        if (displayToCustomer)
            //email
            await messageProviderService.SendNewMerchandiseReturnNoteAddedCustomerMessage(merchandiseReturn,
                merchandiseReturnNote, order);
    }

    public virtual async Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id)
    {
        var merchandiseReturnNote =
            (await merchandiseReturnService.GetMerchandiseReturnNotes(merchandiseReturn.Id)).FirstOrDefault(on =>
                on.Id == id);
        if (merchandiseReturnNote == null)
            throw new ArgumentException("No merchandise return note found with the specified id");

        merchandiseReturnNote.MerchandiseReturnId = merchandiseReturn.Id;
        await merchandiseReturnService.DeleteMerchandiseReturnNote(merchandiseReturnNote);

        //delete an old "attachment" file
        if (!string.IsNullOrEmpty(merchandiseReturnNote.DownloadId))
        {
            var attachment = await downloadService.GetDownloadById(merchandiseReturnNote.DownloadId);
            if (attachment != null)
                await downloadService.DeleteDownload(attachment);
        }
    }

    #region Fields

    #endregion Fields

    #region Constructors

    #endregion
}