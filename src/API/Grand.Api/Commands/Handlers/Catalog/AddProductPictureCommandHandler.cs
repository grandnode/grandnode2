using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductPictureCommandHandler : IRequestHandler<AddProductPictureCommand, bool>
    {
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;

        public AddProductPictureCommandHandler(
            IProductService productService,
            IPictureService pictureService)
        {
            _productService = productService;
            _pictureService = pictureService;
        }

        public async Task<bool> Handle(AddProductPictureCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id);
            if (product == null)
                return false;

            var picture = await _pictureService.GetPictureById(request.Model.PictureId);
            if (picture == null)
                return false;

            await _productService.InsertProductPicture(new ProductPicture {
                PictureId = picture.Id,
                DisplayOrder = request.Model.DisplayOrder,
            }, product.Id);

            return true;
        }
    }
}
