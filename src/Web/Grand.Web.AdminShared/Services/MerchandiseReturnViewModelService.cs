using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Grand.Web.AdminShared.Services;

public class MerchandiseReturnViewModelService(
    IOrderService orderService,
    IProductService productService,
    ICustomerService customerService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
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
    IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
    IMerchandiseReturnLabelService merchandiseReturnLabelService,
    IReturnShippingService returnShippingService,
    ICurrencyService currencyService,
    IInventoryDocumentService inventoryDocumentService,
    IWarehouseService warehouseService,
    IAdminDataScope<Warehouse> warehouseScope,
    IRequestPermissionService permissionService,
    ILogger<MerchandiseReturnViewModelService> logger)
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
        var customer = await customerService.GetCustomerById(merchandiseReturn.CustomerId);
        if (customer != null)
            model.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : translationService.GetResource("Admin.Customers.Guest");
        else
            model.CustomerInfo = translationService.GetResource("Admin.Customers.Guest");

        model.MerchandiseReturnStatusId = merchandiseReturn.MerchandiseReturnStatusId;
        model.MerchandiseReturnStatusStr =
            enumTranslationService.GetTranslationEnum(merchandiseReturn.MerchandiseReturnStatus);
        //the edit page offers Received only to recover a posted receipt the return was not marked received by
        //(e.g. busy at the time); the list asks no one
        var receivedByHand = !excludeProperties &&
                             merchandiseReturn.MerchandiseReturnStatus.CanChangeTo(MerchandiseReturnStatus.Received) &&
                             await CanMarkReceived(merchandiseReturn.Id);
        model.NextStatuses = merchandiseReturn.MerchandiseReturnStatus.NextStatuses()
            .Where(x => x != MerchandiseReturnStatus.Received || receivedByHand)
            .Select(x => (int)x).ToList();
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
            //the list leaves it out: no carrier is asked for the tracking events of every row
            model.ReturnShipping = await PrepareReturnShipping(merchandiseReturn, order);
        }

        return model;
    }

    private const int MaxTrackingNumberLength = 100;

    //the status filter's value for every status
    private const int AllStatuses = -1;

    /// <summary>
    ///     Whether the staff may mark the return received: its goods came back to stock through a posted return
    ///     receipt, which marks it received itself unless that failed
    /// </summary>
    private Task<bool> CanMarkReceived(string merchandiseReturnId)
    {
        return inventoryDocumentService.IsReturnReceived(merchandiseReturnId);
    }

    private async Task<MerchandiseReturnModel.ReturnShippingModel> PrepareReturnShipping(
        MerchandiseReturn merchandiseReturn, Order order)
    {
        var shipping = merchandiseReturn.ReturnShipping;
        if (string.IsNullOrEmpty(shipping?.ProviderSystemName))
            return null;

        var provider = returnShippingService.LoadProviderBySystemName(shipping.ProviderSystemName);
        //the cost is kept in the primary store currency, with the rate of the order's currency
        var orderCurrency = await currencyService.GetCurrencyByCode(order.CustomerCurrencyCode)
                            ?? contextAccessor.WorkContext.WorkingCurrency;
        var model = new MerchandiseReturnModel.ReturnShippingModel {
            MethodName = provider?.FriendlyName ?? shipping.ProviderSystemName,
            OptionName = shipping.OptionName,
            //"Free" as the customer reads it on the storefront
            Cost = shipping.Cost > 0
                ? priceFormatter.FormatPrice(shipping.Cost * shipping.CurrencyRate, orderCurrency)
                : translationService.GetResource(
                    $"{scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.ReturnShipping.Free"),
            DropOffPoint = FormatDropOffPoint(shipping.DropOffPoint),
            TrackingNumber = shipping.TrackingNumber,
            ShownTrackingNumber = shipping.TrackingNumber,
            ReturnCode = shipping.ReturnCode,
            Instructions = shipping.Instructions,
            HasLabel = shipping.LabelStatus == ReturnLabelStatus.Created &&
                       !string.IsNullOrEmpty(shipping.LabelDownloadId),
            LabelStatus = translationService.GetResource(
                $"{scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.ReturnShipping.LabelStatus.{shipping.LabelStatus}"),
            LabelError = shipping.LabelError,
            //the label service's own rule: a label not created yet, of a return whose goods are not back yet
            CanRetryLabel = (shipping.LabelStatus is ReturnLabelStatus.Pending or ReturnLabelStatus.Failed) &&
                            merchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Pending
                                or MerchandiseReturnStatus.Accepted
        };

        if (!string.IsNullOrEmpty(shipping.TrackingNumber) && provider?.ShipmentTracker != null)
            try
            {
                var events = await provider.ShipmentTracker.GetShipmentEvents(shipping.TrackingNumber);
                foreach (var shipmentEvent in events ?? [])
                    model.TrackingEvents.Add(new MerchandiseReturnModel.TrackingEventModel {
                        EventName = shipmentEvent.EventName,
                        Location = shipmentEvent.Location,
                        Date = shipmentEvent.Date
                    });
            }
            catch (Exception ex)
            {
                //a carrier that is down leaves the page without the events, not without the page
                logger.LogWarning(ex, "Return shipping provider {SystemName} failed to give the tracking events of " +
                                      "{TrackingNumber}", shipping.ProviderSystemName, shipping.TrackingNumber);
            }

        return model;
    }

    private static string FormatDropOffPoint(ReturnDropOffPoint point)
    {
        if (point == null)
            return null;

        var address = point.Address == null
            ? null
            : string.Join(", ", new[] { point.Address.Address1, point.Address.ZipPostalCode, point.Address.City }
                .Where(part => !string.IsNullOrEmpty(part)));
        return string.Join(", ", new[] { point.Name, address }.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string NormalizeTrackingNumber(string trackingNumber)
    {
        trackingNumber = trackingNumber?.Trim();
        if (string.IsNullOrEmpty(trackingNumber))
            return null;
        return trackingNumber.Length > MaxTrackingNumberLength
            ? trackingNumber[..MaxTrackingNumberLength]
            : trackingNumber;
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

    public virtual MerchandiseReturnListModel PrepareReturnRequestListModel()
    {
        var model = new MerchandiseReturnListModel {
            //every return is listed at first, not only the pending ones
            SearchMerchandiseReturnStatusId = AllStatuses,
            MerchandiseReturnStatus = enumTranslationService.ToSelectList(MerchandiseReturnStatus.Pending, false).ToList()
        };
        model.MerchandiseReturnStatus.Insert(0,
            new SelectListItem {
                Text = translationService.GetResource("Admin.Common.All"),
                Value = AllStatuses.ToString(CultureInfo.InvariantCulture),
                Selected = true
            });

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

    public virtual async Task PrepareReturnReceipts(MerchandiseReturnModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var receipts = await inventoryDocumentService.GetReturnReceipts(model.Id);
        //a staff member without inventory documents sees the receipts named, and creates or opens none
        var managesDocuments = await permissionService.Authorize(PermissionSystemName.InventoryDocuments);
        model.CanReceiveToStock = managesDocuments && receipts.QuantityLeft;
        model.ReturnReceipts = new List<MerchandiseReturnModel.ReturnReceiptModel>();
        var storeId = await warehouseScope.GetDefaultStoreIdAsync();
        foreach (var receipt in receipts.Receipts)
            model.ReturnReceipts.Add(new MerchandiseReturnModel.ReturnReceiptModel {
                Id = receipt.Id,
                Number = receipt.Number,
                Status = enumTranslationService.GetTranslationEnum(receipt.Status),
                //the inventory document panel's own check: the panel's store, and a warehouse it books
                CanOpen = managesDocuments && (storeId == null || receipt.StoreId == storeId) &&
                          await CanUse(receipt.WarehouseId)
            });
    }

    /// <summary>
    ///     Whether the panel books the warehouse; none chosen yet on a draft is allowed
    /// </summary>
    private async Task<bool> CanUse(string warehouseId)
    {
        if (string.IsNullOrEmpty(warehouseId))
            return true;

        var warehouse = await warehouseService.GetWarehouseById(warehouseId);
        return warehouse != null && await warehouseScope.HasAccess(warehouse);
    }

    public virtual async Task<(MerchandiseReturn MerchandiseReturn, string Error)> UpdateMerchandiseReturnModel(
        MerchandiseReturn merchandiseReturn, MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes)
    {
        //received without a posted return receipt is refused before anything is saved: no button offers it
        if (model.ChangeStatusTo == (int)MerchandiseReturnStatus.Received &&
            !await CanMarkReceived(merchandiseReturn.Id))
            return (null, MerchandiseReturnErrors.StatusChangeNotAllowed);

        //the fields go onto the return as stored: a status changed since the page was loaded is kept
        var error = await merchandiseReturnWorkflowService.Update(merchandiseReturn.Id, stored =>
        {
            stored.CustomerComments = model.CustomerComments;
            stored.StaffNotes = model.StaffNotes;
            stored.ExternalId = model.ExternalId;
            stored.UserFields = model.UserFields;

            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate)
                stored.PickupDate = model.PickupDate;
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            {
                stored.PickupAddress = model.PickupAddress.ToEntity();
                if (stored.PickupAddress != null)
                    stored.PickupAddress.Attributes = customAddressAttributes;
            }
        });
        if (error != null)
            return (null, error);

        //the tracking number goes through the label service, under the label's lock: after the save above, never
        //inside it, which holds the order's lock the label service takes second. Only a number the staff changed
        //on the page is written, and only over the number the page was shown with: one the label or the customer
        //wrote since the page was loaded is kept
        if (model.ReturnShipping != null && merchandiseReturn.ReturnShipping != null)
        {
            var trackingNumber = NormalizeTrackingNumber(model.ReturnShipping.TrackingNumber);
            var shown = NormalizeTrackingNumber(model.ReturnShipping.ShownTrackingNumber);
            if (trackingNumber != shown)
            {
                error = await merchandiseReturnLabelService.UpdateShipping(merchandiseReturn.Id,
                    shipping => shipping.TrackingNumber = trackingNumber,
                    current => NormalizeTrackingNumber(current.ReturnShipping?.TrackingNumber) == shown);
                if (error != null)
                    return (null, error);
            }
        }

        //a status button saved what was typed above and changes the status now; the reason box is posted by every
        //button, but only a rejection sends what was typed in it to the customer
        if (model.ChangeStatusTo.HasValue)
        {
            var status = (MerchandiseReturnStatus)model.ChangeStatusTo.Value;
            error = await merchandiseReturnWorkflowService.ChangeStatus(merchandiseReturn.Id, status,
                status == MerchandiseReturnStatus.Rejected ? model.ChangeStatusNote : null,
                contextAccessor.WorkContext.CurrentCustomer.StatusChangedBy());
        }

        return (merchandiseReturn, error);
    }

    public virtual Task<string> DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn)
    {
        return merchandiseReturnWorkflowService.Delete(merchandiseReturn.Id);
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