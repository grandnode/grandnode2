using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class CategoryController : BaseODataController
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

        [SwaggerOperation(summary: "Get entity from Category by key", OperationId = "GetCategoryById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Categories))
                return Forbid();

            var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Domain.Catalog.Category>(key));
            if (!category.Any())
                return NotFound();

            return Ok(category.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from Category", OperationId = "GetCategories")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Categories))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<CategoryDto, Domain.Catalog.Category>()));
        }

        [SwaggerOperation(summary: "Add new entity to Category", OperationId = "InsertCategory")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] CategoryDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Categories))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddCategoryCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in Category", OperationId = "UpdateCategory")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Put([FromBody] CategoryDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Categories))
                return Forbid();

            var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Domain.Catalog.Category>(model.Id));
            if (!category.Any())
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new UpdateCategoryCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }
        [SwaggerOperation(summary: "Update entity in Category (delta)", OperationId = "UpdateCategoryPatch")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<CategoryDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Categories))
                return Forbid();

            var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Domain.Catalog.Category>(key));
            if (!category.Any())
            {
                return NotFound();
            }
            var cat = category.FirstOrDefault();
            model.ApplyTo(cat);

            if (ModelState.IsValid)
            {
                await _mediator.Send(new UpdateCategoryCommand() { Model = cat });
                return Ok();
            }
            return BadRequest(ModelState);
        }
        [SwaggerOperation(summary: "Delete entity from Category", OperationId = "DeleteCategory")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Categories))
                return Forbid();

            var category = await _mediator.Send(new GetGenericQuery<CategoryDto, Domain.Catalog.Category>(key));
            if (!category.Any())
            {
                return NotFound();
            }

            await _mediator.Send(new DeleteCategoryCommand() { Model = category.FirstOrDefault() });

            return Ok();
        }
    }
}
