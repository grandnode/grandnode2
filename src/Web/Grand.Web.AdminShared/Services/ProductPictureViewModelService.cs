using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductPictureViewModelService(
    IProductService productService,
    IPictureService pictureService)
    : IProductPictureViewModelService
{
    //Pictures
    public virtual async Task<IList<ProductModel.ProductPictureModel>> PrepareProductPicturesModel(Product product)
    {
        var items = new List<ProductModel.ProductPictureModel>();
        foreach (var x in product.ProductPictures.OrderBy(x => x.DisplayOrder))
        {
            var picture = await pictureService.GetPictureById(x.PictureId);
            var m = new ProductModel.ProductPictureModel {
                Id = x.Id,
                ProductId = product.Id,
                PictureId = x.PictureId,
                PictureUrl = picture != null ? await pictureService.GetPictureUrl(picture) : null,
                AltAttribute = picture?.AltAttribute,
                TitleAttribute = picture?.TitleAttribute,
                DisplayOrder = x.DisplayOrder,
                IsDefault = x.IsDefault,
                Style = picture?.Style,
                ExtraField = picture?.ExtraField
            };
            items.Add(m);
        }

        return items;
    }

    public virtual async Task<(ProductModel.ProductPictureModel model, Picture Picture)> PrepareProductPictureModel(
        Product product, ProductPicture productPicture)
    {
        var picture = await pictureService.GetPictureById(productPicture.PictureId);
        var model = new ProductModel.ProductPictureModel {
            Id = productPicture.Id,
            ProductId = product.Id,
            PictureId = productPicture.PictureId,
            PictureUrl = picture != null ? await pictureService.GetPictureUrl(picture) : null,
            AltAttribute = picture?.AltAttribute,
            TitleAttribute = picture?.TitleAttribute,
            DisplayOrder = productPicture.DisplayOrder,
            IsDefault = productPicture.IsDefault,
            Style = picture?.Style,
            ExtraField = picture?.ExtraField
        };

        return (model, picture);
    }

    public virtual async Task InsertProductPicture(Product product, Picture picture, int displayOrder)
    {
        if (picture == null)
            throw new ArgumentException("No picture found with the specified id");

        if (product.ProductPictures.Any(x => x.PictureId == picture.Id))
            return;

        var productPicture = new ProductPicture {
            PictureId = picture.Id,
            DisplayOrder = displayOrder
        };
        await productService.InsertProductPicture(productPicture, product.Id);
        await pictureService.SetSeoFilename(picture, pictureService.GetPictureSeName(product.Name));
    }

    public virtual async Task UpdateProductPicture(ProductModel.ProductPictureModel model)
    {
        var product = await productService.GetProductById(model.ProductId, true);

        var productPicture = product.ProductPictures.FirstOrDefault(x => x.Id == model.Id);
        if (productPicture == null)
            throw new ArgumentException("No product picture found with the specified id");

        var picture = await pictureService.GetPictureById(productPicture.PictureId);
        if (picture == null)
            throw new ArgumentException("No picture found with the specified id");

        productPicture.DisplayOrder = model.DisplayOrder;
        productPicture.IsDefault = model.IsDefault;
        await productService.UpdateProductPicture(productPicture, product.Id);

        //Update picture fields
        await pictureService.UpdatePictureField(picture, x => x.AltAttribute, model.AltAttribute);
        await pictureService.UpdatePictureField(picture, x => x.TitleAttribute, model.TitleAttribute);
        await pictureService.UpdatePictureField(picture, x => x.Locales, model.Locales.ToTranslationProperty());
        await pictureService.UpdatePictureField(picture, x => x.Style, model.Style);
        await pictureService.UpdatePictureField(picture, x => x.ExtraField, model.ExtraField);
    }

    public virtual async Task DeleteProductPicture(ProductModel.ProductPictureModel model)
    {
        var product = await productService.GetProductById(model.ProductId, true);

        var productPicture = product.ProductPictures.FirstOrDefault(x => x.Id == model.Id);
        if (productPicture == null)
            throw new ArgumentException("No product picture found with the specified id");

        var pictureId = productPicture.PictureId;
        await productService.DeleteProductPicture(productPicture, product.Id);

        var picture = await pictureService.GetPictureById(pictureId);
        if (picture != null)
            await pictureService.DeletePicture(picture);
    }
}
