using Grand.Module.Api.DTOs.Common;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Common;

public class AddPictureCommand : IRequest<PictureDto>
{
    public required PictureDto PictureDto { get; set; }
}