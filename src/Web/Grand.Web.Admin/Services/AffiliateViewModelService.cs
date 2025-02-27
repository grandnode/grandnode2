using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Affiliates;
using Grand.Domain.Directory;
using Grand.Domain.Payments;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Affiliates;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class AffiliateViewModelService : IAffiliateViewModelService
{
    private readonly IAffiliateService _affiliateService;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IOrderService _orderService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly SeoSettings _seoSettings;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IEnumTranslationService _enumTranslationService;
    public AffiliateViewModelService(IContextAccessor contextAccessor, ICountryService countryService,
        IPriceFormatter priceFormatter, IAffiliateService affiliateService,
        ICustomerService customerService, IOrderService orderService, ITranslationService translationService,
        IDateTimeService dateTimeService,
        IOrderStatusService orderStatusService,
        SeoSettings seoSettings, ICurrencyService currencyService, IEnumTranslationService enumTranslationService)
    {
        _contextAccessor = contextAccessor;
        _countryService = countryService;
        _priceFormatter = priceFormatter;
        _affiliateService = affiliateService;
        _customerService = customerService;
        _orderService = orderService;
        _translationService = translationService;
        _dateTimeService = dateTimeService;
        _orderStatusService = orderStatusService;
        _seoSettings = seoSettings;
        _currencyService = currencyService;
        _enumTranslationService = enumTranslationService;
    }

    public virtual async Task PrepareAffiliateModel(AffiliateModel model, Affiliate affiliate, bool excludeProperties,
        bool prepareEntireAddressModel = true)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (affiliate != null)
        {
            model.Id = affiliate.Id;
            model.Name = affiliate.Name;

            var host = _contextAccessor.StoreContext.CurrentHost == null
                ? _contextAccessor.StoreContext.CurrentStore.Url.TrimEnd('/')
                : _contextAccessor.StoreContext.CurrentHost.Url.TrimEnd('/');
            model.Url = affiliate.GenerateUrl(host);
            if (!excludeProperties)
            {
                model.AdminComment = affiliate.AdminComment;
                model.FriendlyUrlName = string.IsNullOrEmpty(affiliate.FriendlyUrlName)
                    ? ""
                    : affiliate.FriendlyUrlName.ToLowerInvariant();
                model.Active = affiliate.Active;
                model.Address = await affiliate.Address.ToModel(_countryService);
            }
        }

        if (prepareEntireAddressModel)
        {
            model.Address.FirstNameEnabled = true;
            model.Address.FirstNameRequired = true;
            model.Address.LastNameEnabled = true;
            model.Address.LastNameRequired = true;
            model.Address.EmailEnabled = true;
            model.Address.EmailRequired = true;
            model.Address.CompanyEnabled = true;
            model.Address.CountryEnabled = true;
            model.Address.StateProvinceEnabled = true;
            model.Address.CityEnabled = true;
            model.Address.CityRequired = true;
            model.Address.StreetAddressEnabled = true;
            model.Address.StreetAddressRequired = true;
            model.Address.StreetAddress2Enabled = true;
            model.Address.ZipPostalCodeEnabled = true;
            model.Address.ZipPostalCodeRequired = true;
            model.Address.PhoneEnabled = true;
            model.Address.PhoneRequired = true;
            model.Address.FaxEnabled = true;

            //address
            model.Address.AvailableCountries.Add(new SelectListItem
                { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem {
                    Text = c.Name, Value = c.Id, Selected = affiliate != null && c.Id == affiliate.Address.CountryId
                });

            var states = !string.IsNullOrEmpty(model.Address.CountryId)
                ? (await _countryService.GetCountryById(model.Address.CountryId))?.StateProvinces
                : new List<StateProvince>();

            if (states?.Count > 0)
                foreach (var s in states)
                    model.Address.AvailableStates.Add(new SelectListItem {
                        Text = s.Name, Value = s.Id,
                        Selected = affiliate != null && s.Id == affiliate.Address.StateProvinceId
                    });
        }
    }

    public virtual async Task<(IEnumerable<AffiliateModel> affiliateModels, int totalCount)> PrepareAffiliateModelList(
        AffiliateListModel model, int pageIndex, int pageSize)
    {
        var affiliates = await _affiliateService.GetAllAffiliates(model.SearchFriendlyUrlName,
            model.SearchFirstName, model.SearchLastName,
            model.LoadOnlyWithOrders, model.OrdersCreatedFromUtc, model.OrdersCreatedToUtc,
            pageIndex - 1, pageSize, true);

        var affiliateModels = new List<AffiliateModel>();
        foreach (var x in affiliates)
        {
            var m = new AffiliateModel();
            await PrepareAffiliateModel(m, x, false, false);
            affiliateModels.Add(m);
        }

        return (affiliateModels, affiliates.TotalCount);
    }

    public virtual async Task<Affiliate> InsertAffiliateModel(AffiliateModel model)
    {
        var affiliate = new Affiliate {
            Active = model.Active,
            AdminComment = model.AdminComment,
            Name = model.Name
        };
        //validate friendly URL name
        var friendlyUrlName = await ValidateFriendlyUrlName(affiliate, model.FriendlyUrlName, model.Name);
        affiliate.FriendlyUrlName = friendlyUrlName.ToLowerInvariant();
        affiliate.Address = model.Address.ToEntity();
        //some validation
        await _affiliateService.InsertAffiliate(affiliate);
        return affiliate;
    }

    public virtual async Task<Affiliate> UpdateAffiliateModel(AffiliateModel model, Affiliate affiliate)
    {
        affiliate.Active = model.Active;
        affiliate.AdminComment = model.AdminComment;
        affiliate.Name = model.Name;
        //validate friendly URL name
        var friendlyUrlName = await ValidateFriendlyUrlName(affiliate, model.FriendlyUrlName, model.Name);
        affiliate.FriendlyUrlName = friendlyUrlName.ToLowerInvariant();
        affiliate.Address = model.Address.ToEntity(affiliate.Address);
        await _affiliateService.UpdateAffiliate(affiliate);
        return affiliate;
    }

    public virtual async Task<(IEnumerable<AffiliateModel.AffiliatedOrderModel> affiliateOrderModels, int totalCount)>
        PrepareAffiliatedOrderList(Affiliate affiliate, AffiliatedOrderListModel model, int pageIndex, int pageSize)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;
        var shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)model.ShippingStatusId : null;

        var orders = await _orderService.SearchOrders(
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            os: orderStatus,
            ps: paymentStatus,
            ss: shippingStatus,
            affiliateId: affiliate.Id,
            pageIndex: pageIndex - 1,
            pageSize: pageSize);

        var statuses = await _orderStatusService.GetAll();

        var affiliateorders = new List<AffiliateModel.AffiliatedOrderModel>();
        foreach (var order in orders)
        {
            var orderModel = new AffiliateModel.AffiliatedOrderModel {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderCode = order.Code,
                OrderStatus = statuses.FirstOrDefault(y => y.StatusId == order.OrderStatusId)?.Name,
                PaymentStatus = _enumTranslationService.GetTranslationEnum(order.PaymentStatusId),
                ShippingStatus = _enumTranslationService.GetTranslationEnum(order.ShippingStatusId),
                OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal,
                    await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode)),
                CreatedOn = _dateTimeService.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc)
            };
            affiliateorders.Add(orderModel);
        }

        return (affiliateorders, orders.TotalCount);
    }

    public virtual async
        Task<(IEnumerable<AffiliateModel.AffiliatedCustomerModel> affiliateCustomerModels, int totalCount)>
        PrepareAffiliatedCustomerList(Affiliate affiliate, int pageIndex, int pageSize)
    {
        var customers = await _customerService.GetAllCustomers(
            affiliateId: affiliate.Id,
            pageIndex: pageIndex - 1,
            pageSize: pageSize);

        return (customers.Select(customer =>
        {
            var customerModel = new AffiliateModel.AffiliatedCustomerModel {
                Id = customer.Id,
                Name = customer.Email
            };
            return customerModel;
        }), customers.TotalCount);
    }
    
    /// <summary>
    ///     Validate friendly URL name
    /// </summary>
    /// <param name="affiliate">Affiliate</param>
    /// <param name="seoSettings"></param>
    /// <param name="friendlyUrlName">Friendly URL name</param>
    /// <param name="affiliateService"></param>
    /// <param name="name"></param>
    /// <returns>Valid friendly name</returns>
    private async Task<string> ValidateFriendlyUrlName(Affiliate affiliate, string friendlyUrlName, string name)
    {
        ArgumentNullException.ThrowIfNull(affiliate);

        if (string.IsNullOrEmpty(friendlyUrlName))
            friendlyUrlName = name;

        //ensure we have only valid chars
        friendlyUrlName = SeoExtensions.GetSeName(friendlyUrlName, _seoSettings.ConvertNonWesternChars,
            _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);

        //max length
        friendlyUrlName = CommonHelper.EnsureMaximumLength(friendlyUrlName, 200);

        if (string.IsNullOrEmpty(friendlyUrlName))
            return friendlyUrlName;
        //check whether such friendly URL name already exists (and that is not the current affiliate)
        var i = 2;
        var tempName = friendlyUrlName;
        while (true)
        {
            var affiliateByFriendlyUrlName = await _affiliateService.GetAffiliateByFriendlyUrlName(tempName);
            var reserved = affiliateByFriendlyUrlName != null && affiliateByFriendlyUrlName.Id != affiliate.Id;
            if (!reserved)
                break;

            tempName = $"{friendlyUrlName}-{i}";
            i++;
        }

        friendlyUrlName = tempName;

        return friendlyUrlName;
    }
}