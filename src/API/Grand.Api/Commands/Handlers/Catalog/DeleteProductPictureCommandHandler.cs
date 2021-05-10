using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductPictureCommandHandler : IRequestHandler<DeleteProductPictureCommand, bool>
    {
        private readonly IProductService _productService;

        public DeleteProductPictureCommandHandler(
            IProductService productService)
        {
            _productService = productService;
        }

        public async Task<bool> Handle(DeleteProductPictureCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id);

            var productPicture = product.ProductPictures.Where(x => x.PictureId == request.PictureId).FirstOrDefault();
            if (productPicture == null)
                throw new ArgumentException("No product picture found with the specified pictureid");

            await _productService.DeleteProductPicture(productPicture, product.Id);

            return true;
        }
    }
}
