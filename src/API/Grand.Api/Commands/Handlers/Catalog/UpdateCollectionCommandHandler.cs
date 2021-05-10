using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Seo;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateCollectionCommandHandler : IRequestHandler<UpdateCollectionCommand, CollectionDto>
    {
        private readonly ICollectionService _collectionService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IPictureService _pictureService;
        private readonly SeoSettings _seoSettings;

        public UpdateCollectionCommandHandler(
            ICollectionService collectionService,
            ISlugService slugService,
            ILanguageService languageService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IPictureService pictureService,
            SeoSettings seoSettings)
        {
            _collectionService = collectionService;
            _slugService = slugService;
            _languageService = languageService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _pictureService = pictureService;
            _seoSettings = seoSettings;
        }

        public async Task<CollectionDto> Handle(UpdateCollectionCommand request, CancellationToken cancellationToken)
        {
            var collection = await _collectionService.GetCollectionById(request.Model.Id);
            var prevPictureId = collection.PictureId;
            collection = request.Model.ToEntity(collection);
            collection.UpdatedOnUtc = DateTime.UtcNow;
            request.Model.SeName = await collection.ValidateSeName(request.Model.SeName, collection.Name, true, _seoSettings, _slugService, _languageService);
            collection.SeName = request.Model.SeName;
            await _collectionService.UpdateCollection(collection);
            //search engine name
            await _slugService.SaveSlug(collection, request.Model.SeName, "");
            await _collectionService.UpdateCollection(collection);
            //delete an old picture (if deleted or updated)
            if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != collection.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }
            //update picture seo file name
            if (!string.IsNullOrEmpty(collection.PictureId))
            {
                var picture = await _pictureService.GetPictureById(collection.PictureId);
                if (picture != null)
                    await _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(collection.Name));
            }
            //activity log
            await _customerActivityService.InsertActivity("EditCollection", collection.Id, _translationService.GetResource("ActivityLog.EditCollection"), collection.Name);

            return collection.ToModel();
        }
    }
}
