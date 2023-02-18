﻿using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Events;
using Grand.Domain.Localization;
using MediatR;

namespace Grand.Business.Common.Events
{
    public class LanguageDeletedEventHandler : INotificationHandler<EntityDeleted<Language>>
    {
        private readonly ILanguageService _languageService;
        private readonly ISettingService _settingService;

        private readonly LanguageSettings _languageSettings;

        public LanguageDeletedEventHandler(
            ILanguageService languageService,
            ISettingService settingService,
            LanguageSettings languageSettings
            )
        {
            _languageService = languageService;
            _settingService = settingService;
            _languageSettings = languageSettings;
        }
        public async Task Handle(EntityDeleted<Language> notification, CancellationToken cancellationToken)
        {
            //update default admin area language (if required)
            if (_languageSettings.DefaultAdminLanguageId == notification.Entity.Id)
            {
                foreach (var activeLanguage in await _languageService.GetAllLanguages())
                {
                    if (activeLanguage.Id == notification.Entity.Id) continue;
                    _languageSettings.DefaultAdminLanguageId = activeLanguage.Id;
                    await _settingService.SaveSetting(_languageSettings);
                    break;
                }
            }

        }
    }
}
