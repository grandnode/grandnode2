using Grand.Api.Commands.Models.Common;
using Grand.Api.Infrastructure.Extensions;
using Grand.Api.Jwt;
using Grand.Infrastructure.Configuration;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Common
{
    public class GenerateGrandWebTokenCommandHandler : IRequestHandler<GenerateGrandWebTokenCommand, string>
    {
        private readonly GrandWebApiConfig _grandWebApiConfig;

        public GenerateGrandWebTokenCommandHandler(GrandWebApiConfig grandWebApiConfig)
        {
            _grandWebApiConfig = grandWebApiConfig;
        }

        public async Task<string> Handle(GenerateGrandWebTokenCommand request, CancellationToken cancellationToken)
        {
            var token = new JwtTokenBuilder();
            token.AddSecurityKey(JwtSecurityKey.Create(_grandWebApiConfig.SecretKey));

            if (_grandWebApiConfig.ValidateIssuer)
                token.AddIssuer(_grandWebApiConfig.ValidIssuer);
            if (_grandWebApiConfig.ValidateAudience)
                token.AddAudience(_grandWebApiConfig.ValidAudience);

            token.AddClaims(request.Claims);
            token.AddExpiry(_grandWebApiConfig.ExpiryInMinutes);
            token.Build();

            return await Task.FromResult(token.Build().Value);
        }
    }
}
