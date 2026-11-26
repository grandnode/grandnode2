using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Localization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Services;

public class LanguageViewModelService : ILanguageViewModelService
{

    #region Fields

    private readonly ILanguageService _languageService;
    private readonly IRequestTranslationService _translationService;
    private readonly ICurrencyService _currencyService;
    private readonly IWebHostEnvironment _hostingEnvironment;

    #endregion

    #region Constructors

    public LanguageViewModelService(ILanguageService languageService,
        IRequestTranslationService translationService,
        ICurrencyService currencyService,
        IWebHostEnvironment hostingEnvironment)
    {
        _translationService = translationService;
        _languageService = languageService;
        _currencyService = currencyService;
        _hostingEnvironment = hostingEnvironment;
    }

    #endregion

    public virtual void PrepareFlagsModel(LanguageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var filepath = Path.Combine(_hostingEnvironment.WebRootPath, "assets/images/flags");
        model.FlagFileNames = Directory
            .EnumerateFiles(filepath, "*.png", SearchOption.TopDirectoryOnly)
            .Select(x => Path.GetFileName(x))
            .ToList();
    }

    public virtual async Task PrepareCurrenciesModel(LanguageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableCurrencies.Add(new SelectListItem {
            Text = "---",
            Value = ""
        });
        var currencies = await _currencyService.GetAllCurrencies(true);
        foreach (var currency in currencies)
            model.AvailableCurrencies.Add(new SelectListItem {
                Text = currency.Name,
                Value = currency.Id
            });
    }

    public virtual async Task<Language> InsertLanguageModel(LanguageModel model)
    {
        var language = model.ToEntity();
        await _languageService.InsertLanguage(language);
        return language;
    }

    public virtual async Task<Language> UpdateLanguageModel(Language language, LanguageModel model)
    {
        //update
        language = model.ToEntity(language);
        await _languageService.UpdateLanguage(language);
        return language;
    }

    public virtual async Task<(bool error, string message)> InsertLanguageResourceModel(LanguageResourceModel model)
    {
        //the resource is added on a language's page, which posts its id
        ArgumentException.ThrowIfNullOrEmpty(model.LanguageId);

        if (model.Name != null)
            model.Name = model.Name.Trim();
        if (model.Value != null)
            model.Value = model.Value.Trim();

        var res = await _translationService.GetTranslateResourceByName(model.Name, model.LanguageId);
        if (res == null)
        {
            var resource = new TranslationResource {
                LanguageId = model.LanguageId,
                Name = model.Name,
                Value = model.Value,
                Area = (TranslationResourceArea)model.Area
            };
            await _translationService.InsertTranslateResource(resource);
        }
        else
        {
            return (error: true,
                message: string.Format(
                    _translationService.GetResource("Admin.Configuration.Languages.Resources.NameAlreadyExists"),
                    model.Name));
        }

        return (false, string.Empty);
    }

    public virtual async Task<(bool error, string message)> UpdateLanguageResourceModel(LanguageResourceModel model)
    {
        if (model.Name != null)
            model.Name = model.Name.Trim();
        if (model.Value != null)
            model.Value = model.Value.Trim();
        var resource = await _translationService.GetTranslateResourceById(model.Id)
                       ?? throw new ArgumentException("No resource found with the specified id");
        // if the resourceName changed, ensure it isn't being used by another resource
        if (!string.Equals(resource.Name, model.Name, StringComparison.OrdinalIgnoreCase))
        {
            var res = await _translationService.GetTranslateResourceByName(model.Name, model.LanguageId);
            if (res != null && res.Id != resource.Id)
                return (error: true,
                    message: string.Format(
                        _translationService.GetResource("Admin.Configuration.Languages.Resources.NameAlreadyExists"),
                        res.Name));
        }

        resource.Name = model.Name;
        resource.Value = model.Value;
        resource.Area = (TranslationResourceArea)model.Area;

        await _translationService.UpdateTranslateResource(resource);
        return (false, string.Empty);
    }

    public virtual async Task<(IEnumerable<LanguageResourceModel> languageResourceModels, int totalCount)>
        PrepareLanguageResourceModel(LanguageResourceFilterModel model, string languageId, int pageIndex, int pageSize)
    {
        var language = await _languageService.GetLanguageById(languageId);

        var resources = _translationService
            .GetAllResources(languageId)
            .OrderBy(x => x.Name)
            .Select(x => new LanguageResourceModel {
                LanguageId = languageId,
                Id = x.Id,
                Name = x.Name,
                Value = x.Value,
                Area = (int)x.Area
            });

        if (model != null)
        {
            if (!string.IsNullOrEmpty(model.ResourceName))
                resources = resources.Where(x =>
                    x.Name != null && x.Name.ToLowerInvariant().Contains(model.ResourceName.ToLowerInvariant()));
            if (!string.IsNullOrEmpty(model.ResourceValue))
                resources = resources.Where(x =>
                    x.Value != null && x.Value.ToLowerInvariant().Contains(model.ResourceValue.ToLowerInvariant()));
        }

        resources = resources.AsQueryable();
        return (resources.Skip((pageIndex - 1) * pageSize).Take(pageSize), resources.Count());
    }

    public virtual async Task<(bool canProceed, string message)> ValidateLanguageUnpublish(string languageId,
        bool published)
    {
        if (published)
            return (true, string.Empty);

        var allLanguages = await _languageService.GetAllLanguages();
        if (allLanguages.Count == 1 && allLanguages[0].Id == languageId)
            return (false, "At least one published language is required.");

        return (true, string.Empty);
    }

    public virtual async Task<(bool canDelete, string message)> ValidateLanguageDelete(Language language)
    {
        var allLanguages = await _languageService.GetAllLanguages();
        if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id)
            return (false, "At least one published language is required.");

        return (true, string.Empty);
    }
}