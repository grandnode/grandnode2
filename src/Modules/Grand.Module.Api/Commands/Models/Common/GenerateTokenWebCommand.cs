using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Common;

public class GenerateTokenWebCommand : IRequest<string>
{
    public required Dictionary<string, string?> Claims { get; set; }
}