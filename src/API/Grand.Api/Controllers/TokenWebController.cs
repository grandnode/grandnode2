using Grand.Api.Commands.Models.Common;
using Grand.Api.DTOs;
using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Api.Controllers
{
    public class TokenWebController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IMediator _mediator;
        private readonly IStoreHelper _storeHelper;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUserFieldService _userFieldService;
        private readonly IAntiforgery _antiforgery;

        public TokenWebController(
            ICustomerService customerService,
            ICustomerManagerService customerManagerService,
            IMediator mediator,
            IStoreHelper storeHelper,
            IRefreshTokenService refreshTokenService,
            IUserFieldService userFieldService,
            IAntiforgery antiforgery)
        {
            _customerService = customerService;
            _customerManagerService = customerManagerService;
            _mediator = mediator;
            _storeHelper = storeHelper;
            _refreshTokenService = refreshTokenService;
            _userFieldService = userFieldService;
            _antiforgery = antiforgery;
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Guest()
        {
            var customer = await _customerService.InsertGuestCustomer(_storeHelper.HostStore);
            var claims = new Dictionary<string, string> {
                { "Guid", customer.CustomerGuid.ToString()}
            };

            var tokenDto = await GetToken(claims, customer);
            return Ok(tokenDto);
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

            var customer = await _customerService.GetCustomerByEmail(model.Email);

            var claims = new Dictionary<string, string> {
                {
                    "Email", model.Email
                },
                {
                    "Token",
                    await _userFieldService.GetFieldsForEntity<string>(customer, SystemCustomerFieldNames.PasswordToken)
                }
            };
            var tokenDto = await GetToken(claims, customer);
            return Ok(tokenDto);
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Refresh([FromBody] TokenDto tokenDto)
        {
            string email = null, guid = null;
            Customer customer = null;
            var claims = new Dictionary<string, string>();
            var principal = _refreshTokenService.GetPrincipalFromToken(tokenDto.AccessToken);
            email = principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                customer = await _customerService.GetCustomerByEmail(email);
                claims.Add("Email", email);
                claims.Add("Token", await _userFieldService.GetFieldsForEntity<string>(customer, SystemCustomerFieldNames.PasswordToken));
            }
            else
            {
                guid = principal.Claims.ToList().FirstOrDefault(x => x.Type == "Guid")?.Value;
                customer = await _customerService.GetCustomerByGuid(Guid.Parse(guid));
                claims.Add("Guid", guid);
            }

            var customerRefreshToken = await _refreshTokenService.GetCustomerRefreshToken(customer);
            if (customerRefreshToken is null || !customerRefreshToken.Token.Equals(tokenDto.RefreshToken))
            {
                return BadRequest("Invalid refresh token");
            }
            var token = await GetToken(claims, customer); ;
            return Ok(token);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Antiforgery()
        {
            var token = _antiforgery.GetAndStoreTokens(this.HttpContext).RequestToken;
            return Ok(token);
        }

        private async Task<TokenDto> GetToken(Dictionary<string, string> claims, Customer customer)
        {
            var token = await _mediator.Send(new GenerateTokenWebCommand() { Claims = claims });
            var refreshToken = _refreshTokenService.GenerateRefreshToken();
            await _refreshTokenService.SaveRefreshTokenToCustomer(customer, refreshToken);
            return new TokenDto() {
                AccessToken = token,
                RefreshToken = refreshToken
            };
        }
    }
}
