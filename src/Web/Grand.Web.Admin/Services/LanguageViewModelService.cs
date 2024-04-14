using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class LanguageViewModelService : ILanguageViewModelService
{
    #region Constructors

    public LanguageViewModelService(ILanguageService languageService,
        ITranslationService translationService,
        ICurrencyService currencyService)
    {
        _translationService = translationService;
        _languageService = languageService;
        _currencyService = currencyService;
    }

    #endregion

    public virtual void PrepareFlagsModel(LanguageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.FlagFileNames = Directory
            .EnumerateFiles(CommonPath.WebHostMapPath("/assets/images/flags/"), "*.png", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
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
        var resource = await _translationService.GetTranslateResourceById(model.Id);
        // if the resourceName changed, ensure it isn't being used by another resource
        if (!resource.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase))
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
                    x.Name.ToLowerInvariant().Contains(model.ResourceName.ToLowerInvariant()));
            if (!string.IsNullOrEmpty(model.ResourceValue))
                resources = resources.Where(x =>
                    x.Value.ToLowerInvariant().Contains(model.ResourceValue.ToLowerInvariant()));
        }

        resources = resources.AsQueryable();
        return (resources.Skip((pageIndex - 1) * pageSize).Take(pageSize), resources.Count());
    }

    #region Fields

    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly ICurrencyService _currencyService;

    #endregion
}