using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Models.Customers;
using Microsoft.AspNetCore.Mvc;
using Grand.Business.Core.Interfaces.Catalog.Directory;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Customers)]
    public partial class OnlineCustomerController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IGeoLookupService _geoLookupService;
        private readonly IDateTimeService _dateTimeService;
        private readonly CustomerSettings _customerSettings;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        #endregion

        #region Constructors

        public OnlineCustomerController(ICustomerService customerService,
            IGeoLookupService geoLookupService, IDateTimeService dateTimeService,
            CustomerSettings customerSettings,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _customerService = customerService;
            _geoLookupService = geoLookupService;
            _dateTimeService = dateTimeService;
            _customerSettings = customerSettings;
            _translationService = translationService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var customers = await _customerService.GetOnlineCustomers(DateTime.UtcNow.AddMinutes(-_customerSettings.OnlineCustomerMinutes),
                null, _workContext.CurrentCustomer.StaffStoreId, _workContext.CurrentCustomer.SeId, command.Page - 1, command.PageSize);
            var items = new List<OnlineCustomerModel>();
            foreach (var x in customers)
            {
                var item = new OnlineCustomerModel() {
                    Id = x.Id,
                    CustomerInfo = !string.IsNullOrEmpty(x.Email) ? x.Email : _translationService.GetResource("Admin.Customers.Guest"),
                    LastIpAddress = x.LastIpAddress,
                    Location = _geoLookupService.CountryName(x.LastIpAddress),
                    LastActivityDate = _dateTimeService.ConvertToUserTime(x.LastActivityDateUtc, DateTimeKind.Utc),
                    LastVisitedPage = _customerSettings.StoreLastVisitedPage ?
                        x.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastVisitedPage) :
                        _translationService.GetResource("Admin.Dashboards.OnlineCustomers.Fields.LastVisitedPage.Disabled")
                };
                items.Add(item);
            }

            var gridModel = new DataSourceResult {
                Data = items,
                Total = customers.TotalCount
            };

            return Json(gridModel);
        }

        #endregion
    }
}
