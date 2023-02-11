﻿using Grand.Api.Commands.Models.Common;
using Grand.Api.Models.Common;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Grand.Api.Controllers
{
    [ApiController]
    [Area("Api")]
    [Route("[area]/[controller]/[action]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
    [SwaggerTag(description: "Create token")]
    public class TokenController : Controller
    {
        private readonly IUserApiService _userApiService;
        private readonly IMediator _mediator;

        public TokenController(IMediator mediator, IUserApiService userApiService)
        {
            _userApiService = userApiService;
            _mediator = mediator;
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var claims = new Dictionary<string, string> {
                { "Email", model.Email }
            };
            var user = await _userApiService.GetUserByEmail(model.Email);
            if (user != null)
                claims.Add("Token", user.Token);

            var token = await _mediator.Send(new GenerateTokenCommand() { Claims = claims });
            return Content(token);
        }
    }
}
