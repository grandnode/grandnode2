using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class GiftVoucherViewModelService : IGiftVoucherViewModelService
{
    #region Constructors

    public GiftVoucherViewModelService(
        IGiftVoucherService giftVoucherService, IOrderService orderService,
        IPriceFormatter priceFormatter, IMessageProviderService messageProviderService,
        IDateTimeService dateTimeService,
        ICurrencyService currencyService,
        LanguageSettings languageSettings,
        ITranslationService translationService, ILanguageService languageService,
        IWorkContext workContext,
        IStoreService storeService)
    {
        _giftVoucherService = giftVoucherService;
        _orderService = orderService;
        _priceFormatter = priceFormatter;
        _messageProviderService = messageProviderService;
        _dateTimeService = dateTimeService;
        _currencyService = currencyService;
        _languageSettings = languageSettings;
        _translationService = translationService;
        _languageService = languageService;
        _storeService = storeService;
    }

    #endregion

    public virtual async Task<GiftVoucherModel> PrepareGiftVoucherModel(GiftVoucherModel model = null)
    {
        model ??= new GiftVoucherModel();

        foreach (var currency in await _currencyService.GetAllCurrencies())
            model.AvailableCurrencies.Add(
                new SelectListItem { Text = currency.Name, Value = currency.CurrencyCode });

        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        return model;
    }

    public virtual GiftVoucherListModel PrepareGiftVoucherListModel()
    {
        var model = new GiftVoucherListModel();
        model.ActivatedList.Add(new SelectListItem {
            Value = "",
            Text = _translationService.GetResource("Admin.GiftVouchers.List.Activated.All")
        });
        model.ActivatedList.Add(new SelectListItem {
            Value = "1",
            Text = _translationService.GetResource("Admin.GiftVouchers.List.Activated.ActivatedOnly")
        });
        model.ActivatedList.Add(new SelectListItem {
            Value = "2",
            Text = _translationService.GetResource("Admin.GiftVouchers.List.Activated.DeactivatedOnly")
        });
        return model;
    }

    public virtual async Task<(IEnumerable<GiftVoucherModel> giftVoucherModels, int totalCount)>
        PrepareGiftVoucherModel(GiftVoucherListModel model, int pageIndex, int pageSize)
    {
        bool? isGiftVoucherActivated = null;
        switch (model.ActivatedId)
        {
            case 1:
                isGiftVoucherActivated = true;
                break;
            case 2:
                isGiftVoucherActivated = false;
                break;
        }

        var giftVouchers = await _giftVoucherService.GetAllGiftVouchers(
            isGiftVoucherActivated: isGiftVoucherActivated,
            giftVoucherCouponCode: model.CouponCode,
            recipientName: model.RecipientName,
            pageIndex: pageIndex - 1, pageSize: pageSize);

        var giftvouchers = new List<GiftVoucherModel>();
        foreach (var item in giftVouchers)
        {
            var gift = item.ToModel(_dateTimeService);
            var currency = await _currencyService.GetCurrencyByCode(item.CurrencyCode) ??
                           await _currencyService.GetPrimaryStoreCurrency();

            gift.RemainingAmountStr = _priceFormatter.FormatPrice(item.GetGiftVoucherRemainingAmount(), currency);
            gift.AmountStr = _priceFormatter.FormatPrice(item.Amount, currency);
            gift.CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc);

            giftvouchers.Add(gift);
        }

        return (giftvouchers, giftVouchers.TotalCount);
    }

    public virtual async Task<GiftVoucher> InsertGiftVoucherModel(GiftVoucherModel model)
    {
        var giftVoucher = model.ToEntity(_dateTimeService);
        await _giftVoucherService.InsertGiftVoucher(giftVoucher);
        return giftVoucher;
    }

    public virtual async Task<Order> GetOrderFromGiftVoucher(GiftVoucher giftVoucher)
    {
        Order order = null;
        if (giftVoucher.PurchasedWithOrderItem != null)
            order = await _orderService.GetOrderByOrderItemId(giftVoucher.PurchasedWithOrderItem.Id);

        return order;
    }

    public virtual async Task<GiftVoucherModel> FillGiftVoucherModel(GiftVoucher giftVoucher, GiftVoucherModel model)
    {
        var currency = await _currencyService.GetCurrencyByCode(giftVoucher.CurrencyCode) ??
                       await _currencyService.GetPrimaryStoreCurrency();

        model.PurchasedWithOrderId = giftVoucher.PurchasedWithOrderItem != null
            ? (await GetOrderFromGiftVoucher(giftVoucher))?.Id
            : null;
        model.RemainingAmountStr = _priceFormatter.FormatPrice(giftVoucher.GetGiftVoucherRemainingAmount(), currency);
        model.AmountStr = _priceFormatter.FormatPrice(giftVoucher.Amount, currency);
        model.CreatedOn = _dateTimeService.ConvertToUserTime(giftVoucher.CreatedOnUtc, DateTimeKind.Utc);
        model.CurrencyCode = giftVoucher.CurrencyCode;
        return model;
    }

    public virtual async Task NotifyRecipient(GiftVoucher giftVoucher)
    {
        var order = await GetOrderFromGiftVoucher(giftVoucher);
        var languageId = "";
        if (order != null)
        {
            var customerLang = await _languageService.GetLanguageById(order.CustomerLanguageId) ??
                               (await _languageService.GetAllLanguages()).FirstOrDefault();
            if (customerLang != null)
                languageId = customerLang.Id;
        }
        else
        {
            languageId = _languageSettings.DefaultAdminLanguageId;
        }

        var queuedEmailId = await _messageProviderService.SendGiftVoucherMessage(giftVoucher, order, languageId);
        if (queuedEmailId > 0)
        {
            giftVoucher.IsRecipientNotified = true;
            await _giftVoucherService.UpdateGiftVoucher(giftVoucher);
        }
    }

    public virtual async Task<GiftVoucher> UpdateGiftVoucherModel(GiftVoucher giftVoucher, GiftVoucherModel model)
    {
        giftVoucher = model.ToEntity(giftVoucher, _dateTimeService);
        await _giftVoucherService.UpdateGiftVoucher(giftVoucher);

        return giftVoucher;
    }

    public virtual async Task DeleteGiftVoucher(GiftVoucher giftVoucher)
    {
        await _giftVoucherService.DeleteGiftVoucher(giftVoucher);
    }

    public virtual async Task<GiftVoucherModel> PrepareGiftVoucherModel(GiftVoucher giftVoucher)
    {
        var model = giftVoucher.ToModel(_dateTimeService);
        model = await PrepareGiftVoucherModel(model);
        Order order = null;
        if (giftVoucher.PurchasedWithOrderItem != null)
            order = await _orderService.GetOrderByOrderItemId(giftVoucher.PurchasedWithOrderItem.Id);

        var currency = await _currencyService.GetCurrencyByCode(giftVoucher.CurrencyCode) ??
                       await _currencyService.GetPrimaryStoreCurrency();

        model.PurchasedWithOrderId = giftVoucher.PurchasedWithOrderItem != null ? order?.Id : null;
        model.PurchasedWithOrderNumber = order?.OrderNumber ?? 0;
        model.RemainingAmountStr = _priceFormatter.FormatPrice(giftVoucher.GetGiftVoucherRemainingAmount(), currency);
        model.AmountStr = _priceFormatter.FormatPrice(giftVoucher.Amount, currency);
        model.CreatedOn = _dateTimeService.ConvertToUserTime(giftVoucher.CreatedOnUtc, DateTimeKind.Utc);
        model.CurrencyCode = giftVoucher.CurrencyCode;
        return model;
    }

    public virtual async
        Task<(IEnumerable<GiftVoucherModel.GiftVoucherUsageHistoryModel> giftVoucherUsageHistoryModels, int
            totalCount)> PrepareGiftVoucherUsageHistoryModels(GiftVoucher giftVoucher, int pageIndex, int pageSize)
    {
        var currency = await _currencyService.GetCurrencyByCode(giftVoucher.CurrencyCode) ??
                       await _currencyService.GetPrimaryStoreCurrency();

        var items = new List<GiftVoucherModel.GiftVoucherUsageHistoryModel>();
        foreach (var x in giftVoucher.GiftVoucherUsageHistory.OrderByDescending(gcuh => gcuh.CreatedOnUtc))
        {
            var order = await _orderService.GetOrderById(x.UsedWithOrderId);
            items.Add(new GiftVoucherModel.GiftVoucherUsageHistoryModel {
                Id = x.Id,
                OrderId = x.UsedWithOrderId,
                OrderNumber = order?.OrderNumber ?? 0,
                UsedValue = _priceFormatter.FormatPrice(x.UsedValue, currency),
                CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return (items.Skip((pageIndex - 1) * pageSize).Take(pageSize), items.Count);
    }

    #region Fields

    private readonly IGiftVoucherService _giftVoucherService;
    private readonly IOrderService _orderService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrencyService _currencyService;
    private readonly LanguageSettings _languageSettings;
    private readonly ITranslationService _translationService;
    private readonly ILanguageService _languageService;
    private readonly IStoreService _storeService;

    #endregion
}