using Grand.Module.Api.DTOs.Common;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Common;

public class AddPictureCommand : IRequest<PictureDto>
{
    public PictureDto PictureDto { get; set; }
}