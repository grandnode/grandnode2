using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Order.ExternalOrderApi.Models;
using Order.ExternalOrderApi.Services;
using System.Net;
using System.Text.Json;

namespace Order.ExternalOrderApi.Controllers;

[ApiController]
[Route("api/order")]
public class ExternalOrderApiController : BaseController
{

    private readonly ILogger<ExternalOrderApiController> _logger;
    private readonly IContextAccessor _contextAccessor;
    private readonly IExternalOrderService _externalOrderService;
    private readonly ICustomerService _customerService;

    public ExternalOrderApiController(
        ILogger<ExternalOrderApiController> logger,
        IContextAccessor contextAccessor,
        IExternalOrderService externalOrderService,
        IWorkContextSetter workContextSetter,
        IGrandAuthenticationService authenticationService,
        ICustomerService customerService)
    {
        _logger = logger;
        _contextAccessor = contextAccessor;
        _externalOrderService = externalOrderService;
        _customerService = customerService;
    }



    [HttpPost]
    [ProducesResponseType(typeof(ExternalOrderResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ProcessOrder([FromBody] ExternalOrderPayload externalOrderPayload)
    {
        try
        {
            ExternalOrderModel externalOrderModel = externalOrderPayload.Content.FirstOrDefault();
            var result = await _externalOrderService.ProcessOrder(externalOrderModel);

            if (!result.IsSuccess)
            {
                _logger.LogError("External order API processing failed");

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("Order", error);
                }
                return BadRequest(ModelState);
            }

            _logger.LogInformation("External order API: Order successfully created with ID {OrderId}",
                result.Order?.Id ?? "unknown");

            var response = new ExternalOrderResponse
            {
                Success = true,
                OrderId = result.Order?.Id,
                OrderNumber = result.Order?.OrderNumber
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External order API error: {Message}", ex.Message);
            ModelState.AddModelError("Order", "An error occurred while processing the order");
            return BadRequest(ModelState);
        }
    }
}
