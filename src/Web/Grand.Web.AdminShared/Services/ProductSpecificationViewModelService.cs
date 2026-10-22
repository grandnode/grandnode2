using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Domain.Catalog;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductSpecificationViewModelService(
    ISpecificationAttributeService specificationAttributeService,
    IEnumTranslationService enumTranslationService)
    : IProductSpecificationViewModelService
{
    //Product specification
    public virtual async Task<IList<ProductSpecificationAttributeModel>> PrepareProductSpecificationAttributeModel(
        Product product)
    {
        var items = new List<ProductSpecificationAttributeModel>();
        foreach (var x in product.ProductSpecificationAttributes.OrderBy(x => x.DisplayOrder))
        {
            var psaModel = new ProductSpecificationAttributeModel {
                Id = x.Id,
                AttributeTypeId = (int)x.AttributeTypeId,
                AttributeId = x.SpecificationAttributeId,
                ProductId = product.Id,
                AttributeTypeName = enumTranslationService.GetTranslationEnum(x.AttributeTypeId),
                AllowFiltering = x.AllowFiltering,
                ShowOnProductPage = x.ShowOnProductPage,
                DisplayOrder = x.DisplayOrder,
                AttributeName = x.CustomName
            };

            switch (x.AttributeTypeId)
            {
                case SpecificationAttributeType.Option:
                    var specificationAttribute =
                        await specificationAttributeService.GetSpecificationAttributeById(x.SpecificationAttributeId);
                    if (specificationAttribute != null)
                    {
                        psaModel.AttributeName = specificationAttribute.Name;
                        psaModel.ValueRaw = WebUtility.HtmlEncode(specificationAttribute.SpecificationAttributeOptions
                            .FirstOrDefault(y => y.Id == x.SpecificationAttributeOptionId)?.Name);
                    }

                    psaModel.SpecificationAttributeOptionId = x.SpecificationAttributeOptionId;
                    break;
                case SpecificationAttributeType.CustomText:
                    psaModel.ValueRaw = WebUtility.HtmlEncode(x.CustomValue);
                    break;
                case SpecificationAttributeType.CustomHtmlText:
                    //do not encode?
                    psaModel.ValueRaw = WebUtility.HtmlEncode(x.CustomValue);
                    break;
                case SpecificationAttributeType.Hyperlink:
                    psaModel.ValueRaw = x.CustomValue;
                    break;
            }

            items.Add(psaModel);
        }

        return items;
    }

    public virtual async Task InsertProductSpecificationAttributeModel(
        ProductModel.AddProductSpecificationAttributeModel model, Product product)
    {
        //we allow filtering only for "Option" attribute type
        if (model.AttributeTypeId != (int)SpecificationAttributeType.Option)
        {
            model.AllowFiltering = false;
            model.SpecificationAttributeOptionId = null;
        }

        var psa = model.ToEntity();

        await specificationAttributeService.InsertProductSpecificationAttribute(psa, product.Id);
        product.ProductSpecificationAttributes.Add(psa);
    }

    public virtual async Task UpdateProductSpecificationAttributeModel(ProductSpecificationAttribute psa,
        ProductModel.AddProductSpecificationAttributeModel model)
    {
        psa = model.ToEntity(psa);
        await specificationAttributeService.UpdateProductSpecificationAttribute(psa, model.ProductId);
    }

    public virtual async Task DeleteProductSpecificationAttribute(Product product, ProductSpecificationAttribute psa)
    {
        product.ProductSpecificationAttributes.Remove(psa);
        await specificationAttributeService.DeleteProductSpecificationAttribute(psa, product.Id);
    }
}
