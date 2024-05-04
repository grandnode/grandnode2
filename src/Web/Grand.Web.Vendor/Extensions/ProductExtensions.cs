using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;

namespace Grand.Web.Vendor.Extensions;

public static class ProductExtensions
{
    /// <summary>
    ///     Get product picture (for shopping cart and order details pages)
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="attributes">Attributes</param>
    /// <param name="productService">Product service</param>
    /// <param name="pictureService">Picture service</param>
    /// <returns>Picture</returns>
    public static async Task<Picture> GetProductPicture(this Product product, IList<CustomAttribute> attributes,
        IProductService productService, IPictureService pictureService)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(pictureService);

        Picture picture = null;

        if (attributes != null && attributes.Any())
        {
            var comb = product.FindProductAttributeCombination(attributes);
            if (comb != null)
                if (!string.IsNullOrEmpty(comb.PictureId))
                {
                    var combPicture = await pictureService.GetPictureById(comb.PictureId);
                    if (combPicture != null) picture = combPicture;
                }

            if (picture == null)
            {
                var attributeValues = product.ParseProductAttributeValues(attributes);
                foreach (var attributeValue in attributeValues)
                {
                    var attributePicture = await pictureService.GetPictureById(attributeValue.PictureId);
                    if (attributePicture != null)
                    {
                        picture = attributePicture;
                        break;
                    }
                }
            }
        }

        if (picture == null)
        {
            var pp = product.ProductPictures.MinBy(x => x.DisplayOrder);
            if (pp != null)
                picture = await pictureService.GetPictureById(pp.PictureId);
        }

        if (picture == null && !product.VisibleIndividually && !string.IsNullOrEmpty(product.ParentGroupedProductId))
        {
            var parentProduct = await productService.GetProductById(product.ParentGroupedProductId);
            if (parentProduct != null)
                if (parentProduct.ProductPictures.Any())
                    picture = await pictureService.GetPictureById(parentProduct.ProductPictures.FirstOrDefault()!
                        .PictureId);
        }

        return picture;
    }
}