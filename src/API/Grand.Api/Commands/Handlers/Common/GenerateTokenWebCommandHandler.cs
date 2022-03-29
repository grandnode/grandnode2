using Grand.Api.Commands.Models.Common;
using Grand.Api.Infrastructure.Extensions;
using Grand.Api.Jwt;
using Grand.Infrastructure.Configuration;
using MediatR;

namespace Grand.Api.Commands.Handlers.Common
{
    public class GenerateTokenWebCommandHandler : IRequestHandler<GenerateTokenWebCommand, string>
    {
        private readonly FrontendAPIConfig _frontendApiConfig;

        public GenerateTokenWebCommandHandler(FrontendAPIConfig frontedApiConfig)
        {
            _frontendApiConfig = frontedApiConfig;
        }

        public async Task<string> Handle(GenerateTokenWebCommand request, CancellationToken cancellationToken)
        {
            var token = new JwtTokenBuilder();
            token.AddSecurityKey(JwtSecurityKey.Create(_frontendApiConfig.SecretKey));

            if (_frontendApiConfig.ValidateIssuer)
                token.AddIssuer(_frontendApiConfig.ValidIssuer);
            if (_frontendApiConfig.ValidateAudience)
                token.AddAudience(_frontendApiConfig.ValidAudience);

            token.AddClaims(request.Claims);
            token.AddExpiry(_frontendApiConfig.ExpiryInMinutes);
            token.Build();

            return await Task.FromResult(token.Build().Value);
        }
    }
}
