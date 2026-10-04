using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.ProductReviews)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductReviewController(
    IProductReviewViewModelService productReviewViewModelService,
    IProductReviewService productReviewService,
    ITranslationService translationService,
    IAdminDataScope<ProductReview> scope)
    : BaseController
{
    #region List

    public IActionResult Index() => RedirectToAction("List");

    // Must call PrepareProductReviewListModel, not return a bare model — Admin's original did,
    // Store's original didn't (returned `new ProductReviewListModel()`). This asymmetry is the
    // same shape as Page Phase 15's own Critical regression (a shared List(GET) silently losing
    // Admin's model-prep call); preserving it for both hosts here, not narrowing to Store's gap.
    public async Task<IActionResult> List()
    {
        var model = await productReviewViewModelService.PrepareProductReviewListModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, ProductReviewListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var (productReviewModels, totalCount) =
            await productReviewViewModelService.PrepareProductReviewsModel(model, command.Page, command.PageSize);

        return Json(new DataSourceResult {
            Data = productReviewModels.ToList(),
            Total = totalCount
        });
    }

    #endregion

    #region Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var productReview = await productReviewService.GetProductReviewById(id);
        if (productReview == null) return RedirectToAction("List");
        if (!await scope.HasAccess(productReview)) return RedirectToAction("List");

        var model = new ProductReviewModel();
        await productReviewViewModelService.PrepareProductReviewModel(model, productReview, false, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(ProductReviewModel model, bool continueEditing)
    {
        var productReview = await productReviewService.GetProductReviewById(model.Id);
        if (productReview == null) return RedirectToAction("List");
        if (!await scope.HasAccess(productReview)) return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            productReview = await productReviewViewModelService.UpdateProductReview(productReview, model);
            Success(translationService.GetResource("Admin.Catalog.ProductReviews.Updated"));
            return continueEditing
                ? RedirectToAction("Edit", new { productReview.Id, productReview.ProductId })
                : RedirectToAction("List");
        }

        await productReviewViewModelService.PrepareProductReviewModel(model, productReview, true, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var productReview = await productReviewService.GetProductReviewById(id);
        if (productReview == null) return RedirectToAction("List");
        if (!await scope.HasAccess(productReview)) return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await productReviewViewModelService.DeleteProductReview(productReview);
            Success(translationService.GetResource("Admin.Catalog.ProductReviews.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = productReview.Id });
    }

    #endregion

    #region Approve / Disapprove

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ApproveSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null)
            await productReviewViewModelService.ApproveSelected(selectedIds.ToList(), scope.DefaultStoreId ?? "");

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DisapproveSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null)
            await productReviewViewModelService.DisapproveSelected(selectedIds.ToList(), scope.DefaultStoreId ?? "");

        return Json(new { Result = true });
    }

    #endregion

    #region Product search

    public async Task<IActionResult> ProductSearchAutoComplete(string term,
        [FromServices] IProductService productService)
    {
        const int searchTermMinimumLength = 3;
        if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
            return Content("");

        const int productNumber = 15;
        var products = (await productService.SearchProducts(
            storeId: scope.DefaultStoreId ?? "",
            keywords: term,
            pageSize: productNumber,
            showHidden: true)).products;

        var result = (from p in products
                      select new
                      {
                          label = p.Name,
                          productid = p.Id
                      })
            .ToList();
        return Json(result);
    }

    #endregion
}
