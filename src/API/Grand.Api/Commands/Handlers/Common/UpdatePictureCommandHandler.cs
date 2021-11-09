﻿using Grand.Business.Storage.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdatePictureCommandHandler : IRequestHandler<UpdatePictureCommand, bool>
    {
        private readonly IPictureService _pictureService;

        public UpdatePictureCommandHandler(
            IPictureService pictureService)
        {
            _pictureService = pictureService;
        }

        public async Task<bool> Handle(UpdatePictureCommand request, CancellationToken cancellationToken)
        {
            var picture = await _pictureService.GetPictureById(request.Model.Id);
            if (picture == null)
                return false;

            await _pictureService.UpdatePicture(picture.Id, request.Model.PictureBinary,
                request.Model.MimeType, request.Model.SeoFilename, request.Model.AltAttribute, request.Model.TitleAttribute);

            return true;
        }
    }
}
