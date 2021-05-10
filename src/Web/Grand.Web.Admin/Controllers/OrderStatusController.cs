using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Orders;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.OrderStatus)]
    public partial class OrderStatusController : BaseAdminController
    {
        #region Fields

        private readonly IOrderStatusService _orderStatusService;
        private readonly IOrderService _orderService;
        #endregion

        #region Constructors

        public OrderStatusController(IOrderStatusService orderStatusService,
            IOrderService orderService)
        {
            _orderStatusService = orderStatusService;
            _orderService = orderService;
        }

        #endregion

        #region Delivery dates

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Statuses()
        {
            var orderStatuses = await _orderStatusService.GetAll();
            var gridModel = new DataSourceResult
            {
                Data = orderStatuses,
                Total = orderStatuses.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> Update(OrderStatusModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var status = await _orderStatusService.GetById(model.Id);
            status = model.ToEntity(status);
            await _orderStatusService.Update(status);

            return new JsonResult("");
        }

        [HttpPost]
        public async Task<IActionResult> Add(OrderStatusModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var status = new OrderStatus();
            status = model.ToEntity(status);
            status.StatusId = (await _orderStatusService.GetAll()).Max(x => x.StatusId) + 10;
            await _orderStatusService.Insert(status);

            return new JsonResult("");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var status = await _orderStatusService.GetById(id);
            if (status == null)
                throw new ArgumentException("No status found with the specified id");

            if (status.IsSystem)
            {
                return Json(new DataSourceResult { Errors = "You can't delete system status" });
            }

            var orders = await _orderService.SearchOrders(os: status.StatusId, pageSize: 1);
            if (orders.Any())
                return Json(new DataSourceResult { Errors = "There are exists orders with this status" });

            await _orderStatusService.Delete(status);

            return new JsonResult("");
        }


        #endregion


    }
}
