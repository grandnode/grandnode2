using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components
{
    public class ProductEmailAFriendViewComponent : BaseViewComponent
    {
        #region Fields
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly CatalogSettings _catalogSettings;
        private readonly CaptchaSettings _captchaSettings;

        #endregion

        #region Constructors

        public ProductEmailAFriendViewComponent(
            IProductService productService,
            IWorkContext workContext,
            CatalogSettings catalogSettings,
            CaptchaSettings captchaSettings)
        {
            _productService = productService;
            _workContext = workContext;
            _catalogSettings = catalogSettings;
            _captchaSettings = captchaSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(string productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null || !product.Published || !_catalogSettings.EmailAFriendEnabled)
                return Content("");

            var model = new ProductEmailAFriendModel();
            model.ProductId = product.Id;
            model.ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id);
            model.ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id);
            model.YourEmailAddress = _workContext.CurrentCustomer.Email;
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailProductToFriendPage;

            return View(model);

        }

        #endregion

    }
}
