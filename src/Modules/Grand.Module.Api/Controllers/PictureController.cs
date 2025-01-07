using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.Module.Api.Commands.Models.Common;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Net;

namespace Grand.Module.Api.Controllers;

public class PictureController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public PictureController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entities from Picture by key")]
    [EndpointName("GetPictureById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PictureDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures)) return Forbid();

        var picture = await _mediator.Send(new GetGenericQuery<PictureDto, Picture>(key));
        if (picture == null || !picture.Any()) return NotFound();

        return Ok(picture.FirstOrDefault());
    }

    [EndpointDescription("Add new entity in Picture")]
    [EndpointName("InsertPicture")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] PictureDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures)) return Forbid();

        model = await _mediator.Send(new AddPictureCommand { PictureDto = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Picture")]
    [EndpointName("UpdatePicture")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Put([FromBody] PictureDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures)) return Forbid();

        var result = await _mediator.Send(new UpdatePictureCommand { Model = model });
        return Ok(result);
    }

    [EndpointDescription("Delete entity in Picture")]
    [EndpointName("DeletePicture")]
    [HttpDelete("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures)) return Forbid();

        var picture = await _mediator.Send(new GetGenericQuery<PictureDto, Picture>(key));
        if (picture == null || !picture.Any()) return NotFound();

        await _mediator.Send(new DeletePictureCommand { PictureDto = picture.FirstOrDefault() });
        return Ok();
    }
}