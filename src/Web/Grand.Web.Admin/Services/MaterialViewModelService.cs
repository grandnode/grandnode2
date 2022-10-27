using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using static Grand.Web.Admin.Models.Catalog.ProductModel;

namespace Grand.Web.Admin.Services
{
    public class MaterialViewModelService: IMaterialViewModelService
    {
        public IList<MaterialModel> PrepareMaterialViewModel(IEnumerable<Material> materials)
        {
            var materialsList = new List<MaterialModel>();
            // Test data
            materials = new List<Material>() {
                new Material() {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Material 1",
                    FilePath = "Material 1 file path",
                    Cost = 100,
                    Price = 120
                },
                new Material() {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Material 2",
                    FilePath = "Material 2 file path",
                    Cost = 110,
                    Price = 130
                },
                new Material() {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Material 3",
                    FilePath = "Material 3 file path",
                    Cost = 120,
                    Price = 140
                }
            };
            foreach (var material in materials)
            {
                materialsList.Add(material.ToModel());
            }

            return materialsList;
        }
    }
}
