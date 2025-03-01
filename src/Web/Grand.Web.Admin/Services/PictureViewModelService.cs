using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Services;

public class PictureViewModelService : IPictureViewModelService
{
    private readonly ILanguageService _languageService;
    private readonly IPictureService _pictureService;

    public PictureViewModelService(IPictureService pictureService, ILanguageService languageService)
    {
        _pictureService = pictureService;
        _languageService = languageService;
    }

    public virtual async Task<PictureModel> PreparePictureModel(string pictureId, string objectId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(objectId);
        var picture = await _pictureService.GetPictureById(pictureId);
        ArgumentNullException.ThrowIfNull(picture);

        var model = new PictureModel {
            Id = picture.Id,
            ObjectId = objectId,
            PictureUrl = await _pictureService.GetPictureUrl(picture),
            AltAttribute = picture.AltAttribute,
            TitleAttribute = picture.TitleAttribute,
            Style = picture.Style,
            ExtraField = picture.ExtraField
        };

        foreach (var language in await _languageService.GetAllLanguages(true))
        {
            var locale = new PictureModel.PictureLocalizedModel {
                LanguageId = language.Id,
                AltAttribute = picture.GetTranslation(x => x.AltAttribute, language.Id, false),
                TitleAttribute = picture.GetTranslation(x => x.TitleAttribute, language.Id, false)
            };
            model.Locales.Add(locale);
        }

        return model;
    }

    public virtual async Task UpdatePicture(PictureModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var picture = await _pictureService.GetPictureById(model.Id);
        ArgumentNullException.ThrowIfNull(picture);

        //Update picture fields
        await _pictureService.UpdatePictureField(picture, x => x.AltAttribute, model.AltAttribute);
        await _pictureService.UpdatePictureField(picture, x => x.TitleAttribute, model.TitleAttribute);
        await _pictureService.UpdatePictureField(picture, x => x.Locales, model.Locales.ToTranslationProperty());
        await _pictureService.UpdatePictureField(picture, x => x.Style, model.Style);
        await _pictureService.UpdatePictureField(picture, x => x.ExtraField, model.ExtraField);
    }
}