using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
            var picture = await _pictureService.GetPictureById(request.Model.PictureId);
            if (picture != null)
            {
                await _pictureService.UpdatePicture(picture.Id, await _pictureService.LoadPictureBinary(picture),
                picture.MimeType,
                picture.SeoFilename,
                request.Model.AltAttribute,
                request.Model.TitleAttribute);

                await _productService.InsertProductPicture(new ProductPicture
                {
                    PictureId = request.Model.PictureId,
                    DisplayOrder = request.Model.DisplayOrder,
                }, request.Product.Id);
                await _pictureService.SetSeoFilename(request.Model.PictureId, _pictureService.GetPictureSeName(request.Product.Name));
            }

            return true;
        }
    }
}
