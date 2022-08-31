﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Customers;
using Microsoft.AspNetCore.Mvc;
using Grand.Infrastructure;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.SalesEmployees)]
    public partial class SalesEmployeeController : BaseAdminController
    {
        #region Fields

        private readonly ISalesEmployeeService _salesEmployeeService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IDocumentService _documentService;
        private readonly IWorkContext _workContext;
        #endregion

        #region Constructors

        public SalesEmployeeController(
            ISalesEmployeeService salesEmployeeService,
            ICustomerService customerService,
            IOrderService orderService,
            IDocumentService documentService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext
            )
        {
            _salesEmployeeService = salesEmployeeService;
            _customerService = customerService;
            _orderService = orderService;
            _documentService = documentService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        #endregion

        [PermissionAuthorizeAction(PermissionActionName.List)]
        public IActionResult Index() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var weightsModel = (await _salesEmployeeService.GetAll())
                .Select(x => x.ToModel())
                .ToList();

            var gridModel = new DataSourceResult
            {
                Data = weightsModel,
                Total = weightsModel.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Update(SalesEmployeeModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var salesemployee = await _salesEmployeeService.GetSalesEmployeeById(model.Id);
            salesemployee = model.ToEntity(salesemployee);
            await _salesEmployeeService.UpdateSalesEmployee(salesemployee);

            //activity log
            _ = _customerActivityService.InsertActivity("EditSalesEmployee", salesemployee.Id,
                _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.EditSalesEmployee"),
                salesemployee.Name);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Add(SalesEmployeeModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var salesEmployee = new SalesEmployee();
            salesEmployee = model.ToEntity(salesEmployee);
            await _salesEmployeeService.InsertSalesEmployee(salesEmployee);

            //activity log
            _ = _customerActivityService.InsertActivity("AddNewSalesEmployee", salesEmployee.Id,
                _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.AddNewSalesEmployee"),
                salesEmployee.Name);

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

            //activity log
            _ = _customerActivityService.InsertActivity("DeleteSalesEmployee", salesemployee.Id,
                _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.DeleteSalesEmployee"),
                salesemployee.Name);

            return new JsonResult("");
        }

    }
}
