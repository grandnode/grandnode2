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
    public partial class CategoryLayoutController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public CategoryLayoutController(
            IMediator mediator,
            IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from CategoryLayout by key", OperationId = "GetCategoryLayoutById")]
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Maintenance))
                return Forbid();

            var layout = await _mediator.Send(new GetLayoutQuery() { Id = key, LayoutName = typeof(Domain.Catalog.CategoryLayout).Name });
            if (!layout.Any())
                return NotFound();

            return Ok(layout.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from CategoryLayout", OperationId = "GetCategoryLayout")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Maintenance))
                return Forbid();

            return Ok(await _mediator.Send(new GetLayoutQuery() { LayoutName = typeof(Domain.Catalog.CategoryLayout).Name }));

        }
    }
}
