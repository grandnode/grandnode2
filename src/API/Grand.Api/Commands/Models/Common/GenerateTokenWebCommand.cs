using MediatR;
using System.Collections.Generic;

namespace Grand.Api.Commands.Models.Common
{
    public class GenerateTokenWebCommand : IRequest<string>
    {
        public Dictionary<string, string> Claims { get; set; }
    }
}
