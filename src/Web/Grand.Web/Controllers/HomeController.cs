using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Controllers
{
    public class HomeController : BasePublicController
    {
        
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        [IgnoreApi]
        [HttpGet]
        public virtual IActionResult Index()
        {
            _logger.Log(LogLevel.Error, new Exception("dddd"), "ILogger Log from HomeController()");
            _logger.LogTrace("ILogger Trace from HomeController()");
            _logger.LogDebug("ILogger Debug from HomeController()");
            _logger.LogInformation("ILogger Info from HomeController()");
            _logger.LogWarning("ILogger Warn from HomeController()");
            _logger.LogError("ILogger Error from HomeController()");
            _logger.LogCritical("ILogger Fatal from HomeController()");
            return View();
        }
    }
}
