using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class LanguageController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public LanguageController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from Languages by key", OperationId = "GetLanguageById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Languages))
                return Forbid();

            var language = await _mediator.Send(new GetGenericQuery<LanguageDto, Domain.Localization.Language>(key));
            if (!language.Any())
                return NotFound();

            return Ok(language.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from Languages", OperationId = "GetLanguages")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Languages))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<LanguageDto, Domain.Localization.Language>()));
        }
    }
}
