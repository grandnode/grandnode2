using AspNetCore;
using Grand.Domain.Catalog;
using static Grand.Web.Admin.Models.Catalog.ProductModel;

namespace Grand.Web.Admin.Extensions.Mapping
{
    public static class MaterialMappingExtensions
    {
        public static Material ToEntity(this MaterialModel materialModel)
        {
            return new Material() {
                Id = materialModel.Id,
                Name = materialModel.Name,
                FilePath = materialModel.FilePath,
                Cost = materialModel.Cost,
                Price = materialModel.Price
            };
        }

        public static MaterialModel ToModel(this Material material)
        {
            return new MaterialModel() {
                Name = material.Name,
                FilePath = material.FilePath,
                Cost = material.Cost,
                Price = material.Price,
                Id = material.Id
            };            
        }
    }
}
