﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.OrderTags)]
    public class OrderTagsController : BaseAdminController
    {
        private readonly IOrderTagService _orderTagService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;

        public OrderTagsController(IOrderTagService orderTagService, IOrderService orderService, IPermissionService permissionService)
        {
            _orderTagService = orderTagService;
            _orderService = orderService;
            _permissionService = permissionService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var tags = (await _orderTagService.GetAllOrderTags());
            var orderTagsList = new List<OrderTagModel>();
            foreach (var tag in tags)
            {
                var item = new OrderTagModel();
                item.Id = tag.Id;
                item.Name = tag.Name;
                item.OrderCount = await _orderTagService.GetOrderCount(tag.Id, "");
                orderTagsList.Add(item);
            }

            var gridModel = new DataSourceResult
            {
                Data = orderTagsList.OrderByDescending(x => x.OrderCount).PagedForCommand(command),
                Total = tags.Count()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Orders(string tagId, DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Json(new DataSourceResult
                {
                    Data = null,
                    Total = 0
                });

            var tag = await _orderTagService.GetOrderTagById(tagId);
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            var orders = (await _orderService.SearchOrders(pageIndex: command.Page - 1, pageSize: command.PageSize, orderTagId: tag.Id)).ToList();
            var gridModel = new DataSourceResult
            {
                Data = orders.Select(x => new
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber
                }),
                Total = orders.Count
            };

            return Json(gridModel);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var orderTag = await _orderTagService.GetOrderTagById(id);
            if (orderTag == null)
                return RedirectToAction("List");

            var model = new OrderTagModel
            {
                Id = orderTag.Id,
                Name = orderTag.Name,
                OrderCount = await _orderTagService.GetOrderCount(orderTag.Id, "")
            };

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(OrderTagModel model)
        {
            var orderTag = await _orderTagService.GetOrderTagById(model.Id);
            if (orderTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                orderTag.Name = model.Name;
                await _orderTagService.UpdateOrderTag(orderTag);
                ViewBag.RefreshPage = true;
                return View(model);
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var tagOrder = await _orderTagService.GetOrderTagById(id);
            if (tagOrder == null)
                throw new ArgumentException("No order's tag found with the specified id");
            if (ModelState.IsValid)
            {
                await _orderTagService.DeleteOrderTag(tagOrder);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
    }
}
