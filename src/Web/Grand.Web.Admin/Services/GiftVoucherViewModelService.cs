using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class GiftVoucherViewModelService : IGiftVoucherViewModelService
    {
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
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IWorkContext _workContext;
        #endregion

        #region Constructors

        public GiftVoucherViewModelService(
            IGiftVoucherService giftVoucherService, IOrderService orderService,
            IPriceFormatter priceFormatter, IMessageProviderService messageProviderService,
            IDateTimeService dateTimeService,
            ICurrencyService currencyService,
            LanguageSettings languageSettings,
            ITranslationService translationService, ILanguageService languageService,
            ICustomerActivityService customerActivityService, IWorkContext workContext)
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
            _customerActivityService = customerActivityService;
            _workContext = workContext;
        }

        #endregion

        public virtual async Task<GiftVoucherModel> PrepareGiftVoucherModel()
        {
            var model = new GiftVoucherModel();
            foreach (var currency in await _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem { Text = currency.Name, Value = currency.CurrencyCode });
            return model;
        }
        public virtual async Task<GiftVoucherModel> PrepareGiftVoucherModel(GiftVoucherModel model)
        {
            foreach (var currency in await _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem { Text = currency.Name, Value = currency.CurrencyCode });

            return model;
        }

        public virtual GiftVoucherListModel PrepareGiftVoucherListModel()
        {
            var model = new GiftVoucherListModel();
            model.ActivatedList.Add(new SelectListItem
            {
                Value = "",
                Text = _translationService.GetResource("Admin.GiftVouchers.List.Activated.All")
            });
            model.ActivatedList.Add(new SelectListItem
            {
                Value = "1",
                Text = _translationService.GetResource("Admin.GiftVouchers.List.Activated.ActivatedOnly")
            });
            model.ActivatedList.Add(new SelectListItem
            {
                Value = "2",
                Text = _translationService.GetResource("Admin.GiftVouchers.List.Activated.DeactivatedOnly")
            });
            return model;
        }
        public virtual async Task<(IEnumerable<GiftVoucherModel> giftVoucherModels, int totalCount)> PrepareGiftVoucherModel(GiftVoucherListModel model, int pageIndex, int pageSize)
        {
            bool? isGiftVoucherActivated = null;
            if (model.ActivatedId == 1)
                isGiftVoucherActivated = true;
            else if (model.ActivatedId == 2)
                isGiftVoucherActivated = false;
            var giftVouchers = await _giftVoucherService.GetAllGiftVouchers(isGiftVoucherActivated: isGiftVoucherActivated,
                giftVoucherCouponCode: model.CouponCode,
                recipientName: model.RecipientName,
                pageIndex: pageIndex - 1, pageSize: pageSize);

            var giftvouchers = new List<GiftVoucherModel>();
            foreach (var item in giftVouchers)
            {
                var gift = item.ToModel(_dateTimeService);
                var currency = await _currencyService.GetCurrencyByCode(item.CurrencyCode);
                if (currency == null)
                    currency = await _currencyService.GetPrimaryStoreCurrency();

                gift.RemainingAmountStr = _priceFormatter.FormatPrice(item.GetGiftVoucherRemainingAmount(), currency, _workContext.WorkingLanguage, true, false);
                gift.AmountStr = _priceFormatter.FormatPrice(item.Amount, currency, _workContext.WorkingLanguage, true, false);
                gift.CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc);

                giftvouchers.Add(gift);
            }

            return (giftvouchers, giftVouchers.TotalCount);
        }
        public virtual async Task<GiftVoucher> InsertGiftVoucherModel(GiftVoucherModel model)
        {
            var giftVoucher = model.ToEntity(_dateTimeService);
            giftVoucher.CreatedOnUtc = DateTime.UtcNow;
            await _giftVoucherService.InsertGiftVoucher(giftVoucher);

            //activity log
            await _customerActivityService.InsertActivity("AddNewGiftVoucher", giftVoucher.Id, _translationService.GetResource("ActivityLog.AddNewGiftVoucher"), giftVoucher.Code);
            return giftVoucher;
        }
        public virtual async Task<Order> FillGiftVoucherModel(GiftVoucher giftVoucher, GiftVoucherModel model)
        {
            Order order = null;
            if (giftVoucher.PurchasedWithOrderItem != null)
                order = await _orderService.GetOrderByOrderItemId(giftVoucher.PurchasedWithOrderItem.Id);

            var currency = await _currencyService.GetCurrencyByCode(giftVoucher.CurrencyCode);
            if (currency == null)
                currency = await _currencyService.GetPrimaryStoreCurrency();

            model.PurchasedWithOrderId = giftVoucher.PurchasedWithOrderItem != null ? order.Id : null;
            model.RemainingAmountStr = _priceFormatter.FormatPrice(giftVoucher.GetGiftVoucherRemainingAmount(), currency, _workContext.WorkingLanguage, true, false);
            model.AmountStr = _priceFormatter.FormatPrice(giftVoucher.Amount, currency, _workContext.WorkingLanguage, true, false);
            model.CreatedOn = _dateTimeService.ConvertToUserTime(giftVoucher.CreatedOnUtc, DateTimeKind.Utc);
            model.CurrencyCode = giftVoucher.CurrencyCode;
            return order;
        }
        public virtual async Task NotifyRecipient(GiftVoucher giftVoucher, GiftVoucherModel model)
        {
            model = giftVoucher.ToModel(_dateTimeService);
            var order = await FillGiftVoucherModel(giftVoucher, model);
            var languageId = "";
            if (order != null)
            {
                var customerLang = await _languageService.GetLanguageById(order.CustomerLanguageId);
                if (customerLang == null)
                    customerLang = (await _languageService.GetAllLanguages()).FirstOrDefault();
                if (customerLang != null)
                    languageId = customerLang.Id;
            }
            else
            {
                languageId = _languageSettings.DefaultAdminLanguageId;
            }
            int queuedEmailId = await _messageProviderService.SendGiftVoucherMessage(giftVoucher, order, languageId);
            if (queuedEmailId > 0)
            {
                giftVoucher.IsRecipientNotified = true;
                await _giftVoucherService.UpdateGiftVoucher(giftVoucher);
                model.IsRecipientNotified = true;
            }

        }
        public virtual async Task<GiftVoucher> UpdateGiftVoucherModel(GiftVoucher giftVoucher, GiftVoucherModel model)
        {

            giftVoucher = model.ToEntity(giftVoucher, _dateTimeService);
            await _giftVoucherService.UpdateGiftVoucher(giftVoucher);
            //activity log
            await _customerActivityService.InsertActivity("EditGiftVoucher", giftVoucher.Id, _translationService.GetResource("ActivityLog.EditGiftVoucher"), giftVoucher.Code);

            return giftVoucher;
        }
        public virtual async Task DeleteGiftVoucher(GiftVoucher giftVoucher)
        {
            await _giftVoucherService.DeleteGiftVoucher(giftVoucher);
            //activity log
            await _customerActivityService.InsertActivity("DeleteGiftVoucher", giftVoucher.Id, _translationService.GetResource("ActivityLog.DeleteGiftVoucher"), giftVoucher.Code);
        }
        public virtual async Task<GiftVoucherModel> PrepareGiftVoucherModel(GiftVoucher giftVoucher)
        {
            var model = giftVoucher.ToModel(_dateTimeService);
            Order order = null;
            if (giftVoucher.PurchasedWithOrderItem != null)
                order = await _orderService.GetOrderByOrderItemId(giftVoucher.PurchasedWithOrderItem.Id);

            var currency = await _currencyService.GetCurrencyByCode(giftVoucher.CurrencyCode);
            if (currency == null)
                currency = await _currencyService.GetPrimaryStoreCurrency();

            model.PurchasedWithOrderId = giftVoucher.PurchasedWithOrderItem != null ? order?.Id : null;
            model.PurchasedWithOrderNumber = order?.OrderNumber ?? 0;
            model.RemainingAmountStr = _priceFormatter.FormatPrice(giftVoucher.GetGiftVoucherRemainingAmount(), currency, _workContext.WorkingLanguage, true, false);
            model.AmountStr = _priceFormatter.FormatPrice(giftVoucher.Amount, currency, _workContext.WorkingLanguage, true, false);
            model.CreatedOn = _dateTimeService.ConvertToUserTime(giftVoucher.CreatedOnUtc, DateTimeKind.Utc);
            model.CurrencyCode = giftVoucher.CurrencyCode;
            return model;
        }
        public virtual async Task<(IEnumerable<GiftVoucherModel.GiftVoucherUsageHistoryModel> giftVoucherUsageHistoryModels, int totalCount)> PrepareGiftVoucherUsageHistoryModels(GiftVoucher giftVoucher, int pageIndex, int pageSize)
        {
            var currency = await _currencyService.GetCurrencyByCode(giftVoucher.CurrencyCode);
            if (currency == null)
                currency = await _currencyService.GetPrimaryStoreCurrency();

            var items = new List<GiftVoucherModel.GiftVoucherUsageHistoryModel>();
            foreach (var x in giftVoucher.GiftVoucherUsageHistory.OrderByDescending(gcuh => gcuh.CreatedOnUtc))
            {
                var order = await _orderService.GetOrderById(x.UsedWithOrderId);
                items.Add(new GiftVoucherModel.GiftVoucherUsageHistoryModel
                {
                    Id = x.Id,
                    OrderId = x.UsedWithOrderId,
                    OrderNumber = order != null ? order.OrderNumber : 0,
                    UsedValue = _priceFormatter.FormatPrice(x.UsedValue, currency, _workContext.WorkingLanguage, true, false),
                    CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            return (items.Skip((pageIndex - 1) * pageSize).Take(pageSize), items.Count);
        }

    }
}
