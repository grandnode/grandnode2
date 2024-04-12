using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData;

[Route("odata/Product")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class ProductController : BaseODataController
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

    [SwaggerOperation("Get entity from Product by key", OperationId = "GetProductById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        return Ok(product.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from Product", OperationId = "GetProducts")]
    [HttpGet]
    [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<ProductDto, Product>()));
    }

    [SwaggerOperation("Add new entity to Product", OperationId = "InsertProduct")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] ProductDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        model = await _mediator.Send(new AddProductCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in Product", OperationId = "UpdateProduct")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Put([FromBody] ProductDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        await _mediator.Send(new UpdateProductCommand { Model = model });
        return Ok();
    }

    [SwaggerOperation("Partially update entity in Product", OperationId = "PartiallyUpdateProduct")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<ProductDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pr = product.FirstOrDefault();
        model.ApplyTo(pr);
        await _mediator.Send(new UpdateProductCommand { Model = pr });
        return Ok();
    }

    [SwaggerOperation("Delete entity in Product", OperationId = "DeleteProduct")]
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        await _mediator.Send(new DeleteProductCommand { Model = product.FirstOrDefault() });

        return Ok();
    }

    //odata/Product/id/UpdateStock
    //body: { "WarehouseId": "", "Stock": 10 }
    [SwaggerOperation("Invoke action UpdateStock", OperationId = "UpdateStock")]
    [HttpPost("/{key}/UpdateStock")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateStock([FromRoute] string key, [FromBody] ProductUpdateStock model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        if (model == null) return BadRequest();

        await _mediator.Send(new UpdateProductStockCommand
            { Product = product.FirstOrDefault(), WarehouseId = model.WarehouseId, Stock = model.Stock });

        return Ok(true);
    }

    #region Product category

    [SwaggerOperation("Invoke action CreateProductCategory", OperationId = "CreateProductCategory")]
    [HttpPost("/{key}/CreateProductCategory")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateProductCategory([FromRoute] string key,
        [FromBody] ProductCategoryDto productCategory)
    {
        if (productCategory == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pc = product.FirstOrDefault()!.ProductCategories.FirstOrDefault(x =>
            x.CategoryId == productCategory.CategoryId);
        if (pc != null) return BadRequest("Product category mapping found with the specified CategoryId");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductCategoryCommand
                { Product = product.FirstOrDefault(), Model = productCategory });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action UpdateProductCategory", OperationId = "UpdateProductCategory")]
    [HttpPost("/{key}/UpdateProductCategory")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateProductCategory([FromRoute] string key,
        [FromBody] ProductCategoryDto productCategory)
    {
        if (productCategory == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pc = product.FirstOrDefault()!.ProductCategories.FirstOrDefault(x =>
            x.CategoryId == productCategory.CategoryId);
        if (pc == null) return BadRequest("No product category mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductCategoryCommand
                { Product = product.FirstOrDefault(), Model = productCategory });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action DeleteProductCategory", OperationId = "DeleteProductCategory")]
    [HttpPost("/{key}/DeleteProductCategory")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProductCategory([FromRoute] string key,
        [FromBody] ProductCategoryDeleteDto model)
    {
        if (model == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var categoryId = model.CategoryId;
        if (!string.IsNullOrEmpty(categoryId))
        {
            var pc = product.FirstOrDefault()!.ProductCategories.FirstOrDefault(x => x.CategoryId == categoryId);
            if (pc == null) return BadRequest("No product category mapping found with the specified id");

            if (ModelState.IsValid)
            {
                _ = await _mediator.Send(new DeleteProductCategoryCommand
                    { Product = product.FirstOrDefault(), CategoryId = categoryId });
                return Ok(true);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product collection

    [SwaggerOperation("Invoke action CreateProductCollection", OperationId = "CreateProductCollection")]
    [HttpPost("/{key}/CreateProductCollection")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateProductCollection([FromRoute] string key,
        [FromBody] ProductCollectionDto productCollection)
    {
        if (productCollection == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pm = product.FirstOrDefault()!.ProductCollections.FirstOrDefault(x =>
            x.CollectionId == productCollection.CollectionId);
        if (pm != null) return BadRequest("Product collection mapping found with the specified CollectionId");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductCollectionCommand
                { Product = product.FirstOrDefault(), Model = productCollection });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action UpdateProductCollection", OperationId = "UpdateProductCollection")]
    [HttpPost("/{key}/UpdateProductCollection")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateProductCollection([FromRoute] string key,
        [FromBody] ProductCollectionDto productCollection)
    {
        if (productCollection == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pm = product.FirstOrDefault()!.ProductCollections.FirstOrDefault(x =>
            x.CollectionId == productCollection.CollectionId);
        if (pm == null) return BadRequest("No product collection mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductCollectionCommand
                { Product = product.FirstOrDefault(), Model = productCollection });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action DeleteProductCollection", OperationId = "DeleteProductCollection")]
    [HttpPost("/{key}/DeleteProductCollection")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProductCollection([FromRoute] string key,
        [FromBody] ProductCollectionDeleteDto model)
    {
        if (model == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var collectionId = model.CollectionId;
        if (!string.IsNullOrEmpty(collectionId))
        {
            var pm = product.FirstOrDefault()!.ProductCollections.FirstOrDefault(x => x.CollectionId == collectionId);
            if (pm == null) return BadRequest("No product collection mapping found with the specified id");

            if (ModelState.IsValid)
            {
                await _mediator.Send(new DeleteProductCollectionCommand
                    { Product = product.FirstOrDefault(), CollectionId = collectionId });
                return Ok(true);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product picture

    [SwaggerOperation("Invoke action CreateProductPicture", OperationId = "CreateProductPicture")]
    [HttpPost("/{key}/CreateProductPicture")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateProductPicture([FromRoute] string key,
        [FromBody] ProductPictureDto productPicture)
    {
        if (productPicture == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pp = product.FirstOrDefault()!.ProductPictures.FirstOrDefault(x => x.PictureId == productPicture.PictureId);
        if (pp != null) return BadRequest("Product picture mapping found with the specified pictureid");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductPictureCommand
                { Product = product.FirstOrDefault(), Model = productPicture });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action UpdateProductPicture", OperationId = "UpdateProductPicture")]
    [HttpPost("/{key}/UpdateProductPicture")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateProductPicture([FromRoute] string key,
        [FromBody] ProductPictureDto productPicture)
    {
        if (productPicture == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pp = product.FirstOrDefault()!.ProductPictures.FirstOrDefault(x => x.PictureId == productPicture.PictureId);
        if (pp == null) return BadRequest("No product picture mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductPictureCommand
                { Product = product.FirstOrDefault(), Model = productPicture });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action DeleteProductPicture", OperationId = "DeleteProductPicture")]
    [HttpPost("/{key}/DeleteProductPicture")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProductPicture([FromRoute] string key,
        [FromBody] ProductPictureDeleteDto model)
    {
        if (model == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pictureId = model.PictureId;
        if (!string.IsNullOrEmpty(pictureId))
        {
            var pp = product.FirstOrDefault()!.ProductPictures.FirstOrDefault(x => x.PictureId == pictureId);
            if (pp == null) return BadRequest("No product picture mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new DeleteProductPictureCommand
                    { Product = product.FirstOrDefault(), PictureId = pictureId });
                return Ok(result);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product specification

    [SwaggerOperation("Invoke action CreateProductSpecification", OperationId = "CreateProductSpecification")]
    [HttpPost("/{key}/CreateProductSpecification")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateProductSpecification([FromRoute] string key,
        [FromBody] ProductSpecificationAttributeDto productSpecification)
    {
        if (productSpecification == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var psa = product.FirstOrDefault()!.ProductSpecificationAttributes.FirstOrDefault(x =>
            x.Id == productSpecification.Id);
        if (psa != null) return BadRequest("Product specification mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductSpecificationCommand
                { Product = product.FirstOrDefault(), Model = productSpecification });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action UpdateProductSpecification", OperationId = "UpdateProductSpecification")]
    [HttpPost("/{key}/UpdateProductSpecification")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateProductSpecification([FromRoute] string key,
        [FromBody] ProductSpecificationAttributeDto productSpecification)
    {
        if (productSpecification == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var psa = product.FirstOrDefault()!.ProductSpecificationAttributes.FirstOrDefault(x =>
            x.Id == productSpecification.Id);
        if (psa == null) return BadRequest("No product specification mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductSpecificationCommand
                { Product = product.FirstOrDefault(), Model = productSpecification });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action DeleteProductSpecification", OperationId = "DeleteProductSpecification")]
    [HttpPost("/{key}/DeleteProductSpecification")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProductSpecification([FromRoute] string key,
        [FromBody] ProductSpecificationAttributeDeleteDto model)
    {
        if (model == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var specificationId = model.Id;
        if (!string.IsNullOrEmpty(specificationId))
        {
            var psa = product.FirstOrDefault()!.ProductSpecificationAttributes.FirstOrDefault(x =>
                x.Id == specificationId);
            if (psa == null) return BadRequest("No product specification mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new DeleteProductSpecificationCommand
                    { Product = product.FirstOrDefault(), Id = specificationId });
                return Ok(result);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product tierprice

    [SwaggerOperation("Invoke action CreateProductTierPrice", OperationId = "CreateProductTierPrice")]
    [HttpPost("/{key}/CreateProductTierPrice")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateProductTierPrice([FromRoute] string key,
        [FromBody] ProductTierPriceDto productTierPrice)
    {
        if (productTierPrice == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pt = product.FirstOrDefault()!.TierPrices.FirstOrDefault(x => x.Id == productTierPrice.Id);
        if (pt != null) return BadRequest("Product tier price mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductTierPriceCommand
                { Product = product.FirstOrDefault(), Model = productTierPrice });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action UpdateProductTierPrice", OperationId = "UpdateProductTierPrice")]
    [HttpPost("/{key}/UpdateProductTierPrice")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateProductTierPrice([FromRoute] string key,
        [FromBody] ProductTierPriceDto productTierPrice)
    {
        if (productTierPrice == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pt = product.FirstOrDefault()!.TierPrices.FirstOrDefault(x => x.Id == productTierPrice.Id);
        if (pt == null) return BadRequest("No product tier price mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductTierPriceCommand
                { Product = product.FirstOrDefault(), Model = productTierPrice });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action DeleteProductTierPrice", OperationId = "DeleteProductTierPrice")]
    [HttpPost("/{key}/DeleteProductTierPrice")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProductTierPrice([FromRoute] string key,
        [FromBody] ProductTierPriceDeleteDto model)
    {
        if (model == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var tierPriceId = model.Id;
        if (!string.IsNullOrEmpty(tierPriceId))
        {
            var pt = product.FirstOrDefault()!.TierPrices.FirstOrDefault(x => x.Id == tierPriceId);
            if (pt == null) return BadRequest("No product tier price mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new DeleteProductTierPriceCommand
                    { Product = product.FirstOrDefault(), Id = tierPriceId });
                return Ok(result);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product attribute mapping

    [SwaggerOperation("Invoke action CreateProductAttributeMapping", OperationId = "CreateProductAttributeMapping")]
    [HttpPost("/{key}/CreateProductAttributeMapping")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateProductAttributeMapping([FromRoute] string key,
        [FromBody] ProductAttributeMappingDto productAttributeMapping)
    {
        if (productAttributeMapping == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pam = product.FirstOrDefault()!.ProductAttributeMappings.FirstOrDefault(x =>
            x.Id == productAttributeMapping.Id);
        if (pam != null) return BadRequest("Product attribute mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductAttributeMappingCommand
                { Product = product.FirstOrDefault(), Model = productAttributeMapping });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action UpdateProductAttributeMapping", OperationId = "UpdateProductAttributeMapping")]
    [HttpPost("/{key}/UpdateProductAttributeMapping")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateProductAttributeMapping([FromRoute] string key,
        [FromBody] ProductAttributeMappingDto productAttributeMapping)
    {
        if (productAttributeMapping == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pam = product.FirstOrDefault()!.ProductAttributeMappings.FirstOrDefault(x =>
            x.Id == productAttributeMapping.Id);
        if (pam == null) return BadRequest("No product attribute mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductAttributeMappingCommand
                { Product = product.FirstOrDefault(), Model = productAttributeMapping });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Invoke action DeleteProductAttributeMapping", OperationId = "DeleteProductAttributeMapping")]
    [HttpPost("/{key}/DeleteProductAttributeMapping")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProductAttributeMapping([FromRoute] string key,
        [FromBody] ProductAttributeMappingDeleteDto model)
    {
        if (model == null) return BadRequest();

        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var attrId = model.Id;
        if (string.IsNullOrEmpty(attrId)) return NotFound();

        var pam = product.FirstOrDefault()!.ProductAttributeMappings.FirstOrDefault(x => x.Id == attrId);
        if (pam == null) return BadRequest("No product attribute mapping found with the specified id");

        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _mediator.Send(new DeleteProductAttributeMappingCommand
            { Product = product.FirstOrDefault(), Model = pam });
        return Ok(result);
    }

    #endregion
}