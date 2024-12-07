using Grand.Module.Api.DTOs.Common;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Common;

public class DeletePictureCommand : IRequest<bool>
{
    public PictureDto PictureDto { get; set; }
}