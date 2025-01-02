using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Http;

namespace Grand.Module.Api.Controllers;

public class CategoryController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CategoryController(
        IMediator mediator,
        IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from Category by key", OperationId = "GetCategoryById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Category>(key));
        if (!category.Any()) return NotFound();

        return Ok(category.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from Category", OperationId = "GetCategories")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CategoryDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CategoryDto, Category>()));
    }

    [SwaggerOperation("Add new entity to Category", OperationId = "InsertCategory")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] CategoryDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        model = await _mediator.Send(new AddCategoryCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in Category", OperationId = "UpdateCategory")]
    [HttpPut("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Put([FromRoute] string key, [FromBody] CategoryDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Category>(key));
        if (!category.Any()) return NotFound();

        model = await _mediator.Send(new UpdateCategoryCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in Category (delta)", OperationId = "UpdateCategoryPatch")]
    [HttpPatch("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<CategoryDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Category>(key));
        if (!category.Any()) return NotFound();

        var cat = category.FirstOrDefault();
        model.ApplyTo(cat);
        await _mediator.Send(new UpdateCategoryCommand { Model = cat });
        return Ok();
    }

    [SwaggerOperation("Delete entity from Category", OperationId = "DeleteCategory")]
    [HttpDelete("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Category>(key));
        if (!category.Any()) return NotFound();

        await _mediator.Send(new DeleteCategoryCommand { Model = category.FirstOrDefault() });
        return Ok();
    }
}