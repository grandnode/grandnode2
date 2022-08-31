using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components
{
    public class VendorContactViewComponent : BaseViewComponent
    {
        #region Fields
        private readonly IVendorService _vendorService;
        private readonly IWorkContext _workContext;
        private readonly VendorSettings _vendorSettings;
        private readonly CommonSettings _commonSettings;
        private readonly CaptchaSettings _captchaSettings;
        #endregion

        #region Constructors

        public VendorContactViewComponent(
            IVendorService vendorService,
            IWorkContext workContext,
            VendorSettings vendorSettings,
            CommonSettings commonSettings,
            CaptchaSettings captchaSettings)
        {
            _vendorService = vendorService;
            _workContext = workContext;
            _vendorSettings = vendorSettings;
            _commonSettings = commonSettings;
            _captchaSettings = captchaSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(string vendorId)
        {
            if (!_vendorSettings.AllowCustomersToContactVendors)
                return Content("");

            var vendor = await _vendorService.GetVendorById(vendorId);
            if (vendor == null || !vendor.Active || vendor.Deleted)
                return Content("");

            var model = new ContactVendorModel {
                Email = _workContext.CurrentCustomer.Email,
                FullName = _workContext.CurrentCustomer.GetFullName(),
                SubjectEnabled = _commonSettings.SubjectFieldOnContactUsForm,
                DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage,
                VendorId = vendor.Id,
                VendorName = vendor.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
            };

            return View(model);

        }

        #endregion

    }
}
