using Grand.Module.Api.DTOs.Common;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Common;

public class DeletePictureCommand : IRequest<bool>
{
    public PictureDto PictureDto { get; set; }
}