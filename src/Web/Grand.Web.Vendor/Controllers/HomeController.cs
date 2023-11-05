using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers
{
    public class HomeController : BaseVendorController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly ILogger _logger;
        private readonly IGrandAuthenticationService _authenticationService;

        #endregion

        #region Ctor

        public HomeController(
            IWorkContext workContext,
            ILogger logger,
            IGrandAuthenticationService authenticationService)
        {
            _workContext = workContext;
            _logger = logger;
            _authenticationService = authenticationService;
        }

        #endregion

        #region Methods

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AccessDenied(string pageUrl)
        {
            _ = _logger.Information(
                $"Access denied to user #{_workContext.CurrentCustomer.Email} on {pageUrl}");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _authenticationService.SignOut();
            return RedirectToRoute("VendorLogin");
        }

        #endregion
    }
}
