using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class MerchandiseReturnViewModelService(
    IOrderService orderService,
    IProductService productService,
    ICustomerService customerService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
    LanguageSettings languageSettings,
    IMerchandiseReturnService merchandiseReturnService,
    IPriceFormatter priceFormatter,
    AddressSettings addressSettings,
    ICountryService countryService,
    IAddressAttributeService addressAttributeService,
    IAddressAttributeParser addressAttributeParser,
    IDownloadService downloadService,
    OrderSettings orderSettings,
    IEnumTranslationService enumTranslationService,
    IContextAccessor contextAccessor,
    CustomerConfig customerConfig,
    IAdminDataScope<MerchandiseReturn> scope,
    IMediator mediator,
    IResourceLock resourceLock,
    IInventoryDocumentService inventoryDocumentService)
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

        model.Total = priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency, contextAccessor.WorkContext.WorkingCurrency);
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
            //with per-store customer identity the same e-mail may exist in several stores - scope
            //the lookup to the current store so the search matches this store's customer (folded in
            //from Vendor's original service, spec §2.4/§11 - approved, disclosed behavior change for
            //Admin/Store when CustomerConfig.RegisterCustomersPerStore is on)
            var storeId = customerConfig.RegisterCustomersPerStore
                ? contextAccessor.StoreContext.CurrentStore.Id
                : "";
            var customer = await customerService.GetCustomerByEmail(model.SearchCustomerEmail.ToLowerInvariant(), storeId);
            customerId = customer != null ? customer.Id : "00000000-0000-0000-0000-000000000000";
        }

        DateTime? startDateValue = model.StartDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.StartDate.Value, dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.EndDate.Value, dateTimeService.CurrentTimeZone);

        var merchandiseReturns = await merchandiseReturnService.SearchMerchandiseReturns(
            storeId: scope.DefaultStoreId ?? "",
            customerId: customerId,
            vendorId: scope.DefaultVendorId ?? "",
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

        if (states?.Count > 0)
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
        await merchandiseReturnMessageProviderService.SendMerchandiseReturnStatusChangedCustomerMessage(merchandiseReturn, order,
            languageSettings.DefaultAdminLanguageId);
    }

    public virtual MerchandiseReturnListModel PrepareReturnRequestListModel()
    {
        var model = new MerchandiseReturnListModel {
            //Merchandise return status
            MerchandiseReturnStatus = enumTranslationService.ToSelectList(MerchandiseReturnStatus.Pending, false).ToList()
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
            ArgumentNullException.ThrowIfNull(orderItem);
            items.Add(new MerchandiseReturnModel.MerchandiseReturnItemModel {
                ProductId = orderItem.ProductId,
                ProductName = (await productService.GetProductByIdIncludeArch(orderItem.ProductId)).Name,
                ProductSku = orderItem.Sku,
                Quantity = item.Quantity,
                UnitPrice = priceFormatter.FormatPrice(orderItem.UnitPriceInclTax, contextAccessor.WorkContext.WorkingCurrency),
                ReasonForReturn = item.ReasonForReturn,
                RequestedAction = item.RequestedAction
            });
        }

        return items;
    }

    public virtual async Task<(MerchandiseReturn MerchandiseReturn, string Error)> UpdateMerchandiseReturnModel(
        MerchandiseReturn merchandiseReturn, MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes)
    {
        //the lock the storefront holds to submit a return for the order
        await using (await resourceLock.Acquire(
                         [MerchandiseReturnExtensions.QuantityLockKey(merchandiseReturn.OrderId)]))
        {
            var error = await StatusChangeError(merchandiseReturn,
                (MerchandiseReturnStatus)model.MerchandiseReturnStatusId);
            if (error != null)
                return (null, error);

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
        }

        if (model.NotifyCustomer)
            await NotifyCustomer(merchandiseReturn);
        return (merchandiseReturn, null);
    }

    /// <summary>
    ///     Why the status may not change, as a resource key without the panel's prefix; null when it may. Checked
    ///     against the status stored now, not the copy loaded before the lock, which another save may have changed
    /// </summary>
    private async Task<string> StatusChangeError(MerchandiseReturn merchandiseReturn, MerchandiseReturnStatus status)
    {
        var stored = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturn.Id) ?? merchandiseReturn;
        var takesQuantity = stored.MerchandiseReturnStatus.TakesQuantity();

        //a rejected or cancelled return set back takes its quantity again, so that quantity must still be left
        if (!takesQuantity && status.TakesQuantity() && !await IsQuantityLeft(stored))
            return "Orders.MerchandiseReturns.QuantityExceeded";

        //rejecting or cancelling gives the quantity back to the customer, so none of it may be in stock already
        if (takesQuantity && !status.TakesQuantity() && await IsReceived(stored))
            return "Orders.MerchandiseReturns.AlreadyReceived";

        return null;
    }

    private async Task<bool> IsReceived(MerchandiseReturn merchandiseReturn)
    {
        var receipts = await inventoryDocumentService.Search(new InventoryDocumentSearch {
            Type = InventoryDocumentType.ReturnReceipt,
            Status = InventoryDocumentStatus.Posted,
            MerchandiseReturnId = merchandiseReturn.Id
        }, 0, 1);
        return receipts.Count > 0;
    }

    private async Task<bool> IsQuantityLeft(MerchandiseReturn merchandiseReturn)
    {
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        if (order == null)
            return false;

        var returnable = await mediator.Send(new GetReturnableQuantitiesQuery { Order = order, ForStaff = true });
        return merchandiseReturn.MerchandiseReturnItems.GroupBy(x => x.OrderItemId)
            .All(x => x.Sum(item => item.Quantity) <= returnable.GetValueOrDefault(x.Key));
    }

    public virtual async Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn)
    {
        //the order's returned quantities are counted again under the lock its returns are saved with
        await using (await resourceLock.Acquire(
                         [MerchandiseReturnExtensions.QuantityLockKey(merchandiseReturn.OrderId)]))
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
            await merchandiseReturnMessageProviderService.SendNewMerchandiseReturnNoteAddedCustomerMessage(merchandiseReturn,
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