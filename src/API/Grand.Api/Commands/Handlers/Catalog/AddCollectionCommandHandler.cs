using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class AddCollectionCommandHandler : IRequestHandler<AddCollectionCommand, CollectionDto>
{
    private readonly ICollectionService _collectionService;
    private readonly ILanguageService _languageService;
    private readonly SeoSettings _seoSettings;
    private readonly ISlugService _slugService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public AddCollectionCommandHandler(
        ICollectionService collectionService,
        ISlugService slugService,
        ILanguageService languageService,
        ITranslationService translationService,
        IWorkContext workContext,
        SeoSettings seoSettings)
    {
        _collectionService = collectionService;
        _slugService = slugService;
        _languageService = languageService;
        _translationService = translationService;
        _workContext = workContext;
        _seoSettings = seoSettings;
    }

    public async Task<CollectionDto> Handle(AddCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = request.Model.ToEntity();
        await _collectionService.InsertCollection(collection);
        request.Model.SeName = await collection.ValidateSeName(request.Model.SeName, collection.Name, true,
            _seoSettings, _slugService, _languageService);
        collection.SeName = request.Model.SeName;
        await _collectionService.UpdateCollection(collection);
        await _slugService.SaveSlug(collection, request.Model.SeName, "");

        return collection.ToModel();
    }
}