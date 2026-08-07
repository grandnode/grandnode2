using Grand.Module.Api.DTOs.Common;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Common;

public class UpdatePictureCommand : IRequest<bool>
{
    public PictureDto Model { get; set; }
}