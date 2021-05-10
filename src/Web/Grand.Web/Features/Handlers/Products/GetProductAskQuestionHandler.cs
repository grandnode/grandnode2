using Grand.Business.Common.Extensions;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductAskQuestionHandler : IRequestHandler<GetProductAskQuestion, ProductAskQuestionModel>
    {
        private readonly CaptchaSettings _captchaSettings;

        public GetProductAskQuestionHandler(CaptchaSettings captchaSettings)
        {
            _captchaSettings = captchaSettings;
        }

        public async Task<ProductAskQuestionModel> Handle(GetProductAskQuestion request, CancellationToken cancellationToken)
        {
            var model = new ProductAskQuestionModel();
            model.Id = request.Product.Id;
            model.ProductName = request.Product.GetTranslation(x => x.Name, request.Language.Id);
            model.ProductSeName = request.Product.GetSeName(request.Language.Id);
            model.Email = request.Customer.Email;
            model.FullName = request.Customer.GetFullName();
            model.Phone = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);
            model.Message = "";
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnAskQuestionPage;

            return await Task.FromResult(model);
        }
    }
}
