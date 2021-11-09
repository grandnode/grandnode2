using Grand.Api.DTOs.Common;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdatePictureCommand : IRequest<bool>
    {
        public PictureDto Model { get; set; }
    }
}
