using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using MediatR;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Api.Controllers.OData
{
	public partial class PageController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public PageController(
            IMediator mediator,
            IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from Page by key", OperationId = "GetPageById")]
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Pages))
                return Forbid();

            var productAttribute = await _mediator.Send(new GetQuery<PageDto>() { Id = key });
            if (!productAttribute.Any())
                return NotFound();

            return Ok(productAttribute.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from Page", OperationId = "GetPages")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Pages))
                return Forbid();

            return Ok(await _mediator.Send(new GetQuery<PageDto>()));
        }

    }
}
