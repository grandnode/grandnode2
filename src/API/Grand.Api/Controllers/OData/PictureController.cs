using Grand.Api.Commands.Models.Common;
using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Media;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData;

[Route("odata/Picture")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class PictureController : BaseODataController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public PictureController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entities from Picture by key", OperationId = "GetPictureById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures)) return Forbid();

        var picture = await _mediator.Send(new GetGenericQuery<PictureDto, Picture>(key));
        if (picture == null || !picture.Any()) return NotFound();

        return Ok(picture.FirstOrDefault());
    }

    [SwaggerOperation("Add new entity in Picture", OperationId = "InsertPicture")]
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

    [SwaggerOperation("Update entity in Picture", OperationId = "UpdatePicture")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Put([FromBody] PictureDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Categories)) return Forbid();

        var picture = await _mediator.Send(new GetGenericQuery<PictureDto, Picture>(model.Id));
        if (picture == null || !picture.Any()) return NotFound();

        var result = await _mediator.Send(new UpdatePictureCommand { Model = model });
        return Ok(result);
    }

    [SwaggerOperation("Delete entity in Picture", OperationId = "DeletePicture")]
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures)) return Forbid();

        var picture = await _mediator.Send(new GetGenericQuery<PictureDto, Picture>(key));
        if (picture == null || !picture.Any()) return NotFound();

        await _mediator.Send(new DeletePictureCommand { PictureDto = picture.FirstOrDefault() });
        return Ok();
    }
}