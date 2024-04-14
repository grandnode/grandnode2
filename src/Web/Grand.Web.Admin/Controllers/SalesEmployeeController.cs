using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.SalesEmployees)]
public class SalesEmployeeController : BaseAdminController
{
    #region Constructors

    public SalesEmployeeController(
        ISalesEmployeeService salesEmployeeService,
        ICustomerService customerService,
        IOrderService orderService,
        IDocumentService documentService
    )
    {
        _salesEmployeeService = salesEmployeeService;
        _customerService = customerService;
        _orderService = orderService;
        _documentService = documentService;
    }

    #endregion

    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var weightsModel = (await _salesEmployeeService.GetAll())
            .Select(x => x.ToModel())
            .ToList();

        var gridModel = new DataSourceResult {
            Data = weightsModel,
            Total = weightsModel.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Update(SalesEmployeeModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var salesemployee = await _salesEmployeeService.GetSalesEmployeeById(model.Id);
        salesemployee = model.ToEntity(salesemployee);
        await _salesEmployeeService.UpdateSalesEmployee(salesemployee);
        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Add(SalesEmployeeModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var salesEmployee = new SalesEmployee();
        salesEmployee = model.ToEntity(salesEmployee);
        await _salesEmployeeService.InsertSalesEmployee(salesEmployee);

        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var salesemployee = await _salesEmployeeService.GetSalesEmployeeById(id);
        if (salesemployee == null)
            throw new ArgumentException("No sales employee found with the specified id");

        var customers = await _customerService.GetAllCustomers(salesEmployeeId: id);
        if (customers.Any())
            return Json(new DataSourceResult { Errors = "Sales employee is related with customers" });

        var orders = await _orderService.SearchOrders(salesEmployeeId: id);
        if (orders.Any())
            return Json(new DataSourceResult { Errors = "Sales employee is related with orders" });

        var documents = await _documentService.GetAll(seId: id);
        if (documents.Any())
            return Json(new DataSourceResult { Errors = "Sales employee is related with documents" });

        await _salesEmployeeService.DeleteSalesEmployee(salesemployee);

        return new JsonResult("");
    }

    #region Fields

    private readonly ISalesEmployeeService _salesEmployeeService;
    private readonly ICustomerService _customerService;
    private readonly IOrderService _orderService;
    private readonly IDocumentService _documentService;

    #endregion
}