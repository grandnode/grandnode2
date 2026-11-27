using Grand.Module.Api.Commands.Models.Common;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Handlers.Common;

public class AddPictureCommandHandler : IRequestHandler<AddPictureCommand, PictureDto>
{
    private readonly IPictureService _pictureService;

    public AddPictureCommandHandler(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public async Task<PictureDto> Handle(AddPictureCommand request, CancellationToken cancellationToken)
    {
        //the validator requires the binary of a new picture
        var picture = await _pictureService.InsertPicture(
            request.PictureDto.PictureBinary ?? throw new ArgumentException("The picture has no binary", nameof(request)),
            request.PictureDto.MimeType,
            request.PictureDto.SeoFilename,
            request.PictureDto.AltAttribute,
            request.PictureDto.TitleAttribute,
            request.PictureDto.IsNew,
            request.PictureDto.Reference,
            request.PictureDto.ObjectId);

        return picture.ToModel();
    }
}