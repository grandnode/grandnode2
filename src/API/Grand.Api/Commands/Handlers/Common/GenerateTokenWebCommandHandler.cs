using Grand.Api.Commands.Models.Common;
using Grand.Api.Infrastructure.Extensions;
using Grand.Api.Jwt;
using Grand.Infrastructure.Configuration;
using MediatR;

namespace Grand.Api.Commands.Handlers.Common
{
    public class GenerateTokenWebCommandHandler : IRequestHandler<GenerateTokenWebCommand, string>
    {
        private readonly FrontendAPIConfig _frontentApiConfig;

        public GenerateTokenWebCommandHandler(FrontendAPIConfig frontedApiConfig)
        {
            _frontentApiConfig = frontedApiConfig;
        }

        public async Task<string> Handle(GenerateTokenWebCommand request, CancellationToken cancellationToken)
        {
            var token = new JwtTokenBuilder();
            token.AddSecurityKey(JwtSecurityKey.Create(_frontentApiConfig.SecretKey));

            if (_frontentApiConfig.ValidateIssuer)
                token.AddIssuer(_frontentApiConfig.ValidIssuer);
            if (_frontentApiConfig.ValidateAudience)
                token.AddAudience(_frontentApiConfig.ValidAudience);

            token.AddClaims(request.Claims);
            token.AddExpiry(_frontentApiConfig.ExpiryInMinutes);
            token.Build();

            return await Task.FromResult(token.Build().Value);
        }
    }
}
