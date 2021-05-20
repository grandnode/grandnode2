using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Affiliates;
using Grand.Domain.Directory;
using Grand.Domain.Payments;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Affiliates;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class AffiliateViewModelService : IAffiliateViewModelService
    {
        private readonly IWorkContext _workContext;
        private readonly ICountryService _countryService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IAffiliateService _affiliateService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly SeoSettings _seoSettings;

        public AffiliateViewModelService(IWorkContext workContext, ICountryService countryService,
            IPriceFormatter priceFormatter, IAffiliateService affiliateService,
            ICustomerService customerService, IOrderService orderService, ITranslationService translationService, IDateTimeService dateTimeService,
            IOrderStatusService orderStatusService,
            SeoSettings seoSettings)
        {
            _workContext = workContext;
            _countryService = countryService;
            _priceFormatter = priceFormatter;
            _affiliateService = affiliateService;
            _customerService = customerService;
            _orderService = orderService;
            _translationService = translationService;
            _dateTimeService = dateTimeService;
            _orderStatusService = orderStatusService;
            _seoSettings = seoSettings;
        }

        public virtual async Task PrepareAffiliateModel(AffiliateModel model, Affiliate affiliate, bool excludeProperties,
            bool prepareEntireAddressModel = true)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (affiliate != null)
            {
                model.Id = affiliate.Id;
                model.Name = affiliate.Name;
                model.Url = affiliate.GenerateUrl(_workContext);
                if (!excludeProperties)
                {
                    model.AdminComment = affiliate.AdminComment;
                    model.FriendlyUrlName = string.IsNullOrEmpty(affiliate.FriendlyUrlName) ? "" : affiliate.FriendlyUrlName.ToLowerInvariant();
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
                model.Address.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
                foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                    model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (affiliate != null && c.Id == affiliate.Address.CountryId) });

                var states = !String.IsNullOrEmpty(model.Address.CountryId) ? (await _countryService.GetCountryById(model.Address.CountryId))?.StateProvinces : new List<StateProvince>();
                if (states.Count > 0)
                {
                    foreach (var s in states)
                        model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (affiliate != null && s.Id == affiliate.Address.StateProvinceId) });
                }
            }
        }

        public virtual async Task<(IEnumerable<AffiliateModel> affiliateModels, int totalCount)> PrepareAffiliateModelList(AffiliateListModel model, int pageIndex, int pageSize)
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
            var affiliate = new Affiliate();
            affiliate.Active = model.Active;
            affiliate.AdminComment = model.AdminComment;
            affiliate.Name = model.Name;
            //validate friendly URL name
            var friendlyUrlName = await affiliate.ValidateFriendlyUrlName(_affiliateService, _seoSettings, model.FriendlyUrlName, model.Name);
            affiliate.FriendlyUrlName = friendlyUrlName.ToLowerInvariant();
            affiliate.Address = model.Address.ToEntity();
            affiliate.Address.CreatedOnUtc = DateTime.UtcNow;
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
            var friendlyUrlName = await affiliate.ValidateFriendlyUrlName(_affiliateService, _seoSettings, model.FriendlyUrlName, model.Name);
            affiliate.FriendlyUrlName = friendlyUrlName.ToLowerInvariant();
            affiliate.Address = model.Address.ToEntity(affiliate.Address);
            await _affiliateService.UpdateAffiliate(affiliate);
            return affiliate;
        }
        public virtual async Task<(IEnumerable<AffiliateModel.AffiliatedOrderModel> affiliateOrderModels, int totalCount)> PrepareAffiliatedOrderList(Affiliate affiliate, AffiliatedOrderListModel model, int pageIndex, int pageSize)
        {
            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)(model.PaymentStatusId) : null;
            ShippingStatus? shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)(model.ShippingStatusId) : null;

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
                var orderModel = new AffiliateModel.AffiliatedOrderModel
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderCode = order.Code,
                    OrderStatus = statuses.FirstOrDefault(y => y.StatusId == order.OrderStatusId)?.Name,
                    PaymentStatus = order.PaymentStatusId.GetTranslationEnum(_translationService, _workContext),
                    ShippingStatus = order.ShippingStatusId.GetTranslationEnum(_translationService, _workContext),
                    OrderTotal = await _priceFormatter.FormatPrice(order.OrderTotal, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage),
                    CreatedOn = _dateTimeService.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc)
                };
                affiliateorders.Add(orderModel);
            }

            return (affiliateorders, orders.TotalCount);
        }

        public virtual async Task<(IEnumerable<AffiliateModel.AffiliatedCustomerModel> affiliateCustomerModels, int totalCount)> PrepareAffiliatedCustomerList(Affiliate affiliate, int pageIndex, int pageSize)
        {
            var customers = await _customerService.GetAllCustomers(
                affiliateId: affiliate.Id,
                pageIndex: pageIndex - 1,
                pageSize: pageSize);

            return (customers.Select(customer =>
                {
                    var customerModel = new AffiliateModel.AffiliatedCustomerModel
                    {
                        Id = customer.Id,
                        Name = customer.Email
                    };
                    return customerModel;
                }), customers.TotalCount);
        }

    }
}
