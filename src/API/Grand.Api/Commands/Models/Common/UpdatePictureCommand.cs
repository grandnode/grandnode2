using Grand.Api.DTOs.Common;
using MediatR;

namespace Grand.Api.Commands.Models.Common;

public class UpdatePictureCommand : IRequest<bool>
{
    public PictureDto Model { get; set; }
}