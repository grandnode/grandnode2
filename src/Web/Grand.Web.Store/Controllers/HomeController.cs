using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

public class HomeController : BaseStoreController
{
    #region Ctor

    public HomeController(
        IContextAccessor contextAccessor,
        ILogger<HomeController> logger,
        IGrandAuthenticationService authenticationService)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
        _authenticationService = authenticationService;
    }

    #endregion

    #region Fields

    private readonly IContextAccessor _contextAccessor;
    private readonly ILogger<HomeController> _logger;
    private readonly IGrandAuthenticationService _authenticationService;

    #endregion

    #region Methods

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        _logger.LogInformation("Access denied to user #{CurrentCustomerEmail}", _contextAccessor.WorkContext.CurrentCustomer.Email);
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await _authenticationService.SignOut();
        return RedirectToRoute("StoreLogin");
    }

    #endregion
}