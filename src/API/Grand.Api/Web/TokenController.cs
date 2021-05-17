using Grand.Api.Commands.Models.Common;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Api.Web
{

    public class TokenController : BaseController
    {
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IMediator _mediator;

        public TokenController(ICustomerService customerService,IWorkContext workContext,ICustomerManagerService customerManagerService,IMediator mediator)
        {
            _customerService = customerService;
            _workContext = workContext;
            _customerManagerService = customerManagerService;
            _mediator = mediator;
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Guest()
        {
            var customer=await _customerService.InsertGuestCustomer(_workContext.CurrentStore);
            var claims = new Dictionary<string, string> {
                { "Id", customer.Id}
            };

            var token = await _mediator.Send(new GenerateGrandWebTokenCommand() { Claims = claims });
            return Ok(token);
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Models.Common.LoginModel model)
        {
            var result = await _customerManagerService.LoginCustomer(model.Email, model.Password);
            if (!result.Equals(CustomerLoginResults.Successful))
            {
                return BadRequest(result.ToString());
            }
            var claims = new Dictionary<string, string> {
                { "Email", model.Email}
            };
            var token = await _mediator.Send(new GenerateGrandWebTokenCommand() { Claims = claims });
            return Ok(token);
        }
    }
}
