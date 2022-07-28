using Grand.Api.Commands.Models.Common;
using Grand.Api.DTOs;
using Grand.Api.Models.Common;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Grand.Api.Controllers
{
    public class TokenWebController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IMediator _mediator;
        private readonly IStoreHelper _storeHelper;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUserFieldService _userFieldService;
        private readonly IAntiforgery _antiforgery;
        private readonly FrontendAPIConfig _apiConfig;

        public TokenWebController(
            ICustomerService customerService,
            IMediator mediator,
            IStoreHelper storeHelper,
            IRefreshTokenService refreshTokenService,
            IUserFieldService userFieldService,
            IAntiforgery antiforgery,
            FrontendAPIConfig apiConfig)
        {
            _customerService = customerService;
            _mediator = mediator;
            _storeHelper = storeHelper;
            _refreshTokenService = refreshTokenService;
            _userFieldService = userFieldService;
            _antiforgery = antiforgery;
            _apiConfig = apiConfig;
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Guest()
        {
            if (!_apiConfig.Enabled)
                return BadRequest("API is disabled");

            var customer = await _customerService.InsertGuestCustomer(_storeHelper.StoreHost);
            var claims = new Dictionary<string, string> {
                { "Guid", customer.CustomerGuid.ToString()}
            };

            var tokenDto = await GetToken(claims, customer);
            return Ok(tokenDto);
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var customer = await _customerService.GetCustomerByEmail(model.Email);
                var claims = new Dictionary<string, string> {
                    { "Email", model.Email },
                    { "Token", await _userFieldService.GetFieldsForEntity<string>(customer, SystemCustomerFieldNames.PasswordToken) }
                };
                var tokenDto = await GetToken(claims, customer);
                return Ok(tokenDto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Refresh([FromBody] TokenDto tokenDto)
        {
            if (!_apiConfig.Enabled)
                return BadRequest("API is disabled");

            string email = null, guid = null;
            Customer customer = null;
            var claims = new Dictionary<string, string>();
            ClaimsPrincipal principal = null;
            try
            {
                principal = _refreshTokenService.GetPrincipalFromToken(tokenDto.AccessToken);
                email = principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
            }
            catch (Exception)
            {
                return BadRequest("Invalid access token");
            }
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

            if (customerRefreshToken.ValidTo.CompareTo(DateTime.UtcNow) < 0)
            {
                return BadRequest("Token expired");
            }
            var token = await GetToken(claims, customer); ;
            return Ok(token);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Antiforgery()
        {
            if (!_apiConfig.Enabled)
                return BadRequest("API is disabled");

            var token = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
            return Ok(token);
        }

        private async Task<TokenDto> GetToken(Dictionary<string, string> claims, Customer customer)
        {
            var refreshTokenValue = _refreshTokenService.GenerateRefreshToken();
            var refreshToken = await _refreshTokenService.SaveRefreshTokenToCustomer(customer, refreshTokenValue);
            claims.Add("RefreshId", refreshToken.RefreshId);
            var token = await _mediator.Send(new GenerateTokenWebCommand() { Claims = claims });
            return new TokenDto() {
                AccessToken = token,
                RefreshToken = refreshTokenValue
            };
        }
    }
}
