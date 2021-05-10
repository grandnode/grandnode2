using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Queries.Models;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Affiliates;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Affiliates)]
    public partial class AffiliateController : BaseAdminController
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly IAffiliateService _affiliateService;
        private readonly IAffiliateViewModelService _affiliateViewModelService;
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Constructors

        public AffiliateController(ITranslationService translationService,
            IAffiliateService affiliateService, IAffiliateViewModelService affiliateViewModelService,
            IMediator mediator,
            IPermissionService permissionService)
        {
            _translationService = translationService;
            _affiliateService = affiliateService;
            _affiliateViewModelService = affiliateViewModelService;
            _mediator = mediator;
            _permissionService = permissionService;
        }

        #endregion

        #region Methods

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = new AffiliateListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, AffiliateListModel model)
        {
            var affiliatesModel = await _affiliateViewModelService.PrepareAffiliateModelList(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = affiliatesModel.affiliateModels,
                Total = affiliatesModel.totalCount,
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new AffiliateModel();
            await _affiliateViewModelService.PrepareAffiliateModel(model, null, false);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(AffiliateModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var affiliate = await _affiliateViewModelService.InsertAffiliateModel(model);
                Success(_translationService.GetResource("Admin.Affiliates.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = affiliate.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            await _affiliateViewModelService.PrepareAffiliateModel(model, null, true);
            return View(model);

        }


        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var affiliate = await _affiliateService.GetAffiliateById(id);
            if (affiliate == null)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            var model = new AffiliateModel();
            await _affiliateViewModelService.PrepareAffiliateModel(model, affiliate, false);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(AffiliateModel model, bool continueEditing)
        {
            var affiliate = await _affiliateService.GetAffiliateById(model.Id);
            if (affiliate == null)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                affiliate = await _affiliateViewModelService.UpdateAffiliateModel(model, affiliate);

                Success(_translationService.GetResource("Admin.Affiliates.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = affiliate.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            await _affiliateViewModelService.PrepareAffiliateModel(model, affiliate, true);
            return View(model);
        }

        //delete
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var affiliate = await _affiliateService.GetAffiliateById(id);
            if (affiliate == null)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            var customers = new GetCustomerQuery()
            {
                AffiliateId = affiliate.Id,
                PageSize = 1,
            };
            var query_customer = (await _mediator.Send(customers)).Count();
            if (query_customer > 0)
                ModelState.AddModelError("", "There are exist customers related with affiliate");

            var orders = new GetOrderQuery()
            {
                AffiliateId = affiliate.Id,
                PageSize = 1,
            };

            var query_order = (await _mediator.Send(orders)).Count();
            if (query_order > 0)
                ModelState.AddModelError("", "There are exist orders related with affiliate");


            if (ModelState.IsValid)
            {
                await _affiliateService.DeleteAffiliate(affiliate);
                Success(_translationService.GetResource("Admin.Affiliates.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> AffiliatedOrderList(DataSourceRequest command, AffiliatedOrderListModel model)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Json(new DataSourceResult
                {
                    Data = null,
                    Total = 0
                });

            var affiliate = await _affiliateService.GetAffiliateById(model.AffliateId);
            if (affiliate == null)
                throw new ArgumentException("No affiliate found with the specified id");

            var affiliateOrders = await _affiliateViewModelService.PrepareAffiliatedOrderList(affiliate, model, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = affiliateOrders.affiliateOrderModels,
                Total = affiliateOrders.totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> AffiliatedCustomerList(string affiliateId, DataSourceRequest command)
        {
            var affiliate = await _affiliateService.GetAffiliateById(affiliateId);
            if (affiliate == null)
                throw new ArgumentException("No affiliate found with the specified id");

            var affiliateCustomers = await _affiliateViewModelService.PrepareAffiliatedCustomerList(affiliate, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = affiliateCustomers.affiliateCustomerModels,
                Total = affiliateCustomers.totalCount
            };

            return Json(gridModel);
        }
        #endregion
    }
}
