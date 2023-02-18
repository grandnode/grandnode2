﻿using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class ProductController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public ProductController(
            IMediator mediator,
            IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from Product by key", OperationId = "GetProductById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            return Ok(product.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from Product", OperationId = "GetProducts")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>()));
        }

        [SwaggerOperation(summary: "Add new entity to Product", OperationId = "InsertProduct")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] ProductDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            model = await _mediator.Send(new AddProductCommand() { Model = model });
            return Ok(model);
        }

        [SwaggerOperation(summary: "Update entity in Product", OperationId = "UpdateProduct")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Put([FromBody] ProductDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            await _mediator.Send(new UpdateProductCommand() { Model = model });
            return Ok();
        }

        [SwaggerOperation(summary: "Partially update entity in Product", OperationId = "PartiallyUpdateProduct")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<ProductDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pr = product.FirstOrDefault();
            model.ApplyTo(pr, ModelState);
            await _mediator.Send(new UpdateProductCommand() { Model = pr });
            return Ok();
        }

        [SwaggerOperation(summary: "Delete entity in Product", OperationId = "DeleteProduct")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            await _mediator.Send(new DeleteProductCommand() { Model = product.FirstOrDefault() });

            return Ok();
        }

        //odata/Product/(id)/UpdateStock
        //body: { "WarehouseId": "", "Stock": 10 }
        [SwaggerOperation(summary: "Invoke action UpdateStock", OperationId = "UpdateStock")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateStock(string key, [FromBody] ProductUpdateStock model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            if (model == null) return BadRequest();

            await _mediator.Send(new UpdateProductStockCommand() { Product = product.FirstOrDefault(), WarehouseId = model.WarehouseId, Stock = model.Stock });

            return Ok(true);
        }

        #region Product category

        [SwaggerOperation(summary: "Invoke action CreateProductCategory", OperationId = "CreateProductCategory")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateProductCategory(string key, [FromBody] ProductCategoryDto productCategory)
        {
            if (productCategory == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pc = product.FirstOrDefault().ProductCategories.Where(x => x.CategoryId == productCategory.CategoryId).FirstOrDefault();
            if (pc != null) ModelState.AddModelError("", "Product category mapping found with the specified categoryid");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new AddProductCategoryCommand() { Product = product.FirstOrDefault(), Model = productCategory });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action UpdateProductCategory", OperationId = "UpdateProductCategory")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductCategory(string key, [FromBody] ProductCategoryDto productCategory)
        {
            if (productCategory == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pc = product.FirstOrDefault().ProductCategories.Where(x => x.CategoryId == productCategory.CategoryId).FirstOrDefault();
            if (pc == null) ModelState.AddModelError("", "No product category mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new UpdateProductCategoryCommand() { Product = product.FirstOrDefault(), Model = productCategory });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action DeleteProductCategory", OperationId = "DeleteProductCategory")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductCategory(string key, [FromBody] ProductCategoryDeleteDto model)
        {
            if (model == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var categoryId = model.CategoryId;
            if (!string.IsNullOrEmpty(categoryId))
            {
                var pc = product.FirstOrDefault().ProductCategories.Where(x => x.CategoryId == categoryId.ToString()).FirstOrDefault();
                if (pc == null) ModelState.AddModelError("", "No product category mapping found with the specified id");

                if (ModelState.IsValid)
                {
                    var result = await _mediator.Send(new DeleteProductCategoryCommand() { Product = product.FirstOrDefault(), CategoryId = categoryId.ToString() });
                    return Ok(true);
                }
                return BadRequest(ModelState);
            }
            return NotFound();
        }

        #endregion

        #region Product collection

        [SwaggerOperation(summary: "Invoke action CreateProductCollection", OperationId = "CreateProductCollection")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateProductCollection(string key, [FromBody] ProductCollectionDto productCollection)
        {
            if (productCollection == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pm = product.FirstOrDefault().ProductCollections.Where(x => x.CollectionId == productCollection.CollectionId).FirstOrDefault();
            if (pm != null) ModelState.AddModelError("", "Product collection mapping found with the specified collectionid");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new AddProductCollectionCommand() { Product = product.FirstOrDefault(), Model = productCollection });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action UpdateProductCollection", OperationId = "UpdateProductCollection")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductCollection(string key, [FromBody] ProductCollectionDto productCollection)
        {
            if (productCollection == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pm = product.FirstOrDefault().ProductCollections.Where(x => x.CollectionId == productCollection.CollectionId).FirstOrDefault();
            if (pm == null) ModelState.AddModelError("", "No product collection mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new UpdateProductCollectionCommand() { Product = product.FirstOrDefault(), Model = productCollection });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action DeleteProductCollection", OperationId = "DeleteProductCollection")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductCollection(string key, [FromBody] ProductCollectionDeleteDto model)
        {
            if (model == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var collectionId = model.CollectionId;
            if (!string.IsNullOrEmpty(collectionId))
            {
                var pm = product.FirstOrDefault().ProductCollections.Where(x => x.CollectionId == collectionId.ToString()).FirstOrDefault();
                if (pm == null) ModelState.AddModelError("", "No product collection mapping found with the specified id");

                if (ModelState.IsValid)
                {
                    var result = await _mediator.Send(new DeleteProductCollectionCommand() { Product = product.FirstOrDefault(), CollectionId = collectionId.ToString() });
                    return Ok(true);
                }
                return BadRequest(ModelState);
            }
            return NotFound();
        }

        #endregion

        #region Product picture

        [SwaggerOperation(summary: "Invoke action CreateProductPicture", OperationId = "CreateProductPicture")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateProductPicture(string key, [FromBody] ProductPictureDto productPicture)
        {
            if (productPicture == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pp = product.FirstOrDefault().ProductPictures.Where(x => x.PictureId == productPicture.PictureId).FirstOrDefault();
            if (pp != null) ModelState.AddModelError("", "Product picture mapping found with the specified pictureid");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new AddProductPictureCommand() { Product = product.FirstOrDefault(), Model = productPicture });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action UpdateProductPicture", OperationId = "UpdateProductPicture")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductPicture(string key, [FromBody] ProductPictureDto productPicture)
        {
            if (productPicture == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pp = product.FirstOrDefault().ProductPictures.Where(x => x.PictureId == productPicture.PictureId).FirstOrDefault();
            if (pp == null) ModelState.AddModelError("", "No product picture mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new UpdateProductPictureCommand() { Product = product.FirstOrDefault(), Model = productPicture });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action DeleteProductPicture", OperationId = "DeleteProductPicture")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductPicture(string key, [FromBody] ProductPictureDeleteDto model)
        {
            if (model == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pictureId = model.PictureId;
            if (!string.IsNullOrEmpty(pictureId))
            {
                var pp = product.FirstOrDefault().ProductPictures.Where(x => x.PictureId == pictureId.ToString()).FirstOrDefault();
                if (pp == null) ModelState.AddModelError("", "No product picture mapping found with the specified id");

                if (ModelState.IsValid)
                {
                    var result = await _mediator.Send(new DeleteProductPictureCommand() { Product = product.FirstOrDefault(), PictureId = pictureId.ToString() });
                    return Ok(result);
                }
                return BadRequest(ModelState);
            }
            return NotFound();
        }

        #endregion

        #region Product specification

        [SwaggerOperation(summary: "Invoke action CreateProductSpecification", OperationId = "CreateProductSpecification")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateProductSpecification(string key, [FromBody] ProductSpecificationAttributeDto productSpecification)
        {
            if (productSpecification == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var psa = product.FirstOrDefault().ProductSpecificationAttributes.Where(x => x.Id == productSpecification.Id).FirstOrDefault();
            if (psa != null) ModelState.AddModelError("", "Product specification mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new AddProductSpecificationCommand() { Product = product.FirstOrDefault(), Model = productSpecification });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action UpdateProductSpecification", OperationId = "UpdateProductSpecification")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductSpecification(string key, [FromBody] ProductSpecificationAttributeDto productSpecification)
        {
            if (productSpecification == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var psa = product.FirstOrDefault().ProductSpecificationAttributes.Where(x => x.Id == productSpecification.Id).FirstOrDefault();
            if (psa == null) ModelState.AddModelError("", "No product specification mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new UpdateProductSpecificationCommand() { Product = product.FirstOrDefault(), Model = productSpecification });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action DeleteProductSpecification", OperationId = "DeleteProductSpecification")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductSpecification(string key, [FromBody] ProductSpecificationAttributeDeleteDto model)
        {
            if (model == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var specificationId = model.Id;
            if (!string.IsNullOrEmpty(specificationId))
            {
                var psa = product.FirstOrDefault().ProductSpecificationAttributes.Where(x => x.Id == specificationId.ToString()).FirstOrDefault();
                if (psa == null) ModelState.AddModelError("", "No product specification mapping found with the specified id");

                if (ModelState.IsValid)
                {
                    var result = await _mediator.Send(new DeleteProductSpecificationCommand() { Product = product.FirstOrDefault(), Id = specificationId.ToString() });
                    return Ok(result);
                }
                return BadRequest(ModelState);
            }
            return NotFound();
        }

        #endregion

        #region Product tierprice

        [SwaggerOperation(summary: "Invoke action CreateProductTierPrice", OperationId = "CreateProductTierPrice")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateProductTierPrice(string key, [FromBody] ProductTierPriceDto productTierPrice)
        {
            if (productTierPrice == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pt = product.FirstOrDefault().TierPrices.Where(x => x.Id == productTierPrice.Id).FirstOrDefault();
            if (pt != null) ModelState.AddModelError("", "Product tier price mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new AddProductTierPriceCommand() { Product = product.FirstOrDefault(), Model = productTierPrice });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action UpdateProductTierPrice", OperationId = "UpdateProductTierPrice")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductTierPrice(string key, [FromBody] ProductTierPriceDto productTierPrice)
        {
            if (productTierPrice == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pt = product.FirstOrDefault().TierPrices.Where(x => x.Id == productTierPrice.Id).FirstOrDefault();
            if (pt == null) ModelState.AddModelError("", "No product tier price mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new UpdateProductTierPriceCommand() { Product = product.FirstOrDefault(), Model = productTierPrice });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action DeleteProductTierPrice", OperationId = "DeleteProductTierPrice")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductTierPrice(string key, [FromBody] ProductTierPriceDeleteDto model)
        {
            if (model == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var tierPriceId = model.Id;
            if (!string.IsNullOrEmpty(tierPriceId))
            {
                var pt = product.FirstOrDefault().TierPrices.Where(x => x.Id == tierPriceId.ToString()).FirstOrDefault();
                if (pt == null) ModelState.AddModelError("", "No product tier price mapping found with the specified id");

                if (ModelState.IsValid)
                {
                    var result = await _mediator.Send(new DeleteProductTierPriceCommand() { Product = product.FirstOrDefault(), Id = tierPriceId.ToString() });
                    return Ok(result);
                }
                return BadRequest(ModelState);
            }
            return NotFound();
        }

        #endregion

        #region Product attribute mapping

        [SwaggerOperation(summary: "Invoke action CreateProductAttributeMapping", OperationId = "CreateProductAttributeMapping")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateProductAttributeMapping(string key, [FromBody] ProductAttributeMappingDto productAttributeMapping)
        {
            if (productAttributeMapping == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pam = product.FirstOrDefault().ProductAttributeMappings.Where(x => x.Id == productAttributeMapping.Id).FirstOrDefault();
            if (pam != null) ModelState.AddModelError("", "Product attribute mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new AddProductAttributeMappingCommand() { Product = product.FirstOrDefault(), Model = productAttributeMapping });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action UpdateProductAttributeMapping", OperationId = "UpdateProductAttributeMapping")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductAttributeMapping(string key, [FromBody] ProductAttributeMappingDto productAttributeMapping)
        {
            if (productAttributeMapping == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var pam = product.FirstOrDefault().ProductAttributeMappings.Where(x => x.Id == productAttributeMapping.Id).FirstOrDefault();
            if (pam == null) ModelState.AddModelError("", "No product attribute mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new UpdateProductAttributeMappingCommand() { Product = product.FirstOrDefault(), Model = productAttributeMapping });
                return Ok(result);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Invoke action DeleteProductAttributeMapping", OperationId = "DeleteProductAttributeMapping")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductAttributeMapping(string key, [FromBody] ProductAttributeMappingDeleteDto model)
        {
            if (model == null) return BadRequest();

            if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

            var product = await _mediator.Send(new GetGenericQuery<ProductDto, Domain.Catalog.Product>(key));
            if (!product.Any()) return NotFound();

            var attrId = model.Id;
            if (!string.IsNullOrEmpty(attrId))
            {
                var pam = product.FirstOrDefault().ProductAttributeMappings.Where(x => x.Id == attrId.ToString()).FirstOrDefault();
                if (pam == null) ModelState.AddModelError("", "No product attribute mapping found with the specified id");

                if (ModelState.IsValid)
                {
                    var result = await _mediator.Send(new DeleteProductAttributeMappingCommand() { Product = product.FirstOrDefault(), Model = pam });
                    return Ok(result);
                }
                return BadRequest(ModelState);
            }
            return NotFound();
        }
        #endregion
    }
}
