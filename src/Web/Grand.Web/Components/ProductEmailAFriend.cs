using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class ProductEmailAFriendViewComponent : BaseViewComponent
{
    #region Constructors

    public ProductEmailAFriendViewComponent(
        IProductService productService,
        IWorkContextAccessor workContextAccessor,
        CatalogSettings catalogSettings,
        CaptchaSettings captchaSettings)
    {
        _productService = productService;
        _workContextAccessor = workContextAccessor;
        _catalogSettings = catalogSettings;
        _captchaSettings = captchaSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product is not { Published: true } || !_catalogSettings.EmailAFriendEnabled)
            return Content("");

        var model = new ProductEmailAFriendModel {
            ProductId = product.Id,
            ProductName = product.GetTranslation(x => x.Name, _workContextAccessor.WorkContext.WorkingLanguage.Id),
            ProductSeName = product.GetSeName(_workContextAccessor.WorkContext.WorkingLanguage.Id),
            YourEmailAddress = _workContextAccessor.WorkContext.CurrentCustomer.Email,
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailProductToFriendPage
        };

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly CatalogSettings _catalogSettings;
    private readonly CaptchaSettings _captchaSettings;

    #endregion
}