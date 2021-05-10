using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using MediatR;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Threading.Tasks;

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

        [SwaggerOperation(summary: "Get entity from Brand by key", OperationId = "GetBrandById")]
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            var brand = await _mediator.Send(new GetQuery<BrandDto>() { Id = key });
            if (!brand.Any())
                return NotFound();

            return Ok(brand);
        }

        [SwaggerOperation(summary: "Get entities from Brand", OperationId = "GetBrands")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            return Ok(await _mediator.Send(new GetQuery<BrandDto>()));
        }

        [SwaggerOperation(summary: "Add new entity to Brand", OperationId = "InsertBrand")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] BrandDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddBrandCommand() { Model = model });
                return Created(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in Brand", OperationId = "UpdateBrand")]
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] BrandDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();


            var brand = await _mediator.Send(new GetQuery<BrandDto>() { Id = model.Id });
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
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<BrandDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            var brand = await _mediator.Send(new GetQuery<BrandDto>() { Id = key });
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
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            var brand = await _mediator.Send(new GetQuery<BrandDto>() { Id = key });
            if (!brand.Any())
            {
                return NotFound();
            }

            await _mediator.Send(new DeleteBrandCommand() { Model = brand.FirstOrDefault() });

            return Ok();
        }
    }
}
