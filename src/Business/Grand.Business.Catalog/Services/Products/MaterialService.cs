using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Caching;
using MediatR;
using Grand.Infrastructure.Extensions;

namespace Grand.Business.Catalog.Services.Products
{
    public class MaterialService : IMaterialService
    {
        private IRepository<Product> _productRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        public MaterialService(IRepository<Product> productRepository, IMediator mediator, ICacheBase cacheBase)
        {
            _productRepository = productRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        public async Task<Material> UpdateMaterial(Material material, string productId, string productAttributeMappingId, string productAttributeValueId)
        {
            // Find the product Mapping
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            var p = await _productRepository.GetByIdAsync(productId);
            if (p != null)
            {
                var pavs = p.ProductAttributeMappings.Where(x => x.Id == productAttributeMappingId).FirstOrDefault();
                if (pavs != null)
                {
                    var pav = pavs.ProductAttributeValues.Where(x => x.Id == productAttributeValueId).FirstOrDefault();
                    if (pav != null)
                    {
                        var currentMaterial = pav.Materials.Where(x => x.Id == material.Id).FirstOrDefault();

                        if(currentMaterial != null)
                        {
                            currentMaterial.Price = material.Price;
                            currentMaterial.Locales = material.Locales;
                            currentMaterial.FilePath = material.FilePath;
                            currentMaterial.Cost = material.Cost;
                            currentMaterial.Name = material.Name;

                            await _productRepository.UpdateToSet(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMappingId, pavs);
                        }

                    }
                }
            }

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(material);
            return material;
        }

        public async Task<Material> InsertMaterial(Material material, string productId, string productAttributeMappingId, string productAttributeValueId)
        {
            // Find the product Mapping
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            var p = await _productRepository.GetByIdAsync(productId);
            if (p != null)
            {
                var pavs = p.ProductAttributeMappings.Where(x => x.Id == productAttributeMappingId).FirstOrDefault();
                if (pavs != null)
                {
                    var pav = pavs.ProductAttributeValues.Where(x => x.Id == productAttributeValueId).FirstOrDefault();
                    if (pav != null)
                    {
                        pav.Materials.Add(material);

                        await _productRepository.UpdateToSet(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMappingId, pavs);
                    }
                }
            }

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(material);

            return material;
        }
    }
}
