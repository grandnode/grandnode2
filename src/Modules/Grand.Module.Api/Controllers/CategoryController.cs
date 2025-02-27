using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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

    [EndpointDescription("Get entity from Category by key")]
    [EndpointName("GetCategoryById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Category>(key));
        if (!category.Any()) return NotFound();

        return Ok(category.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Category")]
    [EndpointName("GetCategories")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CategoryDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CategoryDto, Category>()));
    }

    [EndpointDescription("Add new entity to Category")]
    [EndpointName("InsertCategory")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CategoryDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        model = await _mediator.Send(new AddCategoryCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Category")]
    [EndpointName("UpdateCategory")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] CategoryDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        model = await _mediator.Send(new UpdateCategoryCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Category (delta)")]
    [EndpointName("UpdateCategoryPatch")]
    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    [EndpointDescription("Delete entity from Category")]
    [EndpointName("DeleteCategory")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Category>(key));
        if (!category.Any()) return NotFound();

        await _mediator.Send(new DeleteCategoryCommand { Model = category.FirstOrDefault() });
        return Ok();
    }
}