﻿using Grand.Domain.Localization;
using Grand.Web.Admin.Models.Localization;

namespace Grand.Web.Admin.Interfaces
{
    public interface ILanguageViewModelService
    {
        void PrepareFlagsModel(LanguageModel model);
        Task PrepareCurrenciesModel(LanguageModel model);
        Task<Language> InsertLanguageModel(LanguageModel model);
        Task<Language> UpdateLanguageModel(Language language, LanguageModel model);
        Task<(bool error, string message)> InsertLanguageResourceModel(LanguageResourceModel model);
        Task<(bool error, string message)> UpdateLanguageResourceModel(LanguageResourceModel model);
        Task<(IEnumerable<LanguageResourceModel> languageResourceModels, int totalCount)> PrepareLanguageResourceModel(LanguageResourceFilterModel model, string languageId, int pageIndex, int pageSize);
    }
}
