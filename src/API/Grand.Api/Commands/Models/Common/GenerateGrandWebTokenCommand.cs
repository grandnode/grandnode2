using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Common
{
    public class GenerateGrandWebTokenCommand : IRequest<string>
    {
        public Dictionary<string, string> Claims { get; set; }
    }
}
