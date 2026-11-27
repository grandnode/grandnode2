using Grand.Module.Api.DTOs.Common;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Common;

public class DeletePictureCommand : IRequest<bool>
{
    public required PictureDto PictureDto { get; set; }
}