using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Products
{
    /// <summary>
    /// Specification attribute service
    /// </summary>
    public partial class SpecificationAttributeService : ISpecificationAttributeService
    {
        #region Fields

        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public SpecificationAttributeService(ICacheBase cacheBase,
            IRepository<SpecificationAttribute> specificationAttributeRepository,
            IRepository<Product> productRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _specificationAttributeRepository = specificationAttributeRepository;
            _mediator = mediator;
            _productRepository = productRepository;
        }

        #endregion

        #region Methods

        #region Specification attribute

        /// <summary>
        /// Gets a specification attribute
        /// </summary>
        /// <param name="specificationAttributeId">The specification attribute identifier</param>
        /// <returns>Specification attribute</returns>
        public virtual async Task<SpecificationAttribute> GetSpecificationAttributeById(string specificationAttributeId)
        {
            string key = string.Format(CacheKey.SPECIFICATION_BY_ID_KEY, specificationAttributeId);
            return await _cacheBase.GetAsync(key, () => _specificationAttributeRepository.GetByIdAsync(specificationAttributeId));
        }

        /// <summary>
        /// Gets a specification attribute by sename
        /// </summary>
        /// <param name="sename">Sename</param>
        /// <returns>Specification attribute</returns>
        public virtual async Task<SpecificationAttribute> GetSpecificationAttributeBySeName(string sename)
        {
            if (string.IsNullOrEmpty(sename))
                return await Task.FromResult<SpecificationAttribute>(null);

            sename = sename.ToLowerInvariant();

            var key = string.Format(CacheKey.SPECIFICATION_BY_SENAME, sename);
            return await _cacheBase.GetAsync(key, async () => 
                    await Task.FromResult(_specificationAttributeRepository.Table.Where(x => x.SeName == sename)
                .FirstOrDefault()));
        }


        /// <summary>
        /// Gets specification attributes
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Specification attributes</returns>
        public virtual async Task<IPagedList<SpecificationAttribute>> GetSpecificationAttributes(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from sa in _specificationAttributeRepository.Table
                        orderby sa.DisplayOrder
                        select sa;
            return await PagedList<SpecificationAttribute>.Create(query, pageIndex, pageSize);
        }

        
        /// <summary>
        /// Inserts a specification attribute
        /// </summary>
        /// <param name="specificationAttribute">The specification attribute</param>
        public virtual async Task InsertSpecificationAttribute(SpecificationAttribute specificationAttribute)
        {
            if (specificationAttribute == null)
                throw new ArgumentNullException(nameof(specificationAttribute));

            await _specificationAttributeRepository.InsertAsync(specificationAttribute);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(specificationAttribute);
        }

        /// <summary>
        /// Updates the specification attribute
        /// </summary>
        /// <param name="specificationAttribute">The specification attribute</param>
        public virtual async Task UpdateSpecificationAttribute(SpecificationAttribute specificationAttribute)
        {
            if (specificationAttribute == null)
                throw new ArgumentNullException(nameof(specificationAttribute));

            await _specificationAttributeRepository.UpdateAsync(specificationAttribute);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(specificationAttribute);
        }
        /// <summary>
        /// Deletes a specification attribute
        /// </summary>
        /// <param name="specificationAttribute">The specification attribute</param>
        public virtual async Task DeleteSpecificationAttribute(SpecificationAttribute specificationAttribute)
        {
            if (specificationAttribute == null)
                throw new ArgumentNullException(nameof(specificationAttribute));

            //delete from all product collections
            await _productRepository.PullFilter(string.Empty, x => x.ProductSpecificationAttributes, z => z.SpecificationAttributeId, specificationAttribute.Id, true);

            await _specificationAttributeRepository.DeleteAsync(specificationAttribute);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(specificationAttribute);
        }

        #endregion

        #region Specification attribute option

        /// <summary>
        /// Gets a specification attribute option
        /// </summary>
        /// <param name="specificationAttributeOptionId">The specification attribute option identifier</param>
        /// <returns>Specification attribute option</returns>
        public virtual async Task<SpecificationAttribute> GetSpecificationAttributeByOptionId(string specificationAttributeOptionId)
        {
            if (string.IsNullOrEmpty(specificationAttributeOptionId))
                return await Task.FromResult<SpecificationAttribute>(null);

            string key = string.Format(CacheKey.SPECIFICATION_BY_OPTIONID_KEY, specificationAttributeOptionId);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _specificationAttributeRepository.Table
                            where p.SpecificationAttributeOptions.Any(x => x.Id == specificationAttributeOptionId)
                            select p;
                return await Task.FromResult(query.FirstOrDefault());
            });
        }

        /// <summary>
        /// Deletes a specification attribute option
        /// </summary>
        /// <param name="specificationAttributeOption">The specification attribute option</param>
        public virtual async Task DeleteSpecificationAttributeOption(SpecificationAttributeOption specificationAttributeOption)
        {
            if (specificationAttributeOption == null)
                throw new ArgumentNullException(nameof(specificationAttributeOption));

            //delete from all product collections
            await _productRepository.PullFilter(string.Empty, x => x.ProductSpecificationAttributes, z => z.SpecificationAttributeOptionId, specificationAttributeOption.Id, true);

            var specificationAttribute = await GetSpecificationAttributeByOptionId(specificationAttributeOption.Id);
            var sao = specificationAttribute.SpecificationAttributeOptions.Where(x => x.Id == specificationAttributeOption.Id).FirstOrDefault();
            if (sao == null)
                throw new ArgumentException("No specification attribute option found with the specified id");

            specificationAttribute.SpecificationAttributeOptions.Remove(sao);
            await UpdateSpecificationAttribute(specificationAttribute);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(specificationAttributeOption);
        }


        #endregion

        #region Product specification attribute

        
        /// <summary>
        /// Inserts a product specification attribute mapping
        /// </summary>
        /// <param name="productSpecificationAttribute">Product specification attribute mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertProductSpecificationAttribute(ProductSpecificationAttribute productSpecificationAttribute, string productId)
        {
            if (productSpecificationAttribute == null)
                throw new ArgumentNullException(nameof(productSpecificationAttribute));

            await _productRepository.AddToSet(productId, x => x.ProductSpecificationAttributes, productSpecificationAttribute);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(productSpecificationAttribute);
        }

        /// <summary>
        /// Updates the product specification attribute mapping
        /// </summary>
        /// <param name="productSpecificationAttribute">Product specification attribute mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task UpdateProductSpecificationAttribute(ProductSpecificationAttribute productSpecificationAttribute, string productId)
        {
            if (productSpecificationAttribute == null)
                throw new ArgumentNullException(nameof(productSpecificationAttribute));

            await _productRepository.UpdateToSet(productId, x => x.ProductSpecificationAttributes, z => z.Id, productSpecificationAttribute.Id, productSpecificationAttribute);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(productSpecificationAttribute);
        }
        /// <summary>
        /// Deletes a product specification attribute mapping
        /// </summary>
        /// <param name="productSpecificationAttribute">Product specification attribute</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteProductSpecificationAttribute(ProductSpecificationAttribute productSpecificationAttribute, string productId)
        {
            if (productSpecificationAttribute == null)
                throw new ArgumentNullException(nameof(productSpecificationAttribute));

            await _productRepository.PullFilter(productId, x => x.ProductSpecificationAttributes, x => x.Id, productSpecificationAttribute.Id);

            //clear cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(productSpecificationAttribute);
        }

        /// <summary>
        /// Gets a count of product specification attribute mapping records
        /// </summary>
        /// <param name="productId">Product identifier; "" to load all records</param>
        /// <param name="specificationAttributeOptionId">The specification attribute option identifier; "" to load all records</param>
        /// <returns>Count</returns>
        public virtual int GetProductSpecificationAttributeCount(string productId = "", string specificationAttributeOptionId = "")
        {
            var query = from p in _productRepository.Table
                        select p;

            if (!string.IsNullOrEmpty(productId))
                query = query.Where(psa => psa.Id == productId);
            if (!string.IsNullOrEmpty(specificationAttributeOptionId))
                query = query.Where(psa => psa.ProductSpecificationAttributes.Any(x => x.SpecificationAttributeOptionId == specificationAttributeOptionId));

            return query.Count();
        }

        #endregion

        #endregion
    }
}
