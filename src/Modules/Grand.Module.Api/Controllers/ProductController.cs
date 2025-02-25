using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class ProductController : BaseApiController
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

    [EndpointDescription("Get entity from Product by key")]
    [EndpointName("GetProductById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        return Ok(product.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Product")]
    [EndpointName("GetProducts")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<ProductDto, Product>()));
    }

    [EndpointDescription("Add new entity to Product")]
    [EndpointName("InsertProduct")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] ProductDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        model = await _mediator.Send(new AddProductCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Product")]
    [EndpointName("UpdateProduct")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] ProductDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        await _mediator.Send(new UpdateProductCommand { Model = model });
        return Ok();
    }

    [EndpointDescription("Partially update entity in Product")]
    [EndpointName("PartiallyUpdateProduct")]
    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<ProductDto> model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pr = product.FirstOrDefault();
        model.ApplyTo(pr);
        await _mediator.Send(new UpdateProductCommand { Model = pr });
        return Ok();
    }

    [EndpointDescription("Delete entity in Product")]
    [EndpointName("DeleteProduct")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        await _mediator.Send(new DeleteProductCommand { Model = product.FirstOrDefault() });

        return Ok();
    }

    //api/Product/id/UpdateStock
    //body: { "WarehouseId": "", "Stock": 10 }
    [EndpointDescription("Invoke action UpdateStock")]
    [EndpointName("UpdateStock")]
    [HttpPost("{key}/UpdateStock")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock([FromRoute] string key, [FromBody] ProductUpdateStock model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        if (model == null) return BadRequest();

        await _mediator.Send(new UpdateProductStockCommand { Product = product.FirstOrDefault(), WarehouseId = model.WarehouseId, Stock = model.Stock });

        return Ok(true);
    }

    #region Product category

    [EndpointDescription("Invoke action CreateProductCategory")]
    [EndpointName("CreateProductCategory")]
    [HttpPost("{key}/CreateProductCategory")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProductCategory([FromRoute] string key, [FromBody] ProductCategoryDto productCategory)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productCategory == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pc = product.FirstOrDefault()!.ProductCategories.FirstOrDefault(x =>
            x.CategoryId == productCategory.CategoryId);
        if (pc != null) return BadRequest("Product category mapping found with the specified CategoryId");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductCategoryCommand { Product = product.FirstOrDefault(), Model = productCategory });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action UpdateProductCategory")]
    [EndpointName("UpdateProductCategory")]
    [HttpPost("{key}/UpdateProductCategory")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductCategory([FromRoute] string key, [FromBody] ProductCategoryDto productCategory)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productCategory == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pc = product.FirstOrDefault()!.ProductCategories.FirstOrDefault(x =>
            x.CategoryId == productCategory.CategoryId);
        if (pc == null) return BadRequest("No product category mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductCategoryCommand { Product = product.FirstOrDefault(), Model = productCategory });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action DeleteProductCategory")]
    [EndpointName("DeleteProductCategory")]
    [HttpPost("{key}/DeleteProductCategory")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductCategory([FromRoute] string key, [FromBody] ProductCategoryDeleteDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (model == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var categoryId = model.CategoryId;
        if (!string.IsNullOrEmpty(categoryId))
        {
            var pc = product.FirstOrDefault()!.ProductCategories.FirstOrDefault(x => x.CategoryId == categoryId);
            if (pc == null) return BadRequest("No product category mapping found with the specified id");

            if (ModelState.IsValid)
            {
                _ = await _mediator.Send(new DeleteProductCategoryCommand { Product = product.FirstOrDefault(), CategoryId = categoryId });
                return Ok(true);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product collection

    [EndpointDescription("Invoke action CreateProductCollection")]
    [EndpointName("CreateProductCollection")]
    [HttpPost("{key}/CreateProductCollection")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProductCollection([FromRoute] string key, [FromBody] ProductCollectionDto productCollection)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productCollection == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pm = product.FirstOrDefault()!.ProductCollections.FirstOrDefault(x => x.CollectionId == productCollection.CollectionId);
        if (pm != null) return BadRequest("Product collection mapping found with the specified CollectionId");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductCollectionCommand { Product = product.FirstOrDefault(), Model = productCollection });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action UpdateProductCollection")]
    [EndpointName("UpdateProductCollection")]
    [HttpPost("{key}/UpdateProductCollection")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductCollection([FromRoute] string key, [FromBody] ProductCollectionDto productCollection)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productCollection == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pm = product.FirstOrDefault()!.ProductCollections.FirstOrDefault(x =>
            x.CollectionId == productCollection.CollectionId);
        if (pm == null) return BadRequest("No product collection mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductCollectionCommand { Product = product.FirstOrDefault(), Model = productCollection });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action DeleteProductCollection")]
    [EndpointName("DeleteProductCollection")]
    [HttpPost("{key}/DeleteProductCollection")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductCollection([FromRoute] string key, [FromBody] ProductCollectionDeleteDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (model == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var collectionId = model.CollectionId;
        if (!string.IsNullOrEmpty(collectionId))
        {
            var pm = product.FirstOrDefault()!.ProductCollections.FirstOrDefault(x => x.CollectionId == collectionId);
            if (pm == null) return BadRequest("No product collection mapping found with the specified id");

            if (ModelState.IsValid)
            {
                await _mediator.Send(new DeleteProductCollectionCommand { Product = product.FirstOrDefault(), CollectionId = collectionId });
                return Ok(true);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product picture

    [EndpointDescription("Invoke action CreateProductPicture")]
    [EndpointName("CreateProductPicture")]
    [HttpPost("{key}/CreateProductPicture")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProductPicture([FromRoute] string key, [FromBody] ProductPictureDto productPicture)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productPicture == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pp = product.FirstOrDefault()!.ProductPictures.FirstOrDefault(x => x.PictureId == productPicture.PictureId);
        if (pp != null) return BadRequest("Product picture mapping found with the specified pictureid");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductPictureCommand { Product = product.FirstOrDefault(), Model = productPicture });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action UpdateProductPicture")]
    [EndpointName("UpdateProductPicture")]
    [HttpPost("{key}/UpdateProductPicture")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductPicture([FromRoute] string key, [FromBody] ProductPictureDto productPicture)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productPicture == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pp = product.FirstOrDefault()!.ProductPictures.FirstOrDefault(x => x.PictureId == productPicture.PictureId);
        if (pp == null) return BadRequest("No product picture mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductPictureCommand { Product = product.FirstOrDefault(), Model = productPicture });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action DeleteProductPicture")]
    [EndpointName("DeleteProductPicture")]
    [HttpPost("{key}/DeleteProductPicture")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductPicture([FromRoute] string key, [FromBody] ProductPictureDeleteDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (model == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pictureId = model.PictureId;
        if (!string.IsNullOrEmpty(pictureId))
        {
            var pp = product.FirstOrDefault()!.ProductPictures.FirstOrDefault(x => x.PictureId == pictureId);
            if (pp == null) return BadRequest("No product picture mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new DeleteProductPictureCommand { Product = product.FirstOrDefault(), PictureId = pictureId });
                return Ok(result);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product specification

    [EndpointDescription("Invoke action CreateProductSpecification")]
    [EndpointName("CreateProductSpecification")]
    [HttpPost("{key}/CreateProductSpecification")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProductSpecification([FromRoute] string key, [FromBody] ProductSpecificationAttributeDto productSpecification)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productSpecification == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var psa = product.FirstOrDefault()!.ProductSpecificationAttributes.FirstOrDefault(x =>
            x.Id == productSpecification.Id);
        if (psa != null) return BadRequest("Product specification mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductSpecificationCommand { Product = product.FirstOrDefault(), Model = productSpecification });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action UpdateProductSpecification")]
    [EndpointName("UpdateProductSpecification")]
    [HttpPost("{key}/UpdateProductSpecification")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductSpecification([FromRoute] string key, [FromBody] ProductSpecificationAttributeDto productSpecification)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productSpecification == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var psa = product.FirstOrDefault()!.ProductSpecificationAttributes.FirstOrDefault(x =>
            x.Id == productSpecification.Id);
        if (psa == null) return BadRequest("No product specification mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductSpecificationCommand { Product = product.FirstOrDefault(), Model = productSpecification });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action DeleteProductSpecification")]
    [EndpointName("DeleteProductSpecification")]
    [HttpPost("{key}/DeleteProductSpecification")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductSpecification([FromRoute] string key, [FromBody] ProductSpecificationAttributeDeleteDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (model == null) return BadRequest();

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
                var result = await _mediator.Send(new DeleteProductSpecificationCommand { Product = product.FirstOrDefault(), Id = specificationId });
                return Ok(result);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product tierprice

    [EndpointDescription("Invoke action CreateProductTierPrice")]
    [EndpointName("CreateProductTierPrice")]
    [HttpPost("{key}/CreateProductTierPrice")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProductTierPrice([FromRoute] string key, [FromBody] ProductTierPriceDto productTierPrice)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productTierPrice == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pt = product.FirstOrDefault()!.TierPrices.FirstOrDefault(x => x.Id == productTierPrice.Id);
        if (pt != null) return BadRequest("Product tier price mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductTierPriceCommand { Product = product.FirstOrDefault(), Model = productTierPrice });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action UpdateProductTierPrice")]
    [EndpointName("UpdateProductTierPrice")]
    [HttpPost("{key}/UpdateProductTierPrice")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductTierPrice([FromRoute] string key, [FromBody] ProductTierPriceDto productTierPrice)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productTierPrice == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pt = product.FirstOrDefault()!.TierPrices.FirstOrDefault(x => x.Id == productTierPrice.Id);
        if (pt == null) return BadRequest("No product tier price mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductTierPriceCommand { Product = product.FirstOrDefault(), Model = productTierPrice });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action DeleteProductTierPrice")]
    [EndpointName("DeleteProductTierPrice")]
    [HttpPost("{key}/DeleteProductTierPrice")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductTierPrice([FromRoute] string key, [FromBody] ProductTierPriceDeleteDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (model == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var tierPriceId = model.Id;
        if (!string.IsNullOrEmpty(tierPriceId))
        {
            var pt = product.FirstOrDefault()!.TierPrices.FirstOrDefault(x => x.Id == tierPriceId);
            if (pt == null) return BadRequest("No product tier price mapping found with the specified id");

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new DeleteProductTierPriceCommand { Product = product.FirstOrDefault(), Id = tierPriceId });
                return Ok(result);
            }

            return BadRequest(ModelState);
        }

        return NotFound();
    }

    #endregion

    #region Product attribute mapping

    [EndpointDescription("Invoke action CreateProductAttributeMapping")]
    [EndpointName("CreateProductAttributeMapping")]
    [HttpPost("{key}/CreateProductAttributeMapping")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProductAttributeMapping([FromRoute] string key, [FromBody] ProductAttributeMappingDto productAttributeMapping)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productAttributeMapping == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pam = product.FirstOrDefault()!.ProductAttributeMappings.FirstOrDefault(x =>
            x.Id == productAttributeMapping.Id);
        if (pam != null) return BadRequest("Product attribute mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new AddProductAttributeMappingCommand { Product = product.FirstOrDefault(), Model = productAttributeMapping });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action UpdateProductAttributeMapping")]
    [EndpointName("UpdateProductAttributeMapping")]
    [HttpPost("{key}/UpdateProductAttributeMapping")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductAttributeMapping([FromRoute] string key, [FromBody] ProductAttributeMappingDto productAttributeMapping)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (productAttributeMapping == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var pam = product.FirstOrDefault()!.ProductAttributeMappings.FirstOrDefault(x =>
            x.Id == productAttributeMapping.Id);
        if (pam == null) return BadRequest("No product attribute mapping found with the specified id");

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new UpdateProductAttributeMappingCommand { Product = product.FirstOrDefault(), Model = productAttributeMapping });
            return Ok(result);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Invoke action DeleteProductAttributeMapping")]
    [EndpointName("DeleteProductAttributeMapping")]
    [HttpPost("{key}/DeleteProductAttributeMapping")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductAttributeMapping([FromRoute] string key, [FromBody] ProductAttributeMappingDeleteDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Products)) return Forbid();

        if (model == null) return BadRequest();

        var product = await _mediator.Send(new GetGenericQuery<ProductDto, Product>(key));
        if (!product.Any()) return NotFound();

        var attrId = model.Id;
        if (string.IsNullOrEmpty(attrId)) return NotFound();

        var pam = product.FirstOrDefault()!.ProductAttributeMappings.FirstOrDefault(x => x.Id == attrId);
        if (pam == null) return BadRequest("No product attribute mapping found with the specified id");

        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _mediator.Send(new DeleteProductAttributeMappingCommand { Product = product.FirstOrDefault(), Model = pam });
        return Ok(result);
    }

    #endregion
}