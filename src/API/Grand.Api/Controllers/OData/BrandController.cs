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
    public partial class BrandController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        public BrandController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entities from Brand", OperationId = "GetBrands")]
        [HttpGet]
        [EnableQuery]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<BrandDto, Domain.Catalog.Brand>()));
        }

        [SwaggerOperation(summary: "Get entity from Brand by key", OperationId = "GetBrandById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Domain.Catalog.Brand>(key));
            if (!brand.Any())
                return NotFound();

            return Ok(brand.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Add new entity to Brand", OperationId = "InsertBrand")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] BrandDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddBrandCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in Brand", OperationId = "UpdateBrand")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Put([FromBody] BrandDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();


            var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Domain.Catalog.Brand>(model.Id));
            if (!brand.Any())
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new UpdateBrandCommand() { Model = model });
                return Ok(model);
            }

            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Partially update entity in Brand", OperationId = "PartiallyUpdateBrand")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<BrandDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Domain.Catalog.Brand>(key));
            if (!brand.Any())
            {
                return NotFound();
            }
            var man = brand.FirstOrDefault();
            model.ApplyTo(man);

            if (ModelState.IsValid)
            {
                await _mediator.Send(new UpdateBrandCommand() { Model = man });
                return Ok();
            }

            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Delete entity in Brand", OperationId = "DeleteBrand")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Domain.Catalog.Brand>(key));
            if (!brand.Any())
            {
                return NotFound();
            }

            await _mediator.Send(new DeleteBrandCommand() { Model = brand.FirstOrDefault() });

            return Ok();
        }
    }
}
