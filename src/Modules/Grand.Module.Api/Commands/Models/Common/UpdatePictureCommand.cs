using Grand.Module.Api.DTOs.Common;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Common;

public class UpdatePictureCommand : IRequest<bool>
{
    public PictureDto Model { get; set; }
}