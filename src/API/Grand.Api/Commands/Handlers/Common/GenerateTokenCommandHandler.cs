﻿using Grand.Api.Commands.Models.Common;
using Grand.Api.Infrastructure.Extensions;
using Grand.Api.Jwt;
using Grand.Infrastructure.Configuration;
using MediatR;

namespace Grand.Api.Commands.Handlers.Common
{
    public class GenerateTokenCommandHandler : IRequestHandler<GenerateTokenCommand, string>
    {
        private readonly BackendAPIConfig _apiConfig;

        public GenerateTokenCommandHandler(BackendAPIConfig apiConfig)
        {
            _apiConfig = apiConfig;
        }
        public async Task<string> Handle(GenerateTokenCommand request, CancellationToken cancellationToken)
        {
            var token = new JwtTokenBuilder();
            token.AddSecurityKey(JwtSecurityKey.Create(_apiConfig.SecretKey));

            if (_apiConfig.ValidateIssuer)
                token.AddIssuer(_apiConfig.ValidIssuer);
            if (_apiConfig.ValidateAudience)
                token.AddAudience(_apiConfig.ValidAudience);

            token.AddClaims(request.Claims);
            token.AddExpiry(_apiConfig.ExpiryInMinutes);
            token.Build();

            return await Task.FromResult(token.Build().Value);
        }
    }
}
